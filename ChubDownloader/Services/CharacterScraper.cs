using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using ChubDownloader.Models;
using System.Collections.Concurrent;
using ConsoleApp1.Services;

namespace ChubDownloader.Services
{
    public class CharacterScraper : ICharacterScraper
    {
        private readonly IWebDriverService _webDriver;
        private readonly IDownloadService _downloadService;
        private readonly string _downloadPath;
        private readonly ConcurrentDictionary<string, string> _globalCharacterIndex;
        private readonly string _indexFilePath;

        // XPath константы
        private static readonly string[] JSON_BUTTON_XPATHS = new[]
        {
            "//*[@id=\"root\"]/div/div/div/main/div/div[1]/div[1]/div[2]/div/button[2]",
            "//*[@id=\"root\"]/div/div/div/main/div/div[2]/div[1]/div[2]/div/button[2]"
        };

        private const string CHARACTER_LIST_SELECTOR = "#chara-list > a.cursor-pointer";
        private const string NEXT_PAGE_XPATH = "//*[@id='rc-tabs-1-panel-characters']/ul[1]/li[@title='Next Page']";

        public CharacterScraper(IWebDriverService webDriver, IDownloadService downloadService, string downloadPath)
        {
            _webDriver = webDriver;
            _downloadService = downloadService;
            _downloadPath = downloadPath;
            _indexFilePath = Path.Combine(Environment.CurrentDirectory, "character_index.json");
            _globalCharacterIndex = LoadCharacterIndex();
        }

        private ConcurrentDictionary<string, string> LoadCharacterIndex()
        {
            try
            {
                if (File.Exists(_indexFilePath))
                {
                    var json = File.ReadAllText(_indexFilePath);
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                    return new ConcurrentDictionary<string, string>(dict);
                }
            }
            catch { }
            
            return new ConcurrentDictionary<string, string>();
        }

        private void SaveCharacterIndex()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_globalCharacterIndex);
                File.WriteAllText(_indexFilePath, json);
            }
            catch { }
        }

        private bool IsCharacterExists(string characterId, out string existingPath)
        {
            return _globalCharacterIndex.TryGetValue(characterId, out existingPath);
        }

        private void RegisterCharacter(string characterId, string filePath)
        {
            _globalCharacterIndex[characterId] = filePath;
            SaveCharacterIndex();
        }

        public async Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken)
        {
            var root = Path.Combine(Environment.CurrentDirectory, "followers");
            Directory.CreateDirectory(root);

            _webDriver.NavigateTo("https://chub.ai/leaderboard?segment=followers");
            _webDriver.WaitForElement(By.CssSelector("main table tbody tr"));

            var rows = _webDriver.FindElements(By.CssSelector("main table tbody tr")).ToList();
            progress.Report($"Найдено пользователей: {rows.Count}");

            // Сохраняем URL'ы пользователей
            var userUrls = new List<(string userName, string userUrl)>();
            
            foreach (var row in rows)
            {
                try
                {
                    var linkElem = row.FindElement(By.CssSelector("td:nth-child(2) a"));
                    var userName = linkElem.Text.Trim().TrimStart('@');
                    var userUrl = linkElem.GetAttribute("href");
                    userUrls.Add((userName, userUrl));
                }
                catch { }
            }

            // Теперь обрабатываем каждого пользователя в той же вкладке
            for (int i = 0; i < userUrls.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var (userName, userUrl) = userUrls[i];
                
                try
                {
                    progress.Report($"[User {i + 1}/{userUrls.Count}] {userName}");

                    var userDir = Path.Combine(root, userName);
                    Directory.CreateDirectory(userDir);

                    // Переходим на страницу пользователя в той же вкладке
                    _webDriver.NavigateTo(userUrl);
                    await Task.Delay(10, cancellationToken);

                    await DownloadUserCharactersAsync(userDir, progress, cancellationToken);

                    // Возвращаемся обратно на страницу лидерборда
                    _webDriver.NavigateTo("https://chub.ai/leaderboard?segment=followers");
                    await Task.Delay(10, cancellationToken);
                }
                catch (Exception ex)
                {
                    progress.Report($"Ошибка обработки пользователя: {ex.Message}");
                }
            }
        }

        public async Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var segmentName = segment.ToString().ToLower();
            var root = Path.Combine(Environment.CurrentDirectory, $"characters");
            Directory.CreateDirectory(root);

            // Параметры для контроля проверки чатов
            bool checkChatCount = minChats > 0;

            int endPage = startPage + pagesToScan - 1;
            progress.Report($"Начинаем сканирование с страницы {startPage} по {endPage}");

            for (int page = startPage; page <= endPage; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                progress.Report($"Страница {page}/{endPage} (осталось: {endPage - page + 1})");

                string url = $"https://chub.ai/?segment={segmentName}&page={page}";
                _webDriver.NavigateTo(url);

                if (!_webDriver.WaitForElement(By.CssSelector(CHARACTER_LIST_SELECTOR)))
                {
                    progress.Report($"Персонажи не найдены на странице {page}");
                    continue;
                }

                var cards = _webDriver.FindElements(By.CssSelector(CHARACTER_LIST_SELECTOR)).ToList();
                
                // Получаем информацию о всех персонажах на странице
                var characterInfos = new List<(string href, string id, int chatCount)>();
                
                foreach (var card in cards)
                {
                    try
                    {
                        var href = card.GetAttribute("href");
                        var id = href.TrimEnd('/').Split('/').Last();
                        
                        var chatCount = checkChatCount ? GetChatCount(card) : int.MaxValue;
                        
                        if (chatCount >= minChats && !IsCharacterExists(id, out _))
                        {
                            characterInfos.Add((href, id, chatCount));
                        }
                    }
                    catch { }
                }

                // Теперь обрабатываем каждого персонажа в той же вкладке
                foreach (var (href, id, chatCount) in characterInfos)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        progress.Report($"{id} (чатов: {chatCount})");

                        _webDriver.NavigateTo(href);
                        await Task.Delay(10, cancellationToken);

                        var downloaded = await DownloadCharacterJsonAsync(root, id);
                        if (downloaded)
                        {
                            var filePath = Path.Combine(root, id + ".json");
                            RegisterCharacter(id, filePath);
                        }

                        // Возвращаемся на страницу списка
                        _webDriver.NavigateTo(url);
                        await Task.Delay(10, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        progress.Report($"Ошибка: {ex.Message}");
                    }
                }
            }
        }

        private async Task DownloadUserCharactersAsync(string userDir, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var charTab = _webDriver.FindElements(By.CssSelector("div[role='tab']"))
                                   .FirstOrDefault(t => t.Text.Trim().Equals("Characters", StringComparison.OrdinalIgnoreCase));
            charTab?.Click();

            if (!_webDriver.WaitForElement(By.CssSelector(CHARACTER_LIST_SELECTOR)))
            {
                progress.Report("Нет персонажей у пользователя");
                return;
            }

            int pageNum = 1;
            bool hasNext = true;
            var allCharacterUrls = new List<(string href, string id)>();

            // Сначала собираем все URL'ы персонажей со всех страниц
            while (hasNext && !cancellationToken.IsCancellationRequested)
            {
                var cards = _webDriver.FindElements(By.CssSelector(CHARACTER_LIST_SELECTOR)).ToList();
                progress.Report($"Страница {pageNum}: {cards.Count} персонажей");

                foreach (var card in cards)
                {
                    var href = card.GetAttribute("href");
                    var id = href.TrimEnd('/').Split('/').Last();

                    if (!IsCharacterExists(id, out _))
                    {
                        allCharacterUrls.Add((href, id));
                    }
                }

                hasNext = GoToNextPage();
                if (hasNext) pageNum++;
            }

            // Теперь скачиваем персонажей, возвращаясь на нужную страницу пользователя
            var currentUserUrl = _webDriver.Driver.Url;
            
            foreach (var (href, id) in allCharacterUrls)
            {
                if (cancellationToken.IsCancellationRequested) break;

                _webDriver.NavigateTo(href);
                await Task.Delay(10, cancellationToken);

                var downloaded = await DownloadCharacterJsonAsync(userDir, id);
                if (downloaded)
                {
                    var filePath = Path.Combine(userDir, id + ".json");
                    RegisterCharacter(id, filePath);
                }

                // Возвращаемся на страницу пользователя
                _webDriver.NavigateTo(currentUserUrl);
                await Task.Delay(10, cancellationToken);
            }
        }

        private async Task<bool> DownloadCharacterJsonAsync(string targetDir, string characterId)
        {
            try
            {
                await Task.Delay(10); // Ждем загрузки страницы

                _downloadService.ClearOldFiles(_downloadPath, ".json");

                var wait = new WebDriverWait(_webDriver.Driver, TimeSpan.FromSeconds(10));
                var jsonBtn = wait.Until(_ => TryFindJsonButton());

                if (jsonBtn != null)
                {
                    jsonBtn.Click();
                    return _downloadService.WaitForFileDownload(_downloadPath, targetDir, characterId, ".json");
                }
                
                Console.WriteLine($"JSON-кнопка не найдена для {characterId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки JSON: {ex.Message}");
            }

            return false;
        }

        private IWebElement? TryFindJsonButton()
        {
            foreach (var xpath in JSON_BUTTON_XPATHS)
            {
                try
                {
                    var element = _webDriver.Driver.FindElement(By.XPath(xpath));
                    if (element.Displayed && element.Enabled)
                        return element;
                }
                catch
                {
                    // ignored
                }
            }

            return null;
        }

        private int GetChatCount(IWebElement card)
        {
            try
            {
                var actions = new Actions(_webDriver.Driver);
                var iconBlocks = card.FindElements(By.CssSelector("span.fake-ribbon > div")).ToList();
                if (iconBlocks.Count == 0) return 0;

                actions.MoveToElement(iconBlocks[0]).Perform();
                Thread.Sleep(10); // Даем время на появление tooltip

                var tooltip = _webDriver.Driver.FindElements(By.CssSelector(".ant-tooltip-inner"))
                    .FirstOrDefault(el => el.Displayed);

                if (tooltip != null)
                {
                    return ParseChatCount(tooltip.Text);
                }
            }
            catch { }

            return 0;
        }

        private int ParseChatCount(string text)
        {
            var parts = text.Split(',');
            var chatPart = parts.FirstOrDefault(p => p.Contains("chats"));
            if (chatPart == null) return 0;

            var numStr = chatPart.Replace("Total:", "")
                .Replace("chats", "")
                .Trim()
                .ToLower();

            if (numStr.EndsWith("k"))
            {
                var val = double.Parse(numStr.TrimEnd('k'), System.Globalization.CultureInfo.InvariantCulture);
                return (int)(val * 1000);
            }

            if (int.TryParse(numStr, out var result))
                return result;

            return 0;
        }

        private bool GoToNextPage()
        {
            try
            {
                var nextButton = _webDriver.FindElements(By.XPath(NEXT_PAGE_XPATH)).FirstOrDefault() ??
                                 _webDriver.FindElements(By.CssSelector(".ant-pagination-next[title='Next Page']")).FirstOrDefault();

                if (nextButton != null && nextButton.GetAttribute("aria-disabled") != "true")
                {
                    ((IJavaScriptExecutor)_webDriver.Driver).ExecuteScript("arguments[0].click();", nextButton);
                    Thread.Sleep(100);
                    return true;
                }
            }
            catch { }

            return false;
        }
    }
}
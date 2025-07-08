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

            for (int i = 0; i < rows.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                var row = rows[i];
                try
                {
                    var linkElem = row.FindElement(By.CssSelector("td:nth-child(2) a"));
                    var userName = linkElem.Text.Trim().TrimStart('@');
                    var userUrl = linkElem.GetAttribute("href");

                    progress.Report($"[User {i + 1}/{rows.Count}] {userName}");

                    var userDir = Path.Combine(root, userName);
                    Directory.CreateDirectory(userDir);

                    _webDriver.OpenNewTab(userUrl);
                    await Task.Delay(150, cancellationToken);
                    _webDriver.SwitchToLastTab();

                    await DownloadUserCharactersAsync(userDir, progress, cancellationToken);

                    _webDriver.CloseCurrentTab();
                    _webDriver.SwitchToMainWindow();
                }
                catch (Exception ex)
                {
                    progress.Report($"Ошибка обработки пользователя: {ex.Message}");
                    _webDriver.SwitchToMainWindow();
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
            int batchSize = checkChatCount ? 5 : 20; // Меньше батч если проверяем чаты

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
                
                // Получаем информацию о чатах для всех карточек за один проход
                var cardsWithChatCount = new List<(IWebElement card, string href, string id, int chatCount)>();
                
                if (checkChatCount)
                {
                    progress.Report($"Получение информации о чатах для {cards.Count} персонажей...");
                    cardsWithChatCount = await GetCardsWithChatCountBatchAsync(cards, cancellationToken);
                }
                else
                {
                    // Без проверки чатов
                    cardsWithChatCount = cards.Select(card => 
                    {
                        var href = card.GetAttribute("href");
                        var id = href.TrimEnd('/').Split('/').Last();
                        return (card, href, id, int.MaxValue);
                    }).ToList();
                }

                // Фильтруем и скачиваем
                foreach (var (card, href, id, chatCount) in cardsWithChatCount)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    try
                    {
                        if (chatCount < minChats) continue;

                        // Проверяем глобальный индекс
                        if (IsCharacterExists(id, out var existingPath))
                        {
                            progress.Report($"{id} уже существует в {existingPath}");
                            continue;
                        }

                        progress.Report($"{id} (чатов: {chatCount})");

                        _webDriver.OpenNewTab(href);
                        _webDriver.SwitchToLastTab();

                        var downloaded = await DownloadCharacterJsonAsync(root, id);
                        if (downloaded)
                        {
                            var filePath = Path.Combine(root, id + ".json");
                            RegisterCharacter(id, filePath);
                        }

                        _webDriver.CloseCurrentTab();
                        _webDriver.SwitchToMainWindow();
                    }
                    catch (Exception ex)
                    {
                        progress.Report($"Ошибка: {ex.Message}");
                    }
                }
            }
        }

        private async Task<List<(IWebElement card, string href, string id, int chatCount)>> GetCardsWithChatCountBatchAsync(
            List<IWebElement> cards, CancellationToken cancellationToken)
        {
            var result = new List<(IWebElement, string, string, int)>();
            
            // Получаем все чаты одним скриптом
            var jsScript = @"
                return Array.from(arguments[0]).map(card => {
                    try {
                        const href = card.getAttribute('href');
                        const id = href.split('/').filter(p => p).pop();
                        const iconBlock = card.querySelector('span.fake-ribbon > div');
                        
                        if (!iconBlock) return { href, id, chatCount: 0 };
                        
                        // Симулируем hover
                        iconBlock.dispatchEvent(new MouseEvent('mouseenter', { bubbles: true }));
                        
                        // Даем время на появление tooltip
                        return new Promise(resolve => {
                            setTimeout(() => {
                                const tooltips = document.querySelectorAll('.ant-tooltip-inner');
                                let chatCount = 0;
                                
                                for (const tooltip of tooltips) {
                                    if (tooltip.style.display !== 'none' && tooltip.offsetParent !== null) {
                                        const text = tooltip.textContent || '';
                                        const match = text.match(/(\d+(?:\.\d+)?k?)\s*chats/i);
                                        if (match) {
                                            const numStr = match[1].toLowerCase();
                                            if (numStr.endsWith('k')) {
                                                chatCount = Math.floor(parseFloat(numStr.slice(0, -1)) * 1000);
                                            } else {
                                                chatCount = parseInt(numStr);
                                            }
                                            break;
                                        }
                                    }
                                }
                                
                                iconBlock.dispatchEvent(new MouseEvent('mouseleave', { bubbles: true }));
                                resolve({ href, id, chatCount });
                            }, 300);
                        });
                    } catch (e) {
                        return { href: '', id: '', chatCount: 0 };
                    }
                });
            ";

            try
            {
                var cardsArray = cards.ToArray();
                var data = await Task.Run(() => 
                    ((IJavaScriptExecutor)_webDriver.Driver).ExecuteAsyncScript(jsScript, cardsArray));
                
                if (data is IEnumerable<object> results)
                {
                    int index = 0;
                    foreach (var item in results)
                    {
                        if (item is Dictionary<string, object> dict)
                        {
                            var href = dict.GetValueOrDefault("href")?.ToString() ?? "";
                            var id = dict.GetValueOrDefault("id")?.ToString() ?? "";
                            var chatCount = Convert.ToInt32(dict.GetValueOrDefault("chatCount") ?? 0);
                            
                            if (!string.IsNullOrEmpty(id))
                            {
                                result.Add((cards[index], href, id, chatCount));
                            }
                        }
                        index++;
                    }
                }
            }
            catch
            {
                // Фоллбэк на старый метод если JS не сработал
                foreach (var card in cards)
                {
                    var href = card.GetAttribute("href");
                    var id = href.TrimEnd('/').Split('/').Last();
                    var chatCount = GetChatCount(card);
                    result.Add((card, href, id, chatCount));
                }
            }

            return result;
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

            while (hasNext && !cancellationToken.IsCancellationRequested)
            {
                var cards = _webDriver.FindElements(By.CssSelector(CHARACTER_LIST_SELECTOR)).ToList();
                progress.Report($"Страница {pageNum}: {cards.Count} персонажей");

                foreach (var card in cards)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    var href = card.GetAttribute("href");
                    var id = href.TrimEnd('/').Split('/').Last();

                    // Проверяем глобальный индекс
                    if (IsCharacterExists(id, out var existingPath))
                    {
                        progress.Report($"{id} уже существует в {existingPath}");
                        continue;
                    }

                    _webDriver.OpenNewTab(href);
                    await Task.Delay(10, cancellationToken);
                    _webDriver.SwitchToLastTab();

                    var downloaded = await DownloadCharacterJsonAsync(userDir, id);
                    if (downloaded)
                    {
                        var filePath = Path.Combine(userDir, id + ".json");
                        RegisterCharacter(id, filePath);
                    }

                    _webDriver.CloseCurrentTab();
                    _webDriver.SwitchToMainWindow();
                }

                hasNext = GoToNextPage();
                if (hasNext) pageNum++;
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
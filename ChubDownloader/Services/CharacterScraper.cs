using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using ChubDownloader.Models;

namespace ChubDownloader.Services
{
    public class CharacterScraper : ICharacterScraper
    {
        private readonly IWebDriverService _webDriver;
        private readonly IDownloadService _downloadService;
        private readonly string _downloadPath;
        
        // XPath константы
        private const string DOWNLOAD_BUTTON_XPATH = "//*[@id='root']/div/div/div/main/div/div[1]/div[1]/div[2]/div/button[1]";
        private const string JSON_BUTTON_XPATH = "//*[@id='root']/div/div/div/main/div/div[1]/div[1]/div[2]/div/button[2]";
        private const string CHARACTER_LIST_SELECTOR = "#chara-list > a.cursor-pointer";
        private const string NEXT_PAGE_XPATH = "//*[@id='rc-tabs-1-panel-characters']/ul[1]/li[@title='Next Page']";
        
        public CharacterScraper(IWebDriverService webDriver, IDownloadService downloadService, string downloadPath)
        {
            _webDriver = webDriver;
            _downloadService = downloadService;
            _downloadPath = downloadPath;
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
                    
                    progress.Report($"[User {i+1}/{rows.Count}] {userName}");
                    
                    var userDir = Path.Combine(root, userName);
                    Directory.CreateDirectory(userDir);
                    
                    _webDriver.OpenNewTab(userUrl);
                    Thread.Sleep(150);
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
        
        public async Task DownloadFromSegmentAsync(Segment segment, int minChats, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken)
        {
            var segmentName = segment.ToString().ToLower();
            var root = Path.Combine(Environment.CurrentDirectory, $"characters_{segmentName}");
            Directory.CreateDirectory(root);
            
            for (int page = 1; page <= pagesToScan; page++)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                progress.Report($"Страница {page}/{pagesToScan}");
                
                string url = $"https://chub.ai/?segment={segmentName}&page={page}";
                _webDriver.NavigateTo(url);
                
                if (!_webDriver.WaitForElement(By.CssSelector(CHARACTER_LIST_SELECTOR)))
                {
                    progress.Report("Персонажи не найдены на странице");
                    break;
                }
                
                var cards = _webDriver.FindElements(By.CssSelector(CHARACTER_LIST_SELECTOR)).ToList();
                
                foreach (var card in cards)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        var chatCount = GetChatCount(card);
                        if (chatCount < minChats) continue;
                        
                        var href = card.GetAttribute("href");
                        var id = href.TrimEnd('/').Split('/').Last();
                        var jsonFile = Path.Combine(root, id + ".json");
                        
                        if (File.Exists(jsonFile)) continue;
                        
                        progress.Report($"{id} (чатов: {chatCount})");
                        
                        _webDriver.OpenNewTab(href);
                        Thread.Sleep(150);
                        _webDriver.SwitchToLastTab();
                        
                        await DownloadCharacterJsonAsync(root, id);
                        
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
        
        private async Task DownloadUserCharactersAsync(string userDir, IProgress<string> progress, CancellationToken cancellationToken)
        {
            // Переключаемся на вкладку Characters
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
                    var jsonFile = Path.Combine(userDir, id + ".json");
                    
                    if (File.Exists(jsonFile)) continue;
                    
                    _webDriver.OpenNewTab(href);
                    Thread.Sleep(150);
                    _webDriver.SwitchToLastTab();
                    
                    await DownloadCharacterJsonAsync(userDir, id);
                    
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
                Thread.Sleep(500); // Ждем загрузки страницы
                
                var wait = new WebDriverWait(_webDriver.Driver, TimeSpan.FromSeconds(10));
                var jsonBtn = wait.Until(d =>
                {
                    var btn = d.FindElement(By.XPath(JSON_BUTTON_XPATH));
                    return (btn.Displayed && btn.Enabled) ? btn : null;
                });
                
                if (jsonBtn != null)
                {
                    _downloadService.ClearOldFiles(_downloadPath, ".json");
                    jsonBtn.Click();
                    
                    return _downloadService.WaitForFileDownload(_downloadPath, targetDir, characterId, ".json");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки JSON: {ex.Message}");
            }
            
            return false;
        }
        
        private int GetChatCount(IWebElement card)
        {
            try
            {
                var actions = new Actions(_webDriver.Driver);
                var iconBlocks = card.FindElements(By.CssSelector("span.fake-ribbon > div")).ToList();
                if (iconBlocks.Count == 0) return 0;
                
                actions.MoveToElement(iconBlocks[0]).Perform();
                Thread.Sleep(300);
                
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
                    Thread.Sleep(400);
                    return true;
                }
            }
            catch { }
            
            return false;
        }
    }
}

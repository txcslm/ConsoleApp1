using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using ChubDownloader.Models;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Infrastructure.FileSystem;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using System.Collections.Concurrent;
using System.Buffers;

namespace ChubDownloader.Services;

public sealed class CharacterScraper : ICharacterScraper
{
    private readonly IWebDriverService _webDriver;
    private readonly IDownloadService _downloadService;
    private readonly IFileSystemService _fileSystemService;
    private readonly string _downloadPath;
    private readonly ConcurrentDictionary<string, string> _globalCharacterIndex;
    private readonly string _indexFilePath;
    private readonly SemaphoreSlim _indexSemaphore;
    
    public CharacterScraper(IWebDriverService webDriver, IDownloadService downloadService, IFileSystemService fileSystemService, string downloadPath)
    {
        _webDriver = webDriver;
        _downloadService = downloadService;
        _fileSystemService = fileSystemService;
        _downloadPath = downloadPath;
        _indexFilePath = Path.Combine(Environment.CurrentDirectory, AppSettings.CharacterIndexFileName);
        _indexSemaphore = new SemaphoreSlim(1, 1);
        _globalCharacterIndex = LoadCharacterIndexAsync().GetAwaiter().GetResult();
    }
    
    private async Task<ConcurrentDictionary<string, string>> LoadCharacterIndexAsync()
    {
        return await _fileSystemService.LoadCharacterIndexAsync(_indexFilePath);
    }
    
    private async Task SaveCharacterIndexAsync()
    {
        await _indexSemaphore.WaitAsync();
        try
        {
            await _fileSystemService.SaveCharacterIndexAsync(_indexFilePath, _globalCharacterIndex);
        }
        finally
        {
            _indexSemaphore.Release();
        }
    }
    
    private bool IsCharacterExists(string characterId, out string? existingPath)
    {
        return _globalCharacterIndex.TryGetValue(characterId, out existingPath);
    }
    
    private async Task RegisterCharacterAsync(string characterId, string filePath)
    {
        _globalCharacterIndex[characterId] = filePath;
        await SaveCharacterIndexAsync();
    }
    
    public async Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        var root = Path.Combine(Environment.CurrentDirectory, AppSettings.FollowersFolderName);
        Directory.CreateDirectory(root);
        
        _webDriver.NavigateTo(WebDriverSettings.LeaderboardUrl);
        _webDriver.WaitForElement(By.CssSelector(WebDriverSettings.MainTableRowSelector));
        
        var rows = _webDriver.FindElements(By.CssSelector(WebDriverSettings.MainTableRowSelector)).ToListOptimized();
        progress.Report($"Найдено пользователей: {rows.Count}");
        
        var userUrls = ExtractUserUrls(rows);
        
        await ProcessUsersAsync(userUrls, root, progress, cancellationToken);
    }
    
    private List<(string userName, string userUrl)> ExtractUserUrls(IList<IWebElement> rows)
    {
        var userUrls = new List<(string, string)>(rows.Count);
        
        foreach (var row in rows)
        {
            try
            {
                var linkElem = row.FindElement(By.CssSelector(WebDriverSettings.UserLinkSelector));
                var userName = linkElem.Text.Trim().TrimStart('@');
                var userUrl = linkElem.GetAttribute("href");
                if (!string.IsNullOrEmpty(userUrl))
                {
                    userUrls.Add((userName, userUrl));
                }
            }
            catch
            {
                // Ignore invalid rows
            }
        }
        
        return userUrls;
    }
    
    private async Task ProcessUsersAsync(List<(string userName, string userUrl)> userUrls, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        for (int i = 0; i < userUrls.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            var (userName, userUrl) = userUrls[i];
            
            try
            {
                progress.Report($"[User {i + 1}/{userUrls.Count}] {userName}");
                
                var userDir = Path.Combine(root, userName);
                Directory.CreateDirectory(userDir);
                
                _webDriver.NavigateTo(userUrl);
                await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
                
                await DownloadUserCharactersAsync(userDir, progress, cancellationToken);
                
                _webDriver.NavigateTo(WebDriverSettings.LeaderboardUrl);
                await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
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
        var root = Path.Combine(Environment.CurrentDirectory, AppSettings.Characters2FolderName);
        Directory.CreateDirectory(root);
        
        var checkChatCount = minChats > 0;
        var endPage = startPage + pagesToScan - 1;
        
        progress.Report($"Начинаем сканирование с страницы {startPage} по {endPage}");
        
        for (int page = startPage; page <= endPage; page++)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            progress.Report($"Страница {page}/{endPage} (осталось: {endPage - page + 1})");
            
            var url = $"{WebDriverSettings.BaseUrl}/?segment={segmentName}&page={page}";
            _webDriver.NavigateTo(url);
            
            if (!_webDriver.WaitForElement(By.CssSelector(WebDriverSettings.CharacterListSelector)))
            {
                progress.Report($"Персонажи не найдены на странице {page}");
                continue;
            }
            
            await ProcessCharactersOnPageAsync(url, minChats, checkChatCount, root, progress, cancellationToken);
        }
    }
    
    private async Task ProcessCharactersOnPageAsync(string pageUrl, int minChats, bool checkChatCount, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var cards = _webDriver.FindElements(By.CssSelector(WebDriverSettings.CharacterListSelector)).ToListOptimized();
        var characterInfos = new List<(string href, string id, int chatCount)>(cards.Count);
        
        foreach (var card in cards)
        {
            try
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();
                
                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id))
                {
                    var chatCount = checkChatCount ? GetChatCount(card) : int.MaxValue;
                    
                    if (chatCount >= minChats && !IsCharacterExists(id, out _))
                    {
                        characterInfos.Add((href, id, chatCount));
                    }
                }
            }
            catch
            {
                // Ignore invalid cards
            }
        }
        
        await ProcessCharactersAsync(characterInfos, pageUrl, root, progress, cancellationToken);
    }
    
    private async Task ProcessCharactersAsync(List<(string href, string id, int chatCount)> characterInfos, string pageUrl, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        foreach (var (href, id, chatCount) in characterInfos)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            try
            {
                progress.Report($"{id} (чатов: {chatCount})");
                
                _webDriver.NavigateTo(href);
                await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
                
                if (await DownloadCharacterJsonAsync(root, id))
                {
                    var filePath = Path.Combine(root, id + AppSettings.JsonExtension);
                    await RegisterCharacterAsync(id, filePath);
                }
                
                _webDriver.NavigateTo(pageUrl);
                await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
            }
            catch (Exception ex)
            {
                progress.Report($"Ошибка: {ex.Message}");
            }
        }
    }
    
    private async Task DownloadUserCharactersAsync(string userDir, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var charTab = _webDriver.FindElements(By.CssSelector(WebDriverSettings.CharacterTabSelector))
            .FirstOrDefault(t => t.Text.Trim().Equals("Characters", StringComparison.OrdinalIgnoreCase));
        charTab?.Click();
        
        if (!_webDriver.WaitForElement(By.CssSelector(WebDriverSettings.CharacterListSelector)))
        {
            progress.Report("Нет персонажей у пользователя");
            return;
        }
        
        var allCharacterUrls = await CollectAllCharacterUrlsAsync(progress, cancellationToken);
        var currentUserUrl = _webDriver.Driver.Url;
        
        await DownloadCollectedCharactersAsync(allCharacterUrls, userDir, currentUserUrl, cancellationToken);
    }
    
    private Task<List<(string href, string id)>> CollectAllCharacterUrlsAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        var allCharacterUrls = new List<(string href, string id)>();
        int pageNum = 1;
        bool hasNext = true;
        
        while (hasNext && !cancellationToken.IsCancellationRequested)
        {
            var cards = _webDriver.FindElements(By.CssSelector(WebDriverSettings.CharacterListSelector)).ToListOptimized();
            progress.Report($"Страница {pageNum}: {cards.Count} персонажей");
            
            foreach (var card in cards)
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();
                
                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id) && !IsCharacterExists(id, out _))
                {
                    allCharacterUrls.Add((href, id));
                }
            }
            
            hasNext = GoToNextPage();
            if (hasNext) pageNum++;
        }
        
        return Task.FromResult(allCharacterUrls);
    }
    
    private async Task DownloadCollectedCharactersAsync(List<(string href, string id)> allCharacterUrls, string userDir, string currentUserUrl, CancellationToken cancellationToken)
    {
        foreach (var (href, id) in allCharacterUrls)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            _webDriver.NavigateTo(href);
            await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
            
            if (await DownloadCharacterJsonAsync(userDir, id))
            {
                var filePath = Path.Combine(userDir, id + AppSettings.JsonExtension);
                await RegisterCharacterAsync(id, filePath);
            }
            
            _webDriver.NavigateTo(currentUserUrl);
            await Task.Delay(AppSettings.TaskDelayMs, cancellationToken);
        }
    }
    
    private async Task<bool> DownloadCharacterJsonAsync(string targetDir, string characterId)
    {
        try
        {
            await Task.Delay(AppSettings.TaskDelayMs);
            
            _downloadService.ClearOldFiles(_downloadPath, AppSettings.JsonExtension);
            
            var wait = new WebDriverWait(_webDriver.Driver, TimeSpan.FromSeconds(AppSettings.WebDriverTimeoutSeconds));
            var jsonBtn = wait.Until(_ => TryFindJsonButton());
            
            if (jsonBtn != null)
            {
                jsonBtn.Click();
                return await _downloadService.WaitForFileDownloadAsync(_downloadPath, targetDir, characterId, AppSettings.JsonExtension);
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
        foreach (var xpath in WebDriverSettings.JsonButtonXPaths)
        {
            try
            {
                var element = _webDriver.Driver.FindElement(By.XPath(xpath));
                if (element.Displayed && element.Enabled)
                    return element;
            }
            catch
            {
                // Ignore
            }
        }
        
        return null;
    }
    
    private int GetChatCount(IWebElement card)
    {
        try
        {
            var actions = new Actions(_webDriver.Driver);
            var iconBlocks = card.FindElements(By.CssSelector(WebDriverSettings.IconBlockSelector)).ToListOptimized();
            if (iconBlocks.Count == 0) return 0;
            
            actions.MoveToElement(iconBlocks[0]).Perform();
            Thread.Sleep(AppSettings.TooltipDelayMs);
            
            var tooltip = _webDriver.Driver.FindElements(By.CssSelector(WebDriverSettings.AntTooltipInnerSelector))
                .FirstOrDefault(el => el.Displayed);
            
            if (tooltip != null)
            {
                return tooltip.Text.ParseChatCount();
            }
        }
        catch
        {
            // Ignore
        }
        
        return 0;
    }
    
    private bool GoToNextPage()
    {
        try
        {
            var nextButton = _webDriver.FindElements(By.XPath(WebDriverSettings.NextPageXPath)).FirstOrDefault() ??
                             _webDriver.FindElements(By.CssSelector(WebDriverSettings.AntPaginationNextSelector)).FirstOrDefault();
            
            if (nextButton != null && nextButton.GetAttribute("aria-disabled") != "true")
            {
                ((IJavaScriptExecutor)_webDriver.Driver).ExecuteScript("arguments[0].click();", nextButton);
                Thread.Sleep(AppSettings.ThreadSleepMs);
                return true;
            }
        }
        catch
        {
            // Ignore
        }
        
        return false;
    }
}
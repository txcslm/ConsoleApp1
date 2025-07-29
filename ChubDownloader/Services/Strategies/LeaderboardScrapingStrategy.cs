using OpenQA.Selenium;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Services.Strategies;

public sealed class LeaderboardScrapingStrategy : IScrapingStrategy
{
    private readonly IWebDriverService _webDriverService;
    private readonly IWebElementExtractor _elementExtractor;
    private readonly INavigationService _navigationService;
    private readonly IProgressReporter _progressReporter;
    private readonly ICharacterIndexManager _indexManager;
    private readonly IDownloadService _downloadService;
    private readonly string _downloadPath;

    public LeaderboardScrapingStrategy(
        IWebDriverService webDriverService,
        IWebElementExtractor elementExtractor,
        INavigationService navigationService,
        IProgressReporter progressReporter,
        ICharacterIndexManager indexManager,
        IDownloadService downloadService,
        string downloadPath)
    {
        _webDriverService = webDriverService;
        _elementExtractor = elementExtractor;
        _navigationService = navigationService;
        _progressReporter = progressReporter;
        _indexManager = indexManager;
        _downloadService = downloadService;
        _downloadPath = downloadPath;
    }

    public async Task ExecuteAsync(ScrapingParameters parameters, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var root = Path.Combine(Environment.CurrentDirectory, AppSettings.FollowersFolderName);
        Directory.CreateDirectory(root);

        await _navigationService.NavigateToAsync(WebDriverSettings.LeaderboardUrl);

        if (!await _navigationService.WaitForElementAsync(By.CssSelector(WebDriverSettings.MainTableRowSelector)))
        {
            _progressReporter.ReportError(progress, "Не удалось загрузить таблицу лидерборда");
            return;
        }

        var rows = _webDriverService.FindElements(By.CssSelector(WebDriverSettings.MainTableRowSelector)).ToListOptimized();
        _progressReporter.ReportProgress(progress, $"Найдено пользователей: {rows.Count}");

        var userUrls = _elementExtractor.ExtractUserUrls(rows);

        await ProcessUsersAsync(userUrls, root, progress, cancellationToken);
    }

    private async Task ProcessUsersAsync(List<(string userName, string userUrl)> userUrls, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        for (int i = 0; i < userUrls.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var (userName, userUrl) = userUrls[i];

            try
            {
                _progressReporter.ReportUserProgress(progress, i + 1, userUrls.Count, userName);

                var userDir = Path.Combine(root, userName);
                Directory.CreateDirectory(userDir);

                await _navigationService.NavigateToAsync(userUrl);
                await DownloadUserCharactersAsync(userDir, progress, cancellationToken);
                await _navigationService.NavigateToAsync(WebDriverSettings.LeaderboardUrl);
            }
            catch (Exception ex)
            {
                StringBuilderLogger.LogError($"Ошибка обработки пользователя {userName}: {ex.Message}", ex);
                _progressReporter.ReportError(progress, $"Ошибка обработки пользователя {userName}: {ex.Message}");
            }
        }
    }

    private async Task DownloadUserCharactersAsync(string userDir, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var charTab = _webDriverService.FindElements(By.CssSelector(WebDriverSettings.CharacterTabSelector))
            .FirstOrDefault(t => t.Text.Trim().Equals("Characters", StringComparison.OrdinalIgnoreCase));
        charTab?.Click();

        if (!await _navigationService.WaitForElementAsync(By.CssSelector(WebDriverSettings.CharacterListSelector)))
        {
            _progressReporter.ReportProgress(progress, "Нет персонажей у пользователя");
            return;
        }

        var allCharacterUrls = await CollectAllCharacterUrlsAsync(progress, cancellationToken);
        var currentUserUrl = _webDriverService.Driver.Url;

        if (allCharacterUrls.Count == 0)
        {
            _progressReporter.ReportProgress(progress, "Нет персонажей для загрузки");
            return;
        }

        await DownloadCollectedCharactersAsync(allCharacterUrls, userDir, currentUserUrl, cancellationToken);
    }

    private async Task<List<(string href, string id)>> CollectAllCharacterUrlsAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        var allCharacterUrls = new List<(string href, string id)>();
        int pageNum = 1;
        bool hasNext = true;

        while (hasNext && !cancellationToken.IsCancellationRequested)
        {
            var cards = _webDriverService.FindElements(By.CssSelector(WebDriverSettings.CharacterListSelector)).ToListOptimized();
            _progressReporter.ReportProgress(progress, $"Страница {pageNum}: {cards.Count} персонажей");

            var characterUrls = await _elementExtractor.ExtractCharacterUrlsAsync(cards, _indexManager);
            allCharacterUrls.AddRange(characterUrls);

            hasNext = _navigationService.GoToNextPage();
            if (hasNext) pageNum++;
        }

        return allCharacterUrls;
    }

    private async Task DownloadCollectedCharactersAsync(List<(string href, string id)> allCharacterUrls, string userDir, string currentUserUrl, CancellationToken cancellationToken)
    {
        foreach (var (href, id) in allCharacterUrls)
        {
            if (cancellationToken.IsCancellationRequested) break;
            
            // Проверяем, не является ли персонаж дубликатом
            if (await _indexManager.IsCharacterExistsAsync(id))
            {
                StringBuilderLogger.WriteDuplicateInfo(id);
                StringBuilderLogger.LogInfo($"{id} пропущен - уже существует");
                continue;
            }

            await _navigationService.NavigateToAsync(href);

            if (await DownloadCharacterJsonAsync(userDir, id))
            {
                var filePath = Path.Combine(userDir, id + AppSettings.JsonExtension);
                await _indexManager.RegisterCharacterAsync(id, filePath);
            }

            await _navigationService.NavigateToAsync(currentUserUrl);
        }
    }

    private async Task<bool> DownloadCharacterJsonAsync(string targetDir, string characterId)
    {
        try
        {
            await _navigationService.DelayAsync();

            _downloadService.ClearOldFiles(_downloadPath, AppSettings.JsonExtension);

            var jsonBtn = _elementExtractor.TryFindJsonButton();
            if (jsonBtn != null)
            {
                jsonBtn.Click();
                return await _downloadService.WaitForFileDownloadAsync(_downloadPath, targetDir, characterId, AppSettings.JsonExtension);
            }

            StringBuilderLogger.LogWarning($"JSON-кнопка не найдена для {characterId}");
        }
        catch (Exception ex)
        {
            StringBuilderLogger.LogError($"Ошибка загрузки JSON для {characterId}: {ex.Message}", ex);
        }

        return false;
    }
}
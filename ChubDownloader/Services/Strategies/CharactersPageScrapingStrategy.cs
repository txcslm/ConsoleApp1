using OpenQA.Selenium;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;

namespace ChubDownloader.Services.Strategies;

public sealed class CharactersPageScrapingStrategy : IScrapingStrategy
{
    private readonly IWebDriverService _webDriverService;
    private readonly IWebElementExtractor _elementExtractor;
    private readonly INavigationService _navigationService;
    private readonly IProgressReporter _progressReporter;
    private readonly ICharacterIndexManager _indexManager;
    private readonly IDownloadService _downloadService;
    private readonly string _downloadPath;

    public CharactersPageScrapingStrategy(
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
        var root = Path.Combine(Environment.CurrentDirectory, AppSettings.Characters5FolderName);
        Directory.CreateDirectory(root);

        var checkChatCount = parameters.MinChats > 0;
        var endPage = parameters.StartPage + parameters.PagesToScan - 1;

        _progressReporter.ReportProgress(progress, $"Начинаем сканирование персонажей с страницы {parameters.StartPage} по {endPage}");

        for (int page = parameters.StartPage; page <= endPage; page++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _progressReporter.ReportPageProgress(progress, page, endPage);

            var url = BuildCharactersPageUrl(page);
            await _navigationService.NavigateToAsync(url);

            if (!await _navigationService.WaitForElementAsync(By.CssSelector(WebDriverSettings.CharactersPageSelector)))
            {
                _progressReporter.ReportProgress(progress, $"Персонажи не найдены на странице {page}");
                continue;
            }

            try
            {
                await ProcessCharactersOnPageAsync(url, parameters.MinChats, checkChatCount, root, progress, cancellationToken);
            }
            catch (Exception ex)
            {
                _progressReporter.ReportError(progress, $"Ошибка обработки страницы {page}: {ex.Message}");
            }
        }
    }

    private static string BuildCharactersPageUrl(int page)
    {
        return $"{WebDriverSettings.BaseUrl}/characters?excludetopics=&first=20&page={page}&namespace=characters&search=&include_forks=true&nsfw=true&nsfw_only=false&require_custom_prompt=false&require_example_dialogues=false&require_images=false&require_expressions=false&nsfl=true&asc=false&min_ai_rating=0&min_tokens=50&max_tokens=100000&chub=true&require_lore=false&exclude_mine=false&require_lore_embedded=false&require_lore_linked=false&language=&sort=star_count&min_tags=2&topics=&special_mode=&name_like=&only_mine=&inclusive_or=false&recommended_verified=false&require_alternate_greetings=false&max_days_ago=&min_users_chatted=";
    }

    private async Task ProcessCharactersOnPageAsync(string pageUrl, int minChats, bool checkChatCount, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var cards = _webDriverService.FindElements(By.CssSelector(WebDriverSettings.CharactersPageSelector)).ToListOptimized();
        var characterInfos = await _elementExtractor.ExtractCharacterInfosAsync(cards, minChats, checkChatCount, _indexManager);

        await ProcessCharactersAsync(characterInfos, pageUrl, root, progress, cancellationToken);
    }

    private async Task ProcessCharactersAsync(List<(string href, string id, int chatCount)> characterInfos, string pageUrl, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        foreach (var (href, id, chatCount) in characterInfos)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                _progressReporter.ReportCharacterProgress(progress, id, chatCount);

                await _navigationService.NavigateToAsync(href);

                if (await DownloadCharacterJsonAsync(root, id))
                {
                    var filePath = Path.Combine(root, id + AppSettings.JsonExtension);
                    await _indexManager.RegisterCharacterAsync(id, filePath);
                }

                await _navigationService.NavigateToAsync(pageUrl);
            }
            catch (Exception ex)
            {
                _progressReporter.ReportError(progress, ex.Message);
            }
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

            Console.WriteLine($"JSON-кнопка не найдена для {characterId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки JSON: {ex.Message}");
        }

        return false;
    }
}
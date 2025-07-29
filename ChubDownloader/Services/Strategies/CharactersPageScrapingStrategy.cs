using OpenQA.Selenium;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using ChubDownloader.Infrastructure.Logging;

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
        _progressReporter.ReportProgress(progress, $"📁 Создана папка: {root}");

        var checkChatCount = parameters.MinChats > 0;
        var endPage = parameters.StartPage + parameters.PagesToScan - 1;

        _progressReporter.ReportProgress(progress, $"🚀 Начинаем сканирование персонажей с страницы {parameters.StartPage} по {endPage}");
        _progressReporter.ReportProgress(progress, $"⚙️ Режим проверки чатов: {(checkChatCount ? $"мин. {parameters.MinChats} чатов" : "без ограничений")}");

        for (int page = parameters.StartPage; page <= endPage; page++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _progressReporter.ReportPageProgress(progress, page, endPage);

            var url = BuildCharactersPageUrl(page);
            _progressReporter.ReportProgress(progress, $"🌐 Переходим на: {url}");
            
            var navigationStart = DateTime.Now;
            await _navigationService.NavigateToAsync(url);
            var navigationTime = DateTime.Now - navigationStart;
            _progressReporter.ReportProgress(progress, $"⏱️ Навигация заняла: {navigationTime.TotalSeconds:F1}с");

            var waitStart = DateTime.Now;
            if (!await _navigationService.WaitForElementAsync(By.CssSelector(WebDriverSettings.CharactersPageSelector)))
            {
                _progressReporter.ReportProgress(progress, $"❌ Персонажи не найдены на странице {page} (селектор: {WebDriverSettings.CharactersPageSelector})");
                continue;
            }
            var waitTime = DateTime.Now - waitStart;
            _progressReporter.ReportProgress(progress, $"✅ Элементы загружены за: {waitTime.TotalSeconds:F1}с");

            try
            {
                var processStart = DateTime.Now;
                await ProcessCharactersOnPageAsync(url, parameters.MinChats, checkChatCount, root, progress, cancellationToken);
                var processTime = DateTime.Now - processStart;
                _progressReporter.ReportProgress(progress, $"⚡ Обработка страницы {page} завершена за: {processTime.TotalSeconds:F1}с");
            }
            catch (Exception ex)
            {
                StringBuilderLogger.LogError($"Ошибка обработки страницы {page}: {ex.Message}", ex);
                _progressReporter.ReportError(progress, $"❌ Ошибка обработки страницы {page}: {ex.Message}");
            }
        }

        var totalPages = endPage - parameters.StartPage + 1;
        _progressReporter.ReportProgress(progress, $"🎉 Сканирование завершено!");
        _progressReporter.ReportProgress(progress, $"📈 Итоговая статистика:");
        _progressReporter.ReportProgress(progress, $"   • Обработано страниц: {totalPages}");
        _progressReporter.ReportProgress(progress, $"   • Персонажей на странице: до 50 (вместо 20)");
        _progressReporter.ReportProgress(progress, $"   • Проверка файлов в папке: {Path.Combine(Environment.CurrentDirectory, AppSettings.Characters5FolderName)}");
    }

    private static string BuildCharactersPageUrl(int page)
    {
        return $"{WebDriverSettings.BaseUrl}/characters?excludetopics=&first=20&page={page}&namespace=characters&search=&include_forks=true&nsfw=true&nsfw_only=false&require_custom_prompt=false&require_example_dialogues=false&require_images=false&require_expressions=false&nsfl=true&asc=false&min_ai_rating=0&min_tokens=50&max_tokens=100000&chub=true&require_lore=false&exclude_mine=false&require_lore_embedded=false&require_lore_linked=false&language=&sort=star_count&min_tags=2&topics=&special_mode=&name_like=&only_mine=&inclusive_or=false&recommended_verified=false&require_alternate_greetings=false&max_days_ago=&min_users_chatted=";
    }

    private async Task ProcessCharactersOnPageAsync(string pageUrl, int minChats, bool checkChatCount, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var findStart = DateTime.Now;
        var cards = _webDriverService.FindElements(By.CssSelector(WebDriverSettings.CharactersPageSelector)).ToListOptimized();
        var findTime = DateTime.Now - findStart;
        
        _progressReporter.ReportProgress(progress, $"🔍 Найдено карточек персонажей: {cards.Count} за {findTime.TotalMilliseconds:F0}мс");
        
        if (cards.Count == 0)
        {
            _progressReporter.ReportProgress(progress, $"⚠️ Карточки не найдены с селектором: {WebDriverSettings.CharactersPageSelector}");
            return;
        }

        var extractStart = DateTime.Now;
        var characterInfos = await _elementExtractor.ExtractCharacterInfosAsync(cards, minChats, checkChatCount, _indexManager);
        var extractTime = DateTime.Now - extractStart;
        
        _progressReporter.ReportProgress(progress, $"📊 Извлечено подходящих персонажей: {characterInfos.Count} из {cards.Count} за {extractTime.TotalSeconds:F1}с");

        if (characterInfos.Count == 0)
        {
            _progressReporter.ReportProgress(progress, $"⚠️ Нет новых персонажей для скачивания на этой странице");
            return;
        }

        await ProcessCharactersAsync(characterInfos, pageUrl, root, progress, cancellationToken);
    }

    private async Task ProcessCharactersAsync(List<(string href, string id, int chatCount)> characterInfos, string pageUrl, string root, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var totalCharacters = characterInfos.Count;
        var successCount = 0;
        var failCount = 0;

        _progressReporter.ReportProgress(progress, $"🎯 Начинаем загрузку {totalCharacters} персонажей...");

        for (int i = 0; i < characterInfos.Count; i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var (href, id, chatCount) = characterInfos[i];
            var characterNumber = i + 1;

            try
            {
                _progressReporter.ReportProgress(progress, $"📥 [{characterNumber}/{totalCharacters}] Загружаем: {id} (чатов: {chatCount})");

                var navStart = DateTime.Now;
                await _navigationService.NavigateToAsync(href);
                var navTime = DateTime.Now - navStart;

                var downloadStart = DateTime.Now;
                var downloadSuccess = await DownloadCharacterJsonAsync(root, id);
                var downloadTime = DateTime.Now - downloadStart;

                if (downloadSuccess)
                {
                    var filePath = Path.Combine(root, id + AppSettings.JsonExtension);
                    await _indexManager.RegisterCharacterAsync(id, filePath);
                    successCount++;
                    _progressReporter.ReportProgress(progress, $"✅ [{characterNumber}/{totalCharacters}] {id} загружен за {(navTime + downloadTime).TotalSeconds:F1}с");
                }
                else
                {
                    failCount++;
                    _progressReporter.ReportProgress(progress, $"❌ [{characterNumber}/{totalCharacters}] {id} - не удалось загрузить JSON");
                }

                // Возвращаемся на страницу списка персонажей
                var backNavStart = DateTime.Now;
                await _navigationService.NavigateToAsync(pageUrl);
                var backNavTime = DateTime.Now - backNavStart;
                
                if (backNavTime.TotalSeconds > 2)
                {
                    StringBuilderLogger.LogWarning($"Медленная навигация назад: {backNavTime.TotalSeconds:F1}с");
                }
            }
            catch (Exception ex)
            {
                failCount++;
                StringBuilderLogger.LogError($"Ошибка с {id}: {ex.Message}", ex);
                _progressReporter.ReportError(progress, $"❌ [{characterNumber}/{totalCharacters}] Ошибка с {id}: {ex.Message}");
            }
        }

        _progressReporter.ReportProgress(progress, $"📈 Итоги: ✅ успешно {successCount}, ❌ ошибок {failCount} из {totalCharacters}");
    }

    private async Task<bool> DownloadCharacterJsonAsync(string targetDir, string characterId)
    {
        try
        {
            var delayStart = DateTime.Now;
            await _navigationService.DelayAsync();
            var delayTime = DateTime.Now - delayStart;

            var clearStart = DateTime.Now;
            _downloadService.ClearOldFiles(_downloadPath, AppSettings.JsonExtension);
            var clearTime = DateTime.Now - clearStart;

            var findButtonStart = DateTime.Now;
            var jsonBtn = _elementExtractor.TryFindJsonButton();
            var findBtnTime = DateTime.Now - findButtonStart;

            if (jsonBtn != null)
            {
                var clickStart = DateTime.Now;
                jsonBtn.Click();
                var clickTime = DateTime.Now - clickStart;

                var waitStart = DateTime.Now;
                var result = await _downloadService.WaitForFileDownloadAsync(_downloadPath, targetDir, characterId, AppSettings.JsonExtension);
                var waitTime = DateTime.Now - waitStart;

                StringBuilderLogger.WriteTiming(characterId, delayTime.TotalMilliseconds, clearTime.TotalMilliseconds, findBtnTime.TotalMilliseconds, clickTime.TotalMilliseconds, waitTime.TotalSeconds);
                return result;
            }

            StringBuilderLogger.WriteFormattedLine("❌ JSON-кнопка не найдена для {0} (поиск занял {1:F0}мс)", characterId, findBtnTime.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            StringBuilderLogger.LogError($"Ошибка загрузки JSON для {characterId}: {ex.Message}", ex);
        }

        return false;
    }
}
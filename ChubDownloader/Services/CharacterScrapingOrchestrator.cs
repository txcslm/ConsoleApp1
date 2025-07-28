using ChubDownloader.Models;
using ChubDownloader.Services.Strategies;

namespace ChubDownloader.Services;

public interface ICharacterScrapingOrchestrator
{
    Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken);
    Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken);
    Task DownloadFromCharactersPageAsync(int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken);
}

public sealed class CharacterScrapingOrchestrator : ICharacterScrapingOrchestrator
{
    private readonly IScrapingStrategyFactory _strategyFactory;
    private readonly string _downloadPath;

    public CharacterScrapingOrchestrator(IScrapingStrategyFactory strategyFactory, string downloadPath)
    {
        _strategyFactory = strategyFactory;
        _downloadPath = downloadPath;
    }

    public async Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _strategyFactory.CreateStrategy(DownloadMode.Leaderboard);
            if (strategy == null)
            {
                progress?.Report("Ошибка: Не удалось создать стратегию для загрузки лидерборда");
                return;
            }

            var parameters = new ScrapingParameters
            {
                DownloadPath = _downloadPath
            };

            parameters.ValidateOrThrow();
            await strategy.ExecuteAsync(parameters, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            progress?.Report($"Критическая ошибка при загрузке лидерборда: {ex.Message}");
            throw;
        }
    }

    public async Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _strategyFactory.CreateStrategy(DownloadMode.SegmentPages);
            if (strategy == null)
            {
                progress?.Report("Ошибка: Не удалось создать стратегию для загрузки сегмента");
                return;
            }

            var parameters = new ScrapingParameters
            {
                Segment = segment,
                MinChats = minChats,
                StartPage = startPage,
                PagesToScan = pagesToScan,
                DownloadPath = _downloadPath
            };

            parameters.ValidateOrThrow();
            await strategy.ExecuteAsync(parameters, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            progress?.Report($"Критическая ошибка при загрузке сегмента: {ex.Message}");
            throw;
        }
    }

    public async Task DownloadFromCharactersPageAsync(int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _strategyFactory.CreateStrategy(DownloadMode.CharactersPages);
            if (strategy == null)
            {
                progress?.Report("Ошибка: Не удалось создать стратегию для загрузки страниц персонажей");
                return;
            }

            var parameters = new ScrapingParameters
            {
                MinChats = minChats,
                StartPage = startPage,
                PagesToScan = pagesToScan,
                DownloadPath = _downloadPath
            };

            parameters.ValidateOrThrow();
            await strategy.ExecuteAsync(parameters, progress, cancellationToken);
        }
        catch (Exception ex)
        {
            progress?.Report($"Критическая ошибка при загрузке страниц персонажей: {ex.Message}");
            throw;
        }
    }
}
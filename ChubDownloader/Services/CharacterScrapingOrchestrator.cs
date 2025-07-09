using ChubDownloader.Models;
using ChubDownloader.Services.Strategies;

namespace ChubDownloader.Services;

public interface ICharacterScrapingOrchestrator
{
    Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken);
    Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken);
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
        var strategy = _strategyFactory.CreateStrategy(DownloadMode.Leaderboard);
        var parameters = new ScrapingParameters
        {
            DownloadPath = _downloadPath
        };

        await strategy.ExecuteAsync(parameters, progress, cancellationToken);
    }

    public async Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken)
    {
        var strategy = _strategyFactory.CreateStrategy(DownloadMode.SegmentPages);
        var parameters = new ScrapingParameters
        {
            Segment = segment,
            MinChats = minChats,
            StartPage = startPage,
            PagesToScan = pagesToScan,
            DownloadPath = _downloadPath
        };

        await strategy.ExecuteAsync(parameters, progress, cancellationToken);
    }
}
using ChubDownloader.Models;
using ChubDownloader.Core.DependencyInjection;

namespace ChubDownloader.Services.Strategies;

public interface IScrapingStrategyFactory
{
    IScrapingStrategy CreateStrategy(DownloadMode mode);
}

public sealed class ScrapingStrategyFactory : IScrapingStrategyFactory
{
    private readonly Core.DependencyInjection.IServiceProvider _serviceProvider;

    public ScrapingStrategyFactory(Core.DependencyInjection.IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IScrapingStrategy CreateStrategy(DownloadMode mode)
    {
        return mode switch
        {
            DownloadMode.Leaderboard => _serviceProvider.GetRequiredService<LeaderboardScrapingStrategy>(),
            DownloadMode.SegmentPages => _serviceProvider.GetRequiredService<SegmentScrapingStrategy>(),
            _ => throw new ArgumentException($"Unsupported download mode: {mode}", nameof(mode))
        };
    }
}
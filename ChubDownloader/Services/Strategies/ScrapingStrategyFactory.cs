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
        try
        {
            return mode switch
            {
                DownloadMode.Leaderboard => _serviceProvider.GetRequiredService<LeaderboardScrapingStrategy>(),
                DownloadMode.SegmentPages => _serviceProvider.GetRequiredService<SegmentScrapingStrategy>(),
                DownloadMode.CharactersPages => _serviceProvider.GetRequiredService<CharactersPageScrapingStrategy>(),
                _ => throw new ArgumentException($"Unsupported download mode: {mode}", nameof(mode))
            };
        }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            throw new InvalidOperationException($"Failed to create strategy for mode '{mode}': {ex.Message}", ex);
        }
    }
}
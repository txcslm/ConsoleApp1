using ChubDownloader.Models;
using ChubDownloader.Services;
using ChubDownloader.Views;
using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Presenters;

public sealed class MainPresenter : IDisposable
{
    private readonly IUserInteraction _userInteraction;
    private readonly IProgressDisplay _progressDisplay;
    private readonly IViewStateManager _viewStateManager;
    private readonly ICharacterScrapingOrchestrator _scrapingOrchestrator;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _disposed;

    public MainPresenter(
        IUserInteraction userInteraction,
        IProgressDisplay progressDisplay,
        IViewStateManager viewStateManager,
        ICharacterScrapingOrchestrator scrapingOrchestrator)
    {
        _userInteraction = userInteraction;
        _progressDisplay = progressDisplay;
        _viewStateManager = viewStateManager;
        _scrapingOrchestrator = scrapingOrchestrator;
        _userInteraction.DownloadRequested += OnDownloadRequested;
    }

    private async void OnDownloadRequested(object? sender, DownloadEventArgs e)
    {
        _viewStateManager.SetEnabled(false);
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = new CancellationTokenSource();

        var progress = new Progress<string>(message => _progressDisplay.UpdateProgress(message));

        try
        {
            switch (e.Mode)
            {
                case DownloadMode.Leaderboard:
                    await _scrapingOrchestrator.DownloadFromLeaderboardAsync(progress, _cancellationTokenSource.Token);
                    break;
                case DownloadMode.SegmentPages when e.Segment.HasValue:
                    await _scrapingOrchestrator.DownloadFromSegmentAsync(e.Segment.Value, e.MinChats, e.StartPage, e.PagesToScan, progress, _cancellationTokenSource.Token);
                    break;
                case DownloadMode.CharactersPages:
                    await _scrapingOrchestrator.DownloadFromCharactersPageAsync(e.MinChats, e.StartPage, e.PagesToScan, progress, _cancellationTokenSource.Token);
                    break;
            }

            _progressDisplay.ShowMessage("Загрузка завершена!");
        }
        catch (OperationCanceledException)
        {
            _progressDisplay.ShowMessage("Загрузка отменена пользователем.");
        }
        catch (Exception ex)
        {
            StringBuilderLogger.LogError($"Ошибка: {ex.Message}", ex);
            _progressDisplay.ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            _viewStateManager.SetEnabled(true);
        }
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _userInteraction.DownloadRequested -= OnDownloadRequested;
        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }
}
using ChubDownloader.Models;
using ChubDownloader.Services;
using ChubDownloader.Views;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Infrastructure.FileSystem;
using ChubDownloader.Core.Configuration;

namespace ChubDownloader.Presenters;

public sealed class MainPresenter
{
    private readonly IMainView _view;
    private readonly ICharacterScraper _scraper;
    private readonly IWebDriverService _webDriver;
    private readonly IDownloadService _downloadService;
    private readonly IFileSystemService _fileSystemService;
    private CancellationTokenSource? _cancellationTokenSource;
    
    public MainPresenter(IMainView view)
    {
        _view = view;
        _view.DownloadRequested += OnDownloadRequested;
        
        var downloadPath = Path.Combine(Environment.CurrentDirectory, AppSettings.TempDownloadsFolderName);
        Directory.CreateDirectory(downloadPath);
        
        var profilePath = Path.Combine(Directory.GetCurrentDirectory(), AppSettings.ChromeProfileFolderName);
        Directory.CreateDirectory(profilePath);
        
        _fileSystemService = new FileSystemService();
        _webDriver = new WebDriverService(downloadPath, profilePath);
        _downloadService = new DownloadService(_fileSystemService);
        _scraper = new CharacterScraper(_webDriver, _downloadService, _fileSystemService, downloadPath);
    }
    
    private async void OnDownloadRequested(object? sender, DownloadEventArgs e)
    {
        _view.SetEnabled(false);
        _cancellationTokenSource = new CancellationTokenSource();
        
        var progress = new Progress<string>(message => _view.UpdateProgress(message));
        
        try
        {
            switch (e.Mode)
            {
                case DownloadMode.Leaderboard:
                    await _scraper.DownloadFromLeaderboardAsync(progress, _cancellationTokenSource.Token);
                    break;
                case DownloadMode.SegmentPages when e.Segment.HasValue:
                    await _scraper.DownloadFromSegmentAsync(e.Segment.Value, e.MinChats, e.StartPage, e.PagesToScan, progress, _cancellationTokenSource.Token);
                    break;
            }
            
            _view.ShowMessage("Загрузка завершена!");
        }
        catch (OperationCanceledException)
        {
            _view.ShowMessage("Загрузка отменена пользователем.");
        }
        catch (Exception ex)
        {
            _view.ShowError($"Ошибка: {ex.Message}");
        }
        finally
        {
            _view.SetEnabled(true);
            _webDriver?.Dispose();
        }
    }
    
    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }
}
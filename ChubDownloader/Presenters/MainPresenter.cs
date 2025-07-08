using ChubDownloader.Models;
using ChubDownloader.Services;
using ChubDownloader.Views;
using ConsoleApp1.Services;

namespace ChubDownloader.Presenters
{
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly ICharacterScraper _scraper;
        private readonly IWebDriverService _webDriver;
        private readonly IDownloadService _downloadService;
        private CancellationTokenSource _cancellationTokenSource;
        
        public MainPresenter(IMainView view)
        {
            _view = view;
            _view.DownloadRequested += OnDownloadRequested;
            
            var downloadPath = Path.Combine(Environment.CurrentDirectory, "temp_downloads");
            Directory.CreateDirectory(downloadPath);
            
            var profilePath = Path.Combine(Directory.GetCurrentDirectory(), "ChromeProfile");
            Directory.CreateDirectory(profilePath);
            
            _webDriver = new WebDriverService(downloadPath, profilePath);
            _downloadService = new DownloadService();
            _scraper = new CharacterScraper(_webDriver, _downloadService, downloadPath);
        }
        
        private async void OnDownloadRequested(object sender, DownloadEventArgs e)
        {
            _view.SetEnabled(false);
            _cancellationTokenSource = new CancellationTokenSource();
            
            var progress = new Progress<string>(message => _view.UpdateProgress(message));
            
            try
            {
                if (e.Mode == DownloadMode.Leaderboard)
                {
                    await _scraper.DownloadFromLeaderboardAsync(progress, _cancellationTokenSource.Token);
                }
                else if (e.Mode == DownloadMode.SegmentPages && e.Segment.HasValue)
                {
                    await _scraper.DownloadFromSegmentAsync(e.Segment.Value, e.MinChats, e.StartPage, e.PagesToScan, progress, _cancellationTokenSource.Token);
                }
                
                _view.ShowMessage("Загрузка завершена!");
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
}
using ChubDownloader.Models;

namespace ChubDownloader.Views
{
  public interface IMainView
  {
    event EventHandler<DownloadEventArgs> DownloadRequested;
        
    void ShowMessage(string message);
    void ShowError(string error);
    void UpdateProgress(string progress);
    void SetEnabled(bool enabled);
        
    DownloadMode GetDownloadMode();
    Segment GetSegment();
    int GetMinChats();
    int GetPagesToScan();
  }
}
using ChubDownloader.Models;

namespace ChubDownloader.Views;

public interface IUserInteraction
{
    event EventHandler<DownloadEventArgs>? DownloadRequested;
    DownloadMode GetDownloadMode();
    Segment GetSegment();
    int GetMinChats();
    int GetStartPage();
    int GetPagesToScan();
}
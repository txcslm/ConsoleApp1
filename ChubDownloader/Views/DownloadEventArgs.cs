using ChubDownloader.Models;

namespace ChubDownloader.Views;

public class DownloadEventArgs : EventArgs
{
  public DownloadMode Mode { get; set; }
  public Segment? Segment { get; set; }
  public int MinChats { get; set; }
  public int PagesToScan { get; set; }
}
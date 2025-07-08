using ChubDownloader.Models;

public class DownloadEventArgs : EventArgs
{
  public DownloadMode Mode { get; set; }
  public Segment? Segment { get; set; }
  public int MinChats { get; set; }
  public int StartPage { get; set; } = 1;  // Новое поле для стартовой страницы
  public int PagesToScan { get; set; }
}
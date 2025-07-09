using ChubDownloader.Models;

namespace ChubDownloader.Services;

public interface ICharacterScraper
{
  Task DownloadFromLeaderboardAsync(IProgress<string> progress, CancellationToken cancellationToken);
  Task DownloadFromSegmentAsync(Segment segment, int minChats, int startPage, int pagesToScan, IProgress<string> progress, CancellationToken cancellationToken);
}
using ChubDownloader.Models;

namespace ChubDownloader.Services.Strategies;

public interface IScrapingStrategy
{
    Task ExecuteAsync(ScrapingParameters parameters, IProgress<string> progress, CancellationToken cancellationToken);
}

public sealed class ScrapingParameters
{
    public Segment? Segment { get; set; }
    public int MinChats { get; set; }
    public int StartPage { get; set; }
    public int PagesToScan { get; set; }
    public string DownloadPath { get; set; } = string.Empty;
}
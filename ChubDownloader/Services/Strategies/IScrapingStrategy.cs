using ChubDownloader.Models;

namespace ChubDownloader.Services.Strategies;

public interface IScrapingStrategy
{
    Task ExecuteAsync(ScrapingParameters parameters, IProgress<string> progress, CancellationToken cancellationToken);
}

public sealed class ScrapingParameters
{
    public Segment? Segment { get; init; }
    public int MinChats { get; init; }
    public int StartPage { get; init; } = 1;
    public int PagesToScan { get; init; } = 1;
    public string DownloadPath { get; init; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(DownloadPath) && 
               StartPage > 0 && 
               PagesToScan > 0 && 
               MinChats >= 0;
    }

    public void ValidateOrThrow()
    {
        if (string.IsNullOrWhiteSpace(DownloadPath))
            throw new ArgumentException("Путь загрузки не может быть пустым", nameof(DownloadPath));
        
        if (StartPage <= 0)
            throw new ArgumentException("Начальная страница должна быть больше 0", nameof(StartPage));
        
        if (PagesToScan <= 0)
            throw new ArgumentException("Количество страниц должно быть больше 0", nameof(PagesToScan));
        
        if (MinChats < 0)
            throw new ArgumentException("Минимальное количество чатов не может быть отрицательным", nameof(MinChats));
    }
}
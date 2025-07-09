namespace ChubDownloader.Services;

public interface IDownloadService
{
    Task<bool> WaitForFileDownloadAsync(string tempPath, string rootPath, string characterId, string extension);
    void ClearOldFiles(string rootPath, string extension);
}
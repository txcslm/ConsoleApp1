namespace ChubDownloader.Services;

public interface IDownloadService
{
    Task<bool> WaitForFileDownloadAsync(string tempPath, string targetPath, string characterId, string extension);
    void ClearOldFiles(string rootPath, string extension);
}
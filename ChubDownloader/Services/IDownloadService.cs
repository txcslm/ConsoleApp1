namespace ChubDownloader.Services;

public interface IDownloadService
{
  bool WaitForFileDownload(string tempPath, string targetPath, string characterId, string extension);
  void ClearOldFiles(string path, string extension);
}

namespace ChubDownloader.Services;

public class DownloadService : IDownloadService
{
  private const int DownloadWaitMaxMs = 5000;
  private const int DownloadCheckIntervalMs = 100;
  private const int FileStableCheckDelayMs = 200;
  private const int RecentFileSeconds = 10;
  private const int OldFileAgeMinutes = 5;
        
  public bool WaitForFileDownload(string tempPath, string targetPath, string characterId, string extension)
  {
    try
    {
      int maxAttempts = DownloadWaitMaxMs / DownloadCheckIntervalMs;
                
      for (int i = 0; i < maxAttempts; i++)
      {
        var files = Directory.GetFiles(tempPath, $"*{extension}")
          .Select(f => new FileInfo(f))
          .Where(f => f.CreationTime > DateTime.Now.AddSeconds(-RecentFileSeconds))
          .OrderByDescending(f => f.CreationTime)
          .ToList();
                    
        if (files.Any())
        {
          var sourceFile = files.First();
                        
          // Проверяем стабильность размера
          var size1 = sourceFile.Length;
          Thread.Sleep(FileStableCheckDelayMs);
          sourceFile.Refresh();
          var size2 = sourceFile.Length;
                        
          if (size1 == size2 && size1 > 0)
          {
            var destFile = Path.Combine(targetPath, characterId + extension);
            File.Move(sourceFile.FullName, destFile, true);
            return true;
          }
        }
                    
        Thread.Sleep(DownloadCheckIntervalMs);
      }
                
      return false;
    }
    catch
    {
      return false;
    }
  }
        
  public void ClearOldFiles(string path, string extension)
  {
    try
    {
      var oldFiles = Directory.GetFiles(path, $"*{extension}")
        .Where(f => File.GetCreationTime(f) < DateTime.Now.AddMinutes(-OldFileAgeMinutes));
      foreach (var file in oldFiles)
      {
        try { File.Delete(file); } catch { }
      }
    }
    catch { }
  }
}
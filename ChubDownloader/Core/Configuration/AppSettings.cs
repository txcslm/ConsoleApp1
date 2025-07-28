namespace ChubDownloader.Core.Configuration;

public sealed class AppSettings
{
  public const int DefaultDebugPort = 9222;
  public const int DownloadWaitMaxMs = 3000; // Уменьшено с 5000
  public const int DownloadCheckIntervalMs = 50; // Уменьшено с 100
  public const int FileStableCheckDelayMs = 100; // Уменьшено с 200
  public const int RecentFileSeconds = 5; // Уменьшено с 10
  public const int OldFileAgeMinutes = 2; // Уменьшено с 5
  public const int WebDriverTimeoutSeconds = 8; // Уменьшено с 10
  public const int PageLoadTimeoutSeconds = 12; // Уменьшено с 15
  public const int ImplicitWaitSeconds = 2; // Уменьшено с 3
  public const int TaskDelayMs = 5; // Уменьшено с 10
  public const int ThreadSleepMs = 50; // Уменьшено с 100
  public const int TooltipDelayMs = 5; // Уменьшено с 10
    
  public const string TempDownloadsFolderName = "temp_downloads";
  public const string ChromeProfileFolderName = "ChromeProfile";
  public const string CharactersFolderName = "characters";
  public const string FollowersFolderName = "followers";
  public const string Characters2FolderName = "characters2";
  public const string Characters3FolderName = "characters3";
  public const string Characters4FolderName = "characters4";
  public const string Characters5FolderName = "characters5";
  public const string CharacterIndexFileName = "character_index.json";
    
  public const string JsonExtension = ".json";
}
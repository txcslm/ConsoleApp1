namespace ChubDownloader.Core.Configuration;

public sealed class AppSettings
{
    public const int DefaultDebugPort = 9222;
    public const int DownloadWaitMaxMs = 5000;
    public const int DownloadCheckIntervalMs = 100;
    public const int FileStableCheckDelayMs = 200;
    public const int RecentFileSeconds = 10;
    public const int OldFileAgeMinutes = 5;
    public const int WebDriverTimeoutSeconds = 10;
    public const int PageLoadTimeoutSeconds = 15;
    public const int ImplicitWaitSeconds = 3;
    public const int TaskDelayMs = 10;
    public const int ThreadSleepMs = 100;
    public const int TooltipDelayMs = 10;
    
    public const string TempDownloadsFolderName = "temp_downloads";
    public const string ChromeProfileFolderName = "ChromeProfile";
    public const string CharactersFolderName = "characters";
    public const string FollowersFolderName = "followers";
    public const string Characters2FolderName = "characters2";
    public const string CharacterIndexFileName = "character_index.json";
    
    public const string JsonExtension = ".json";
}
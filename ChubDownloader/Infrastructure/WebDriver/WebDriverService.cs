using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;
using ChubDownloader.Core.Configuration;

namespace ChubDownloader.Infrastructure.WebDriver;

public sealed class WebDriverService : IWebDriverService, IAsyncDisposable
{
    private readonly ChromeDriver _driver;
    private static int _debugPort = AppSettings.DefaultDebugPort;
    private bool _disposed;
    
    public IWebDriver Driver => _driver;
    
    public WebDriverService(string downloadPath, string userPath)
    {
        _driver = CreateChromeDriver(downloadPath, userPath);
        ConfigureTimeouts();
        MinimizeWindowOnMac();
    }
    
    private ChromeDriver CreateChromeDriver(string downloadPath, string userPath)
    {
        var options = CreateChromeOptions(downloadPath, userPath);
        return new ChromeDriver(options);
    }
    
    private static ChromeOptions CreateChromeOptions(string downloadPath, string userPath)
    {
        var options = new ChromeOptions();
        
        options.AddArgument($"--user-data-dir={userPath}");
        options.AddArgument($"--remote-debugging-port={_debugPort++}");
        options.AddArgument("--remote-allow-origins=*");
        
        ConfigureMacOSSpecificOptions(options);
        ConfigurePerformanceOptions(options);
        ConfigureDownloadPreferences(options, downloadPath);
        
        options.PageLoadStrategy = PageLoadStrategy.Normal;
        
        return options;
    }
    
    private static void ConfigureMacOSSpecificOptions(ChromeOptions options)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;
            
        options.AddArgument("--wm-window-animations-disabled");
        options.AddArgument("--animation-duration-scale=0");
        options.AddArgument("--disable-features=RendererCodeIntegrity");
        options.AddArgument("--disable-features=IsolateOrigins,site-per-process");
        options.AddArgument("--start-minimized");
    }
    
    private static void ConfigurePerformanceOptions(ChromeOptions options)
    {
        options.AddArgument("--disable-backgrounding-occluded-windows");
        options.AddArgument("--disable-renderer-backgrounding");
        options.AddArgument("--disable-background-timer-throttling");
        options.AddArgument("--dns-prefetch-disable");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--disable-blink-features=AutomationControlled");
        options.AddArgument("--window-size=1280,800");
    }
    
    private static void ConfigureDownloadPreferences(ChromeOptions options, string downloadPath)
    {
        options.AddUserProfilePreference("download.default_directory", downloadPath);
        options.AddUserProfilePreference("download.prompt_for_download", false);
        options.AddUserProfilePreference("download.directory_upgrade", true);
        options.AddUserProfilePreference("safebrowsing.enabled", false);
    }
    
    private void ConfigureTimeouts()
    {
        var timeouts = _driver.Manage().Timeouts();
        timeouts.ImplicitWait = TimeSpan.FromSeconds(AppSettings.ImplicitWaitSeconds);
        timeouts.PageLoad = TimeSpan.FromSeconds(AppSettings.PageLoadTimeoutSeconds);
    }
    
    private void MinimizeWindowOnMac()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;
            
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("window.moveTo(10000, 10000);");
        }
        catch
        {
            // Ignore
        }
    }
    
    public void NavigateTo(string url)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _driver.Navigate().GoToUrl(url);
    }
    
    public bool WaitForElement(By by, int timeoutSeconds = AppSettings.WebDriverTimeoutSeconds)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        try
        {
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
            wait.Until(d => d.FindElements(by).Count > 0);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public IReadOnlyCollection<IWebElement> FindElements(By by)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _driver.FindElements(by);
    }
    
    public IWebElement? FindElement(By by)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        try
        {
            return _driver.FindElement(by);
        }
        catch (NoSuchElementException)
        {
            return null;
        }
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _driver?.Quit();
            }
            catch
            {
                // Ignore cleanup errors
            }
            _disposed = true;
        }
    }
    
    protected async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            try
            {
                await Task.Run(() => _driver?.Quit());
            }
            catch
            {
                // Ignore cleanup errors
            }
            _disposed = true;
        }
    }
    
    ~WebDriverService()
    {
        Dispose(false);
    }
}
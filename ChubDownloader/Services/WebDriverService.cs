using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ChubDownloader.Services.ChubDownloader.Services;

public class WebDriverService : IWebDriverService
{
  private readonly ChromeDriver _driver;
  private readonly string _mainWindowHandle;
        
  public IWebDriver Driver => _driver;
        
  public WebDriverService(string downloadPath)
  {
    var options = new ChromeOptions();
    options.AddArgument("--user-data-dir=/Users/txcslm/chub_followers_profile");
    options.AddArgument("--disable-gpu");
    options.AddArgument("--window-size=1920,1080");
    options.AddArgument("--disable-blink-features=AutomationControlled");
    options.AddArgument("--disable-dev-shm-usage");
    options.AddArgument("--no-sandbox");
            
    options.AddUserProfilePreference("download.default_directory", downloadPath);
    options.AddUserProfilePreference("download.prompt_for_download", false);
    options.AddUserProfilePreference("download.directory_upgrade", true);
    options.AddUserProfilePreference("safebrowsing.enabled", false);
            
    options.PageLoadStrategy = PageLoadStrategy.Normal;

    _driver = new ChromeDriver(options);
    _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
    _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            
    _mainWindowHandle = _driver.CurrentWindowHandle;
  }
        
  public void NavigateTo(string url)
  {
    _driver.Navigate().GoToUrl(url);
  }
        
  public void OpenNewTab(string url)
  {
    ((IJavaScriptExecutor)_driver).ExecuteScript($"window.open('{url}', '_blank');");
  }
        
  public void SwitchToLastTab()
  {
    _driver.SwitchTo().Window(_driver.WindowHandles.Last());
  }
        
  public void CloseCurrentTab()
  {
    _driver.Close();
  }
        
  public void SwitchToMainWindow()
  {
    _driver.SwitchTo().Window(_mainWindowHandle);
  }
        
  public bool WaitForElement(By by, int timeoutSeconds = 10)
  {
    try
    {
      var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
      wait.Until(d => d.FindElements(by).Any());
      return true;
    }
    catch
    {
      return false;
    }
  }
        
  public IWebElement FindElement(By by)
  {
    return _driver.FindElement(by);
  }
        
  public IReadOnlyCollection<IWebElement> FindElements(By by)
  {
    return _driver.FindElements(by);
  }
        
  public void Dispose()
  {
    _driver?.Quit();
  }
}
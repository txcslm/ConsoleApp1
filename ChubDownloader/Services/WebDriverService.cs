using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ChubDownloader.Services
{
    public class WebDriverService : IWebDriverService
    {
        private readonly ChromeDriver _driver;
        private readonly string _mainWindowHandle;
        private static int _debugPort = 9222; 
        
        public IWebDriver Driver => _driver;
        
        public WebDriverService(string downloadPath)
        {
            var options = new ChromeOptions();
            
            options.AddArgument("--user-data-dir=/Users/txcslm/chub_followers_profile");
            
            options.AddArgument($"--remote-debugging-port={_debugPort++}"); // Инкрементируем порт для каждого экземпляра
            options.AddArgument("--remote-allow-origins=*");
            
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-background-timer-throttling");
            
            options.AddArgument("--dns-prefetch-disable");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            
            options.AddArgument("--disable-blink-features=AutomationControlled");
            
            options.AddArgument("--window-size=1280,800");
            options.AddArgument("--window-position=100,100");
            
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
            ((IJavaScriptExecutor)_driver).ExecuteScript($@"
                const link = document.createElement('a');
                link.href = '{url}';
                link.target = '_blank';
                link.rel = 'noopener';
                document.body.appendChild(link);
                link.click();
                document.body.removeChild(link);
            ");
            
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
}

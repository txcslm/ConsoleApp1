using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace ChubDownloader.Services
{
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
            
            options.AddArgument("--no-first-run");
            options.AddArgument("--no-default-browser-check");
            options.AddArgument("--disable-popup-blocking");
            
            if (OperatingSystem.IsMacOS())
            {
                options.AddArgument("--start-minimized");
            }
            
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", false);
            
            options.PageLoadStrategy = PageLoadStrategy.Normal;

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            
            _mainWindowHandle = _driver.CurrentWindowHandle;
            
            // Устанавливаем позицию окна чтобы оно не мешало
            _driver.Manage().Window.Position = new System.Drawing.Point(0, 0);
            
            // Опционально: можно сделать окно меньше
            // _driver.Manage().Window.Size = new System.Drawing.Size(1024, 768);
        }
        
        public void NavigateTo(string url)
        {
            _driver.Navigate().GoToUrl(url);
        }
        
        public void OpenNewTab(string url)
        {
            // Открываем вкладку без фокуса
            ((IJavaScriptExecutor)_driver).ExecuteScript($@"
                var newTab = window.open('{url}', '_blank');
                newTab.blur();
                window.focus();
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

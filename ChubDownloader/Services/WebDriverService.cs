using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;

namespace ChubDownloader.Services
{
    public class WebDriverService : IWebDriverService
    {
        private readonly ChromeDriver _driver;
        private static int _debugPort = 9222;
        
        public IWebDriver Driver => _driver;
        
        public WebDriverService(string downloadPath, string userPath)
        {
            var options = new ChromeOptions();
            
            options.AddArgument($"--user-data-dir={userPath}");
            
            options.AddArgument($"--remote-debugging-port={_debugPort++}");
            options.AddArgument("--remote-allow-origins=*");
            
            // Специальные настройки для macOS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Отключаем анимации и визуальные эффекты
                options.AddArgument("--wm-window-animations-disabled");
                options.AddArgument("--animation-duration-scale=0");
                
                // Заставляем Chrome работать в фоне
                options.AddArgument("--disable-features=RendererCodeIntegrity");
                options.AddArgument("--disable-features=IsolateOrigins,site-per-process");
                
                // Минимизируем окно сразу после запуска
                options.AddArgument("--start-minimized");
            }
            
            options.AddArgument("--disable-backgrounding-occluded-windows");
            options.AddArgument("--disable-renderer-backgrounding");
            options.AddArgument("--disable-background-timer-throttling");
            
            options.AddArgument("--dns-prefetch-disable");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            
            options.AddArgument("--disable-blink-features=AutomationControlled");
            
            // Устанавливаем позицию окна за пределами видимой области
            options.AddArgument("--window-size=1280,800");
            
            options.AddUserProfilePreference("download.default_directory", downloadPath);
            options.AddUserProfilePreference("download.prompt_for_download", false);
            options.AddUserProfilePreference("download.directory_upgrade", true);
            options.AddUserProfilePreference("safebrowsing.enabled", false);
            
            options.PageLoadStrategy = PageLoadStrategy.Normal;

            _driver = new ChromeDriver(options);
            _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(3);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(15);
            
            
            // Для macOS: минимизируем окно после запуска
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    // Перемещаем окно за пределы экрана
                    ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                        window.moveTo(10000, 10000);
                    ");
                }
                catch { }
            }
        }
        
        public void NavigateTo(string url)
        {
            _driver.Navigate().GoToUrl(url);
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

        /// <summary>
        /// Находит элементы на странице по заданному селектору.
        /// </summary>
        /// <param name="by">Селектор для поиска элементов.</param>
        /// <returns>Коллекция найденных элементов.</returns>
        
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
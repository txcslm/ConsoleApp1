using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Runtime.InteropServices;

namespace ChubDownloader.Services
{
    public class WebDriverService : IWebDriverService
    {
        private readonly ChromeDriver _driver;
        private readonly string _mainWindowHandle;
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
            
            _mainWindowHandle = _driver.CurrentWindowHandle;
            
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
        
        public void OpenNewTab(string url)
        {
            // Используем JavaScript для открытия новой вкладки без фокуса
            ((IJavaScriptExecutor)_driver).ExecuteScript($@"
        const link = document.createElement('a');
        link.href = '{url}';
        link.target = '_blank';
        link.rel = 'noopener noreferrer';
        
        // Создаем событие с опцией не активировать окно
        const event = new MouseEvent('click', {{
            view: window,
            bubbles: true,
            cancelable: true,
            ctrlKey: true  // Аналог Ctrl+Click для открытия в фоновой вкладке
        }});
        
        document.body.appendChild(link);
        link.dispatchEvent(event);
        document.body.removeChild(link);
    ");
        }
        
        public void SwitchToLastTab()
        {
            var handles = _driver.WindowHandles;
            if (handles.Count > 0)
            {
                _driver.SwitchTo().Window(handles.Last());
            }
        }
        
        public void CloseCurrentTab()
        {
            if (_driver.WindowHandles.Count > 1)
            {
                _driver.Close();
            }
        }
        
        public void SwitchToMainWindow()
        {
            if (_driver.WindowHandles.Contains(_mainWindowHandle))
            {
                _driver.SwitchTo().Window(_mainWindowHandle);
            }
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
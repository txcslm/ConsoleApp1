using OpenQA.Selenium;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Infrastructure.Logging;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using ZLinq;

namespace ChubDownloader.Services;

public interface INavigationService
{
    Task NavigateToAsync(string url);
    Task<bool> WaitForElementAsync(By selector, int timeoutSeconds = AppSettings.WebDriverTimeoutSeconds);
    bool GoToNextPage();
    Task DelayAsync(int milliseconds = AppSettings.TaskDelayMs, CancellationToken cancellationToken = default);
}

public sealed class NavigationService : INavigationService
{
    private readonly IWebDriverService _webDriverService;

    public NavigationService(IWebDriverService webDriverService)
    {
        _webDriverService = webDriverService;
    }

    public async Task NavigateToAsync(string url)
    {
        await WebDriverResilience.ExecuteWithRetryAsync(
            () => Task.Run(() => _webDriverService.NavigateTo(url)),
            $"навигация к {url}");
    }

    public async Task<bool> WaitForElementAsync(By selector, int timeoutSeconds = AppSettings.WebDriverTimeoutSeconds)
    {
        return await WebDriverResilience.ExecuteWithRetryAsync(
            () => Task.FromResult(_webDriverService.WaitForElement(selector, timeoutSeconds)),
            $"ожидание элемента {selector}");
    }

    public bool GoToNextPage()
    {
        try
        {
            var nextButton = _webDriverService.FindElements(By.XPath(WebDriverSettings.NextPageXPath)).FirstOrDefaultOptimized(_ => true) ??
                             _webDriverService.FindElements(By.CssSelector(WebDriverSettings.AntPaginationNextSelector)).FirstOrDefaultOptimized(_ => true);

            if (nextButton != null && nextButton.GetAttribute("aria-disabled") != "true")
            {
                ((IJavaScriptExecutor)_webDriverService.Driver).ExecuteScript("arguments[0].click();", nextButton);
                Thread.Sleep(AppSettings.ThreadSleepMs);
                return true;
            }
        }
        catch
        {
            // Ignore
        }
        return false;
    }

    public async Task DelayAsync(int milliseconds = AppSettings.TaskDelayMs, CancellationToken cancellationToken = default)
    {
        await Task.Delay(milliseconds, cancellationToken);
    }
}
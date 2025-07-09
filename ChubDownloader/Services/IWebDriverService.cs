using OpenQA.Selenium;

namespace ChubDownloader.Services
{
  public interface IWebDriverService : IDisposable
  {
    IWebDriver Driver { get; }
    void NavigateTo(string url);
    bool WaitForElement(By by, int timeoutSeconds = 10);
    IReadOnlyCollection<IWebElement> FindElements(By by);
  }
}
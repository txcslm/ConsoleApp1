using OpenQA.Selenium;

namespace ChubDownloader.Services
{
  public interface IWebDriverService : IDisposable
  {
    IWebDriver Driver { get; }
    void NavigateTo(string url);
    void OpenNewTab(string url);
    void SwitchToLastTab();
    void CloseCurrentTab();
    void SwitchToMainWindow();
    bool WaitForElement(By by, int timeoutSeconds = 10);
    IWebElement FindElement(By by);
    IReadOnlyCollection<IWebElement> FindElements(By by);
  }
}
using OpenQA.Selenium;
using ZLinq;

namespace ChubDownloader.Core.Extensions;

public static class WebDriverExtensions
{
    /// <summary>
    /// Zero-allocation first element finder for WebDriver collections
    /// </summary>
    public static IWebElement? FirstOrDefaultOptimized(this IReadOnlyCollection<IWebElement> elements)
    {
        return elements.Count > 0 ? elements.AsValueEnumerable().FirstOrDefault() : null;
    }
    
    /// <summary>
    /// Zero-allocation first element finder with predicate for WebDriver collections
    /// </summary>
    public static IWebElement? FirstOrDefaultOptimized(this IReadOnlyCollection<IWebElement> elements, Func<IWebElement, bool> predicate)
    {
        return elements.AsValueEnumerable().Where(predicate).FirstOrDefault();
    }
    
    /// <summary>
    /// Zero-allocation where clause for WebDriver collections
    /// </summary>
    public static IEnumerable<IWebElement> WhereOptimized(this IReadOnlyCollection<IWebElement> elements, Func<IWebElement, bool> predicate)
    {
        var result = new List<IWebElement>();
        elements.AsValueEnumerable().Where(predicate).CopyTo(result);
        return result;
    }
    
    /// <summary>
    /// Zero-allocation count for filtered WebDriver collections
    /// </summary>
    public static int CountOptimized(this IReadOnlyCollection<IWebElement> elements, Func<IWebElement, bool> predicate)
    {
        return elements.AsValueEnumerable().Where(predicate).Count();
    }
    
    /// <summary>
    /// Zero-allocation any check for WebDriver collections
    /// </summary>
    public static bool AnyOptimized(this IReadOnlyCollection<IWebElement> elements, Func<IWebElement, bool> predicate)
    {
        return elements.AsValueEnumerable().Where(predicate).Any();
    }
}
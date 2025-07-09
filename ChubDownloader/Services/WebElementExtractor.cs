using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;

namespace ChubDownloader.Services;

public interface IWebElementExtractor
{
    IWebElement? TryFindJsonButton();
    int GetChatCount(IWebElement card);
    List<(string userName, string userUrl)> ExtractUserUrls(IList<IWebElement> rows);
    List<(string href, string id, int chatCount)> ExtractCharacterInfos(IList<IWebElement> cards, int minChats, bool checkChatCount, ICharacterIndexManager indexManager);
    List<(string href, string id)> ExtractCharacterUrls(IList<IWebElement> cards, ICharacterIndexManager indexManager);
}

public sealed class WebElementExtractor : IWebElementExtractor
{
    private readonly IWebDriverService _webDriverService;

    public WebElementExtractor(IWebDriverService webDriverService)
    {
        _webDriverService = webDriverService;
    }

    public IWebElement? TryFindJsonButton()
    {
        foreach (var xpath in WebDriverSettings.JsonButtonXPaths)
        {
            try
            {
                var element = _webDriverService.Driver.FindElement(By.XPath(xpath));
                if (element.Displayed && element.Enabled)
                    return element;
            }
            catch
            {
                // Ignore
            }
        }
        return null;
    }

    public int GetChatCount(IWebElement card)
    {
        try
        {
            var actions = new Actions(_webDriverService.Driver);
            var iconBlocks = card.FindElements(By.CssSelector(WebDriverSettings.IconBlockSelector)).ToListOptimized();
            if (iconBlocks.Count == 0) return 0;

            actions.MoveToElement(iconBlocks[0]).Perform();
            Thread.Sleep(AppSettings.TooltipDelayMs);

            var tooltip = _webDriverService.Driver.FindElements(By.CssSelector(WebDriverSettings.AntTooltipInnerSelector))
                .FirstOrDefault(el => el.Displayed);

            if (tooltip != null)
            {
                return tooltip.Text.ParseChatCount();
            }
        }
        catch
        {
            // Ignore
        }
        return 0;
    }

    public List<(string userName, string userUrl)> ExtractUserUrls(IList<IWebElement> rows)
    {
        var userUrls = new List<(string, string)>(rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var linkElem = row.FindElement(By.CssSelector(WebDriverSettings.UserLinkSelector));
                var userName = linkElem.Text.Trim().TrimStart('@');
                var userUrl = linkElem.GetAttribute("href");
                if (!string.IsNullOrEmpty(userUrl))
                {
                    userUrls.Add((userName, userUrl));
                }
            }
            catch
            {
                // Ignore invalid rows
            }
        }

        return userUrls;
    }

    public List<(string href, string id, int chatCount)> ExtractCharacterInfos(IList<IWebElement> cards, int minChats, bool checkChatCount, ICharacterIndexManager indexManager)
    {
        var characterInfos = new List<(string href, string id, int chatCount)>(cards.Count);

        foreach (var card in cards)
        {
            try
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();

                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id))
                {
                    var chatCount = checkChatCount ? GetChatCount(card) : int.MaxValue;

                    if (chatCount >= minChats && !indexManager.IsCharacterExistsAsync(id).GetAwaiter().GetResult())
                    {
                        characterInfos.Add((href, id, chatCount));
                    }
                }
            }
            catch
            {
                // Ignore invalid cards
            }
        }

        return characterInfos;
    }

    public List<(string href, string id)> ExtractCharacterUrls(IList<IWebElement> cards, ICharacterIndexManager indexManager)
    {
        var characterUrls = new List<(string href, string id)>(cards.Count);

        foreach (var card in cards)
        {
            try
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();

                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id) && !indexManager.IsCharacterExistsAsync(id).GetAwaiter().GetResult())
                {
                    characterUrls.Add((href, id));
                }
            }
            catch
            {
                // Ignore invalid cards
            }
        }

        return characterUrls;
    }
}
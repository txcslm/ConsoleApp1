using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using System.Text;

namespace ChubDownloader.Services;

public interface IWebElementExtractor
{
    IWebElement? TryFindJsonButton();
    int GetChatCount(IWebElement card);
    List<(string userName, string userUrl)> ExtractUserUrls(IList<IWebElement> rows);
    Task<List<(string href, string id, int chatCount)>> ExtractCharacterInfosAsync(IList<IWebElement> cards, int minChats, bool checkChatCount, ICharacterIndexManager indexManager);
    Task<List<(string href, string id)>> ExtractCharacterUrlsAsync(IList<IWebElement> cards, ICharacterIndexManager indexManager);
}

public sealed class WebElementExtractor : IWebElementExtractor
{
    private readonly IWebDriverService _webDriverService;
    private readonly Lazy<Actions> _actions;
    private readonly StringBuilder _stringBuilder = new();

    public WebElementExtractor(IWebDriverService webDriverService)
    {
        _webDriverService = webDriverService;
        _actions = new Lazy<Actions>(() => new Actions(_webDriverService.Driver));
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
            var iconBlocks = card.FindElements(By.CssSelector(WebDriverSettings.IconBlockSelector)).ToListOptimized();
            if (iconBlocks.Count == 0) return 0;

            _actions.Value.MoveToElement(iconBlocks[0]).Perform();
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

    public async Task<List<(string href, string id, int chatCount)>> ExtractCharacterInfosAsync(IList<IWebElement> cards, int minChats, bool checkChatCount, ICharacterIndexManager indexManager)
    {
        var characterInfos = new List<(string href, string id, int chatCount)>(cards.Count);
        var candidateIds = new List<string>(cards.Count);
        var candidateInfos = new List<(string href, string id, int chatCount)>(cards.Count);

        // First pass: collect all candidate IDs and their basic info
        foreach (var card in cards)
        {
            try
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();

                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id))
                {
                    var chatCount = checkChatCount ? GetChatCount(card) : int.MaxValue;
                    
                    if (chatCount >= minChats)
                    {
                        candidateIds.Add(id);
                        candidateInfos.Add((href, id, chatCount));
                    }
                }
            }
            catch
            {
                // Ignore invalid cards
            }
        }

        // Batch check existence for all candidates
        var existenceResults = await Task.WhenAll(candidateIds.Select(id => indexManager.IsCharacterExistsAsync(id)));
        
        // Second pass: filter out existing characters
        for (int i = 0; i < candidateInfos.Count; i++)
        {
            if (!existenceResults[i])
            {
                characterInfos.Add(candidateInfos[i]);
            }
        }

        return characterInfos;
    }

    public async Task<List<(string href, string id)>> ExtractCharacterUrlsAsync(IList<IWebElement> cards, ICharacterIndexManager indexManager)
    {
        var characterUrls = new List<(string href, string id)>(cards.Count);
        var candidateIds = new List<string>(cards.Count);
        var candidateUrls = new List<(string href, string id)>(cards.Count);

        // First pass: collect all candidate IDs and URLs
        foreach (var card in cards)
        {
            try
            {
                var href = card.GetAttribute("href");
                var id = href.ExtractCharacterId();

                if (!string.IsNullOrEmpty(href) && !string.IsNullOrEmpty(id))
                {
                    candidateIds.Add(id);
                    candidateUrls.Add((href, id));
                }
            }
            catch
            {
                // Ignore invalid cards
            }
        }

        // Batch check existence for all candidates
        var existenceResults = await Task.WhenAll(candidateIds.Select(id => indexManager.IsCharacterExistsAsync(id)));
        
        // Second pass: filter out existing characters
        for (int i = 0; i < candidateUrls.Count; i++)
        {
            if (!existenceResults[i])
            {
                characterUrls.Add(candidateUrls[i]);
            }
        }

        return characterUrls;
    }
}
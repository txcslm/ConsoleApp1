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
        Console.WriteLine($"🔍 Поиск JSON кнопки среди {WebDriverSettings.JsonButtonXPaths.Length} селекторов...");
        
        for (int i = 0; i < WebDriverSettings.JsonButtonXPaths.Length; i++)
        {
            var xpath = WebDriverSettings.JsonButtonXPaths[i];
            try
            {
                var findStart = DateTime.Now;
                var element = _webDriverService.Driver.FindElement(By.XPath(xpath));
                var findTime = DateTime.Now - findStart;
                
                Console.WriteLine($"🔍 Селектор {i + 1}: найден элемент за {findTime.TotalMilliseconds:F0}мс");
                
                if (element.Displayed && element.Enabled)
                {
                    Console.WriteLine($"✅ JSON кнопка найдена с селектором {i + 1}: {xpath}");
                    return element;
                }
                else
                {
                    Console.WriteLine($"⚠️ Селектор {i + 1}: элемент найден, но недоступен (Displayed: {element.Displayed}, Enabled: {element.Enabled})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Селектор {i + 1} не сработал: {ex.GetType().Name}");
            }
        }
        
        Console.WriteLine($"❌ JSON кнопка не найдена ни с одним из {WebDriverSettings.JsonButtonXPaths.Length} селекторов");
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
        Console.WriteLine($"🔍 Извлекаем информацию из {cards.Count} карточек (минимум чатов: {minChats}, проверка чатов: {checkChatCount})");
        
        var characterInfos = new List<(string href, string id, int chatCount)>(cards.Count);
        var candidateIds = new List<string>(cards.Count);
        var candidateInfos = new List<(string href, string id, int chatCount)>(cards.Count);
        var skippedByChats = 0;
        var invalidCards = 0;

        var extractStart = DateTime.Now;
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
                    else
                    {
                        skippedByChats++;
                        Console.WriteLine($"⚠️ Пропущен {id}: недостаточно чатов ({chatCount} < {minChats})");
                    }
                }
                else
                {
                    invalidCards++;
                }
            }
            catch (Exception ex)
            {
                invalidCards++;
                Console.WriteLine($"❌ Ошибка обработки карточки: {ex.Message}");
            }
        }
        var extractTime = DateTime.Now - extractStart;
        
        Console.WriteLine($"📊 Первый проход: найдено {candidateInfos.Count} кандидатов, пропущено по чатам: {skippedByChats}, невалидных: {invalidCards} за {extractTime.TotalSeconds:F1}с");

        var checkStart = DateTime.Now;
        var existenceResults = await Task.WhenAll(candidateIds.Select(indexManager.IsCharacterExistsAsync));
        var checkTime = DateTime.Now - checkStart;
        
        Console.WriteLine($"🔍 Проверка существования {candidateIds.Count} персонажей заняла: {checkTime.TotalSeconds:F1}с");
        
        var alreadyExists = 0;
        for (int i = 0; i < candidateInfos.Count; i++)
        {
            if (!existenceResults[i])
            {
                characterInfos.Add(candidateInfos[i]);
            }
            else
            {
                alreadyExists++;
                Console.WriteLine($"⚠️ Персонаж {candidateInfos[i].id} уже существует в индексе");
            }
        }

        Console.WriteLine($"✅ Финальный результат: {characterInfos.Count} новых персонажей (существующих: {alreadyExists})");
        return characterInfos;
    }

    public async Task<List<(string href, string id)>> ExtractCharacterUrlsAsync(IList<IWebElement> cards, ICharacterIndexManager indexManager)
    {
        var characterUrls = new List<(string href, string id)>(cards.Count);
        var candidateIds = new List<string>(cards.Count);
        var candidateUrls = new List<(string href, string id)>(cards.Count);

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

        var existenceResults = await Task.WhenAll(candidateIds.Select(indexManager.IsCharacterExistsAsync));
        
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
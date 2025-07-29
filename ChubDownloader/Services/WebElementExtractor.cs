using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.Extensions;
using System.Text;
using ChubDownloader.Infrastructure.Logging;
using ZLinq;

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
        StringBuilderLogger.WriteFormattedLine("🔍 Поиск JSON кнопки среди {0} селекторов...", WebDriverSettings.JsonButtonXPaths.Length);
        
        for (int i = 0; i < WebDriverSettings.JsonButtonXPaths.Length; i++)
        {
            var xpath = WebDriverSettings.JsonButtonXPaths[i];
            try
            {
                var findStart = DateTime.Now;
                var element = _webDriverService.Driver.FindElement(By.XPath(xpath));
                var findTime = DateTime.Now - findStart;
                
                StringBuilderLogger.WriteFormattedLine("🔍 Селектор {0}: найден элемент за {1:F0}мс", i + 1, findTime.TotalMilliseconds);
                
                if (element.Displayed && element.Enabled)
                {
                    StringBuilderLogger.WriteFormattedLine("✅ JSON кнопка найдена с селектором {0}: {1}", i + 1, xpath);
                    return element;
                }
                else
                {
                    StringBuilderLogger.WriteFormattedLine("⚠️ Селектор {0}: элемент найден, но недоступен (Displayed: {1}, Enabled: {2})", i + 1, element.Displayed, element.Enabled);
                }
            }
            catch (Exception ex)
            {
                StringBuilderLogger.LogWarning($"Селектор {i + 1} не сработал: {ex.GetType().Name}", ex);
            }
        }
        
        StringBuilderLogger.WriteFormattedLine("❌ JSON кнопка не найдена ни с одним из {0} селекторов", WebDriverSettings.JsonButtonXPaths.Length);
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
                .FirstOrDefaultOptimized(el => el.Displayed);

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
        StringBuilderLogger.WriteFormattedLine("🔍 Извлекаем информацию из {0} карточек (минимум чатов: {1}, проверка чатов: {2})", cards.Count, minChats, checkChatCount);
        
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
                        StringBuilderLogger.LogWarning($"Пропущен {id}: недостаточно чатов ({chatCount} < {minChats})");
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
                StringBuilderLogger.LogError($"Ошибка обработки карточки: {ex.Message}", ex);
            }
        }
        var extractTime = DateTime.Now - extractStart;
        
        StringBuilderLogger.WriteFormattedLine("📊 Первый проход: найдено {0} кандидатов, пропущено по чатам: {1}, невалидных: {2} за {3:F1}с", candidateInfos.Count, skippedByChats, invalidCards, extractTime.TotalSeconds);

        var checkStart = DateTime.Now;
        var tasks = new List<Task<bool>>();
        candidateIds.AsValueEnumerable().Select(indexManager.IsCharacterExistsAsync).CopyTo(tasks);
        var existenceResults = await Task.WhenAll(tasks);
        var checkTime = DateTime.Now - checkStart;
        
        StringBuilderLogger.WriteFormattedLine("🔍 Проверка существования {0} персонажей заняла: {1:F1}с", candidateIds.Count, checkTime.TotalSeconds);
        
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
                StringBuilderLogger.LogInfo($"Персонаж {candidateInfos[i].id} уже существует в индексе");
            }
        }

        StringBuilderLogger.WriteFormattedLine("✅ Финальный результат: {0} новых персонажей (существующих: {1})", characterInfos.Count, alreadyExists);
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

        var tasks = new List<Task<bool>>();
        candidateIds.AsValueEnumerable().Select(indexManager.IsCharacterExistsAsync).CopyTo(tasks);
        var existenceResults = await Task.WhenAll(tasks);
        
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
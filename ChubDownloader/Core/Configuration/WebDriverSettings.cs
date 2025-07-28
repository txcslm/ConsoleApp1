namespace ChubDownloader.Core.Configuration;

public sealed class WebDriverSettings
{
    public static readonly string[] JsonButtonXPaths = 
    [
        "//*[@id=\"root\"]/div/div/div/main/div/div[1]/div[1]/div[2]/div/button[2]",
        "//*[@id=\"root\"]/div/div/div/main/div/div[2]/div[1]/div[2]/div/button[2]"
    ];
    
    public const string CharacterListSelector = "#chara-list > a.cursor-pointer";
    public const string CharactersPageSelector = "#chara-list a.cursor-pointer";
    public const string NextPageXPath = "//*[@id='rc-tabs-1-panel-characters']/ul[1]/li[@title='Next Page']";
    public const string AntPaginationNextSelector = ".ant-pagination-next[title='Next Page']";
    public const string AntTooltipInnerSelector = ".ant-tooltip-inner";
    public const string MainTableRowSelector = "main table tbody tr";
    public const string UserLinkSelector = "td:nth-child(2) a";
    public const string CharacterTabSelector = "div[role='tab']";
    public const string IconBlockSelector = "span.fake-ribbon > div";
    
    public const string LeaderboardUrl = "https://chub.ai/leaderboard?segment=followers";
    public const string BaseUrl = "https://chub.ai";
}
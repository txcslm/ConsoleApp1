using System.Globalization;
using ZLinq;

namespace ChubDownloader.Core.Extensions;

public static class StringExtensions
{
    public static int ParseChatCount(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;
            
        var parts = text.Split(',');
        var chatPart = parts.AsValueEnumerable().Where(p => p.Contains("chats")).FirstOrDefault();
        if (chatPart == null) return 0;

        var numStr = chatPart.Replace("Total:", "")
            .Replace("chats", "")
            .Trim()
            .ToLower();

        if (numStr.EndsWith($"k"))
        {
            if (double.TryParse(numStr.TrimEnd('k'), CultureInfo.InvariantCulture, out var val))
                return (int)(val * 1000);
        }

        return int.TryParse(numStr, out var result) ? result : 0;
    }
    
    public static string ExtractCharacterId(this string? href)
    {
        return string.IsNullOrEmpty(href) ? string.Empty : href.ExtractCharacterIdOptimized();
    }
}
using System.Text;
using System.Runtime.CompilerServices;

namespace ChubDownloader.Core.Extensions;

public static class StringOptimizationExtensions
{
    private static readonly ThreadLocal<StringBuilder> ThreadLocalStringBuilder = 
        new(() => new StringBuilder(256));
    
    /// <summary>
    /// Optimized string concatenation using thread-local StringBuilder
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ConcatOptimized(params ReadOnlySpan<string> values)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        
        foreach (var value in values)
        {
            if (!string.IsNullOrEmpty(value))
                sb.Append(value);
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Optimized string building for logging
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BuildLogMessage(string level, string timestamp, string message)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append(level);
        sb.Append(' ');
        sb.Append(timestamp);
        sb.Append(" - ");
        sb.Append(message);
        return sb.ToString();
    }
    
    /// <summary>
    /// Zero-allocation string formatting for simple patterns
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatOptimized(string template, string value1)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        
        var index = template.IndexOf("{0}");
        if (index >= 0)
        {
            sb.Append(template.AsSpan(0, index));
            sb.Append(value1);
            sb.Append(template.AsSpan(index + 3));
        }
        else
        {
            sb.Append(template);
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Zero-allocation string formatting for two parameters
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatOptimized(string template, string value1, string value2)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        
        var span = template.AsSpan();
        var lastIndex = 0;
        
        var index1 = span.IndexOf("{0}");
        if (index1 >= 0)
        {
            sb.Append(span.Slice(lastIndex, index1 - lastIndex));
            sb.Append(value1);
            lastIndex = index1 + 3;
            
            var index2 = span.Slice(lastIndex).IndexOf("{1}");
            if (index2 >= 0)
            {
                index2 += lastIndex;
                sb.Append(span.Slice(lastIndex, index2 - lastIndex));
                sb.Append(value2);
                lastIndex = index2 + 3;
            }
        }
        
        sb.Append(span.Slice(lastIndex));
        return sb.ToString();
    }
    
    /// <summary>
    /// Fast character ID extraction without regex
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ExtractCharacterIdOptimized(this string href)
    {
        if (string.IsNullOrEmpty(href))
            return string.Empty;
            
        var span = href.AsSpan();
        var lastSlash = span.LastIndexOf('/');
        
        if (lastSlash >= 0 && lastSlash < span.Length - 1)
        {
            return span.Slice(lastSlash + 1).ToString();
        }
        
        return string.Empty;
    }
    
    /// <summary>
    /// Optimized progress message formatting
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatProgressMessage(string emoji, string action, int current, int total)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append(emoji);
        sb.Append(' ');
        sb.Append(action);
        sb.Append(' ');
        sb.Append(current);
        sb.Append('/');
        sb.Append(total);
        return sb.ToString();
    }
    
    /// <summary>
    /// Optimized folder creation message
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatFolderCreated(string folderPath)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append("📁 Создана папка: ");
        sb.Append(folderPath);
        return sb.ToString();
    }
    
    /// <summary>
    /// Optimized timing message formatting
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string FormatTiming(string action, double seconds)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append("⏱️ ");
        sb.Append(action);
        sb.Append(": ");
        sb.Append(seconds.ToString("F1"));
        sb.Append('с');
        return sb.ToString();
    }
}
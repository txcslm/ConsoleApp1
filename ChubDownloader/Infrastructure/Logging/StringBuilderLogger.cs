using System.Text;
using ChubDownloader.Core.Extensions;

namespace ChubDownloader.Infrastructure.Logging;

public enum LogLevel
{
    Error,
    Warning,
    Info
}

public static class StringBuilderLogger
{
    private static readonly StringBuilder _stringBuilder = new(1024);
    private static readonly object _lock = new();
    
    private const string ErrorColor = "\u001b[31m";
    private const string WarningColor = "\u001b[33m";
    private const string InfoColor = "\u001b[34m";
    private const string DuplicateColor = "\u001b[35m"; // Фиолетовый
    private const string ResetColor = "\u001b[0m";

    public static void WriteLine(string message)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(message);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteLine(ReadOnlySpan<char> prefix, string message)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            _stringBuilder.Append(' ');
            _stringBuilder.Append(message);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteLine(ReadOnlySpan<char> prefix, ReadOnlySpan<char> message)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append(prefix);
            _stringBuilder.Append(' ');
            _stringBuilder.Append(message);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteFormattedLine(string format, params object[] args)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.AppendFormat(format, args);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    private static void WriteLog(LogLevel level, string message, string? stackTrace = null)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            
            var (color, prefix) = level switch
            {
                LogLevel.Error => (ErrorColor, "❌ [ERROR]"),
                LogLevel.Warning => (WarningColor, "⚠️ [WARNING]"),
                LogLevel.Info => (InfoColor, "ℹ️ [INFO]"),
                _ => (InfoColor, "ℹ️ [INFO]")
            };
            
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _stringBuilder.Append(color);
            _stringBuilder.Append(prefix);
            _stringBuilder.Append(' ');
            _stringBuilder.Append(timestamp);
            _stringBuilder.Append(" - ");
            _stringBuilder.Append(message);
            _stringBuilder.Append(ResetColor);
            
            Console.WriteLine(_stringBuilder.ToString());
            
            if (!string.IsNullOrEmpty(stackTrace) && (level == LogLevel.Error || level == LogLevel.Warning))
            {
                _stringBuilder.Clear();
                _stringBuilder.Append(color);
                _stringBuilder.Append("[STACK TRACE] ");
                _stringBuilder.Append(stackTrace);
                _stringBuilder.Append(ResetColor);
                Console.WriteLine(_stringBuilder.ToString());
            }
        }
    }
    
    public static void LogError(string message, Exception? exception = null)
    {
        var stackTrace = exception?.StackTrace ?? Environment.StackTrace;
        WriteLog(LogLevel.Error, message, stackTrace);
    }
    
    public static void LogWarning(string message, Exception? exception = null)
    {
        var stackTrace = exception?.StackTrace ?? Environment.StackTrace;
        WriteLog(LogLevel.Warning, message, stackTrace);
    }
    
    public static void LogInfo(string message)
    {
        WriteLog(LogLevel.Info, message);
    }
    
    [Obsolete("Use LogError instead")]
    public static void WriteError(string message)
    {
        LogError(message);
    }

    public static void WriteSuccess(string message)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("✅ ");
            _stringBuilder.Append(message);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteProgress(string message)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("📊 ");
            _stringBuilder.Append(message);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    [Obsolete("Use LogWarning instead")]
    public static void WriteWarning(string message)
    {
        LogWarning(message);
    }

    [Obsolete("Use LogInfo instead")]
    public static void WriteInfo(string message)
    {
        LogInfo(message);
    }

    public static void WriteTiming(string characterId, double delayMs, double clearMs, double findMs, double clickMs, double waitSec)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("🔧 ");
            _stringBuilder.Append(characterId);
            _stringBuilder.Append(": delay=");
            _stringBuilder.Append(delayMs.ToString("F0"));
            _stringBuilder.Append("мс, clear=");
            _stringBuilder.Append(clearMs.ToString("F0"));
            _stringBuilder.Append("мс, find=");
            _stringBuilder.Append(findMs.ToString("F0"));
            _stringBuilder.Append("мс, click=");
            _stringBuilder.Append(clickMs.ToString("F0"));
            _stringBuilder.Append("мс, wait=");
            _stringBuilder.Append(waitSec.ToString("F1"));
            _stringBuilder.Append('с');
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteCharacterProgress(int current, int total, string characterId, int chatCount)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("📥 [");
            _stringBuilder.Append(current);
            _stringBuilder.Append('/');
            _stringBuilder.Append(total);
            _stringBuilder.Append("] Загружаем: ");
            _stringBuilder.Append(characterId);
            _stringBuilder.Append(" (чатов: ");
            _stringBuilder.Append(chatCount);
            _stringBuilder.Append(')');
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteCharacterSuccess(int current, int total, string characterId, double totalSeconds)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("✅ [");
            _stringBuilder.Append(current);
            _stringBuilder.Append('/');
            _stringBuilder.Append(total);
            _stringBuilder.Append("] ");
            _stringBuilder.Append(characterId);
            _stringBuilder.Append(" загружен за ");
            _stringBuilder.Append(totalSeconds.ToString("F1"));
            _stringBuilder.Append('с');
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteCharacterError(int current, int total, string characterId, string errorMessage)
    {
        lock (_lock)
        {
            _stringBuilder.Clear();
            _stringBuilder.Append("❌ [");
            _stringBuilder.Append(current);
            _stringBuilder.Append('/');
            _stringBuilder.Append(total);  
            _stringBuilder.Append("] ");
            _stringBuilder.Append(characterId);
            _stringBuilder.Append(" - ");
            _stringBuilder.Append(errorMessage);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }

    public static void WriteDuplicateInfo(string characterId)
    {
        lock (_lock)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            _stringBuilder.Clear();
            _stringBuilder.Append(DuplicateColor);
            _stringBuilder.Append("🔁 [DUPLICATE] ");
            _stringBuilder.Append(timestamp);
            _stringBuilder.Append(" - Character ");
            _stringBuilder.Append(characterId);
            _stringBuilder.Append(" already exists. Skipping download.");
            _stringBuilder.Append(ResetColor);
            Console.WriteLine(_stringBuilder.ToString());
        }
    }
}
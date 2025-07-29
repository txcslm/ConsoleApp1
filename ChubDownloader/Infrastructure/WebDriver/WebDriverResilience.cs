using OpenQA.Selenium;
using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Infrastructure.WebDriver;

public static class WebDriverResilience
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 5000;
    
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        Exception? lastException = null;

        while (attempts < MaxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                attempts++;
                StringBuilderLogger.LogInfo($"Попытка {attempts}: {operationName}");
                
                return await operation();
            }
            catch (WebDriverTimeoutException ex)
            {
                lastException = ex;
                StringBuilderLogger.LogWarning($"Timeout при {operationName}, попытка {attempts}/{MaxRetries}", ex);
                
                if (attempts < MaxRetries)
                {
                    StringBuilderLogger.LogInfo($"Пауза {RetryDelayMs}мс перед повтором...");
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }
            catch (WebDriverException ex) when (ex.Message.Contains("Timed out receiving message from renderer"))
            {
                lastException = ex;
                StringBuilderLogger.LogWarning($"Chrome renderer timeout при {operationName}, попытка {attempts}/{MaxRetries}", ex);
                
                if (attempts < MaxRetries)
                {
                    StringBuilderLogger.LogInfo($"Пауза {RetryDelayMs}мс перед повтором...");
                    await Task.Delay(RetryDelayMs, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                StringBuilderLogger.LogError($"Неожиданная ошибка при {operationName}: {ex.Message}", ex);
                throw;
            }
        }

        StringBuilderLogger.LogError($"Все {MaxRetries} попытки неуспешны для {operationName}");
        throw lastException ?? new InvalidOperationException($"Операция {operationName} не удалась");
    }
    
    public static async Task ExecuteWithRetryAsync(
        Func<Task> operation, 
        string operationName,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithRetryAsync(async () =>
        {
            await operation();
            return true;
        }, operationName, cancellationToken);
    }
    
    public static bool IsRecoverableWebDriverError(Exception ex)
    {
        return ex is WebDriverTimeoutException ||
               (ex is WebDriverException webEx && 
                (webEx.Message.Contains("Timed out receiving message from renderer") ||
                 webEx.Message.Contains("timeout") ||
                 webEx.Message.Contains("not reachable")));
    }
}
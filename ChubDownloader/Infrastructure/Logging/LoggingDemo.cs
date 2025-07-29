using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Infrastructure.Logging;

public static class LoggingDemo
{
    public static void DemonstrateLogging()
    {
        StringBuilderLogger.LogInfo("Демонстрация новой системы логирования:");
        
        // Тест обычных логов
        StringBuilderLogger.LogInfo("Это информационное сообщение (синий цвет)");
        StringBuilderLogger.LogWarning("Это предупреждение (желтый цвет)");
        StringBuilderLogger.LogError("Это ошибка (красный цвет)");
        
        // Тест дубликата
        StringBuilderLogger.WriteDuplicateInfo("test-character-123");
        
        // Тест с исключением
        try
        {
            throw new InvalidOperationException("Тестовое исключение для демонстрации стек-трейса");
        }
        catch (Exception ex)
        {
            StringBuilderLogger.LogError("Тест ошибки со стек-трейсом", ex);
        }
        
        StringBuilderLogger.LogInfo("Демонстрация завершена!");
    }
}
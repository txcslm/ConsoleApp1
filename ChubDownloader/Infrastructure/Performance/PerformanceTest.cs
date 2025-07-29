using System.Diagnostics;
using ChubDownloader.Core.Extensions;
using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Infrastructure.Performance;

public static class PerformanceTest
{
    public static void RunPerformanceTests()
    {
        StringBuilderLogger.LogInfo("🚀 Запуск тестов производительности...");
        
        TestStringOperations();
        TestCollectionOperations();
        TestLoggingOperations();
        
        StringBuilderLogger.LogInfo("✅ Тесты производительности завершены");
    }
    
    private static void TestStringOperations()
    {
        const int iterations = 100_000;
        var testData = new[] { "https://chub.ai/characters/user/character123", "https://example.com/test/abc456" };
        
        // Test old approach with string interpolation
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var href = testData[i % testData.Length];
            var _ = $"📁 Создана папка: {href}";
        }
        sw.Stop();
        var oldTime = sw.ElapsedMilliseconds;
        
        // Test optimized approach
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var href = testData[i % testData.Length];
            var _ = StringOptimizationExtensions.FormatFolderCreated(href);
        }
        sw.Stop();
        var newTime = sw.ElapsedMilliseconds;
        
        var improvement = ((double)(oldTime - newTime) / oldTime) * 100;
        StringBuilderLogger.LogInfo($"📊 String Operations: Старый метод: {oldTime}мс, Новый: {newTime}мс, Улучшение: {improvement:F1}%");
    }
    
    private static void TestCollectionOperations()
    {
        const int iterations = 10_000;
        var testData = Enumerable.Range(1, 1000).ToArray();
        
        // Test old approach with LINQ
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var _ = testData.Where(x => x % 2 == 0).ToArray();
        }
        sw.Stop();
        var oldTime = sw.ElapsedMilliseconds;
        
        // Test optimized approach with ZLinq
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var _ = testData.WhereOptimized(x => x % 2 == 0).ToArrayOptimized();
        }
        sw.Stop();
        var newTime = sw.ElapsedMilliseconds;
        
        var improvement = ((double)(oldTime - newTime) / oldTime) * 100;
        StringBuilderLogger.LogInfo($"📊 Collection Operations: Старый метод: {oldTime}мс, Новый: {newTime}мс, Улучшение: {improvement:F1}%");
    }
    
    private static void TestLoggingOperations()
    {
        const int iterations = 50_000;
        
        // Test string interpolation logging
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var _ = $"[User {i}/1000] TestUser{i}";
        }
        sw.Stop();
        var oldTime = sw.ElapsedMilliseconds;
        
        // Test optimized StringBuilder approach
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var _ = StringOptimizationExtensions.FormatProgressMessage("👤", "User", i, 1000);
        }
        sw.Stop();
        var newTime = sw.ElapsedMilliseconds;
        
        var improvement = ((double)(oldTime - newTime) / oldTime) * 100;
        StringBuilderLogger.LogInfo($"📊 Logging Operations: Старый метод: {oldTime}мс, Новый: {newTime}мс, Улучшение: {improvement:F1}%");
    }
}
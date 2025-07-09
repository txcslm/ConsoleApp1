using ChubDownloader.Views;
using ChubDownloader.Presenters;
using ChubDownloader.Services;
using ChubDownloader.Services.Strategies;
using ChubDownloader.Infrastructure.WebDriver;
using ChubDownloader.Infrastructure.FileSystem;
using ChubDownloader.Core.Configuration;
using ChubDownloader.Core.DependencyInjection;

namespace ChubDownloader;

internal static class Program
{
    private static void Main(string[] args)
    {
        var services = ConfigureServices();
        
        var view = services.GetRequiredService<ConsoleView>();
        var presenter = services.GetRequiredService<MainPresenter>();
        
        try
        {
            view.Start();
            
            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
        finally
        {
            // Cleanup WebDriver
            var webDriver = services.GetService<IWebDriverService>();
            webDriver?.Dispose();
        }
    }
    
    private static ServiceCollection ConfigureServices()
    {
        var services = new ServiceCollection();
        
        // Configuration
        var downloadPath = Path.Combine(Environment.CurrentDirectory, AppSettings.TempDownloadsFolderName);
        var profilePath = Path.Combine(Directory.GetCurrentDirectory(), AppSettings.ChromeProfileFolderName);
        Directory.CreateDirectory(downloadPath);
        Directory.CreateDirectory(profilePath);
        
        // Infrastructure services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IWebDriverService>(provider => new WebDriverService(downloadPath, profilePath));
        
        // Business services
        services.AddSingleton<IDownloadService>(provider => 
            new DownloadService(provider.GetRequiredService<IFileSystemService>()));
        services.AddSingleton<ICharacterIndexManager>(provider => 
            new CharacterIndexManager(provider.GetRequiredService<IFileSystemService>()));
        services.AddSingleton<IWebElementExtractor>(provider => 
            new WebElementExtractor(provider.GetRequiredService<IWebDriverService>()));
        services.AddSingleton<INavigationService>(provider => 
            new NavigationService(provider.GetRequiredService<IWebDriverService>()));
        services.AddSingleton<IProgressReporter, ProgressReporter>();
        
        // Strategies
        services.AddTransient<LeaderboardScrapingStrategy>(provider => 
            new LeaderboardScrapingStrategy(
                provider.GetRequiredService<IWebDriverService>(),
                provider.GetRequiredService<IWebElementExtractor>(),
                provider.GetRequiredService<INavigationService>(),
                provider.GetRequiredService<IProgressReporter>(),
                provider.GetRequiredService<ICharacterIndexManager>(),
                provider.GetRequiredService<IDownloadService>()));
                
        services.AddTransient<SegmentScrapingStrategy>(provider => 
            new SegmentScrapingStrategy(
                provider.GetRequiredService<IWebDriverService>(),
                provider.GetRequiredService<IWebElementExtractor>(),
                provider.GetRequiredService<INavigationService>(),
                provider.GetRequiredService<IProgressReporter>(),
                provider.GetRequiredService<ICharacterIndexManager>(),
                provider.GetRequiredService<IDownloadService>()));
        
        services.AddSingleton<IScrapingStrategyFactory>(provider => 
            new ScrapingStrategyFactory(provider));
        services.AddSingleton<ICharacterScrapingOrchestrator>(provider => 
            new CharacterScrapingOrchestrator(provider.GetRequiredService<IScrapingStrategyFactory>(), downloadPath));
        
        // View and Presenter
        services.AddSingleton(new ConsoleView());
        services.AddSingleton<IUserInteraction>(provider => provider.GetRequiredService<ConsoleView>());
        services.AddSingleton<IProgressDisplay>(provider => provider.GetRequiredService<ConsoleView>());
        services.AddSingleton<IViewStateManager>(provider => provider.GetRequiredService<ConsoleView>());
        services.AddSingleton<MainPresenter>(provider =>
            new MainPresenter(
                provider.GetRequiredService<IUserInteraction>(),
                provider.GetRequiredService<IProgressDisplay>(),
                provider.GetRequiredService<IViewStateManager>(),
                provider.GetRequiredService<ICharacterScrapingOrchestrator>()));
        
        return services;
    }
}
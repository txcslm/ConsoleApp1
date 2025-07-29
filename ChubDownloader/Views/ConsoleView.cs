using ChubDownloader.Models;
using ChubDownloader.Infrastructure.Logging;

namespace ChubDownloader.Views;

public sealed class ConsoleView : IMainView, IUserInteraction, IProgressDisplay, IViewStateManager
{
        public event EventHandler<DownloadEventArgs>? DownloadRequested;
        
        public void Start()
        {
            StringBuilderLogger.WriteLine("=== Chub.ai Characters JSON Downloader ===");
            
            var mode = GetDownloadModeWithValidation();
            var args = new DownloadEventArgs { Mode = mode };
            
            if (mode == DownloadMode.SegmentPages)
            {
                args.Segment = GetSegmentWithValidation();
                args.MinChats = GetMinChatsWithValidation();
                args.StartPage = GetStartPageWithValidation();
                args.PagesToScan = GetPagesToScanWithValidation();
            }
            else if (mode == DownloadMode.CharactersPages)
            {
                args.MinChats = GetMinChatsWithValidation();
                args.StartPage = GetStartPageWithValidation();
                args.PagesToScan = GetPagesToScanWithValidation();
            }
            
            DownloadRequested?.Invoke(this, args);
        }
        
        private DownloadMode GetDownloadModeWithValidation()
        {
            while (true)
            {
                StringBuilderLogger.WriteLine("\nВыберите режим работы:");
                StringBuilderLogger.WriteLine("1. Скачать персонажей из лидерборда (followers)");
                StringBuilderLogger.WriteLine("2. Скачать персонажей с обычных страниц (по сегментам)");
                StringBuilderLogger.WriteLine("3. Скачать персонажей с основной страницы персонажей");
                Console.Write("\nВаш выбор (1, 2 или 3): ");
                
                var choice = Console.ReadLine()?.Trim();
                
                switch (choice)
                {
                    case "1":
                        return DownloadMode.Leaderboard;
                    case "2":
                        return DownloadMode.SegmentPages;
                    case "3":
                        return DownloadMode.CharactersPages;
                    default:
                        StringBuilderLogger.LogError("Неверный выбор! Пожалуйста, введите 1, 2 или 3.");
                        continue;
                }
            }
        }
        
        private Segment GetSegmentWithValidation()
        {
            while (true)
            {
                StringBuilderLogger.WriteLine("\nВыберите сегмент:");
                StringBuilderLogger.WriteLine("1. Quality (качественные персонажи)");
                StringBuilderLogger.WriteLine("2. Newcomer (новые персонажи)");
                StringBuilderLogger.WriteLine("3. Trending (популярные)");
                StringBuilderLogger.WriteLine("4. Timeline (временная лента)");
                StringBuilderLogger.WriteLine("5. Evergreen (вечнозеленые)");
                Console.Write("\nВаш выбор (1-5): ");
                
                var choice = Console.ReadLine()?.Trim();
                
                switch (choice)
                {
                    case "1":
                        return Segment.Quality;
                    case "2":
                        return Segment.Newcomer;
                    case "3":
                        return Segment.Trending;
                    case "4":
                        return Segment.Timeline;
                    case "5":
                        return Segment.Evergreen;
                    default:
                        StringBuilderLogger.LogError("Неверный выбор! Пожалуйста, введите число от 1 до 5.");
                        continue;
                }
            }
        }
        
        private int GetMinChatsWithValidation()
        {
            while (true)
            {
                Console.Write("\nМинимальное количество чатов (0 = без ограничений, например 10000): ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                {
                    StringBuilderLogger.LogError("Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int minChats) && minChats >= 0)
                {
                    if (minChats == 0)
                    {
                        StringBuilderLogger.WriteSuccess("Будут скачиваться все персонажи без ограничений по чатам.");
                    }
                    else
                    {
                        StringBuilderLogger.WriteFormattedLine("✅ Будут скачиваться персонажи с количеством чатов от {0:N0}.", minChats);
                    }
                    return minChats;
                }
                
                StringBuilderLogger.LogError("Неверный формат! Пожалуйста, введите целое число больше или равное 0.");
            }
        }
        
        private int GetStartPageWithValidation()
        {
            while (true)
            {
                Console.Write("\nС какой страницы начать сканирование (например, 1): ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                {
                    StringBuilderLogger.LogError("Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int startPage) && startPage >= 1)
                {
                    StringBuilderLogger.WriteFormattedLine("✅ Начинаем сканирование с страницы {0}.", startPage);
                    return startPage;
                }
                
                StringBuilderLogger.LogError("Неверный формат! Пожалуйста, введите целое число больше 0.");
            }
        }
        
        private int GetPagesToScanWithValidation()
        {
            while (true)
            {
                Console.Write("\nСколько страниц сканировать (например, 5): ");
                var input = Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(input))
                {
                    StringBuilderLogger.LogError("Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int pagesToScan) && pagesToScan >= 1)
                {
                    StringBuilderLogger.WriteFormattedLine("✅ Будет просканировано {0} страниц.", pagesToScan);
                    return pagesToScan;
                }
                
                StringBuilderLogger.LogError("Неверный формат! Пожалуйста, введите целое число больше 0.");
            }
        }
        
        public void ShowMessage(string message)
        {
            StringBuilderLogger.WriteSuccess(message);
        }
        
        public void ShowError(string error)
        {
            StringBuilderLogger.LogError(error);
        }
        
        public void UpdateProgress(string progress)
        {
            StringBuilderLogger.WriteProgress(progress);
        }
        
        public void SetEnabled(bool enabled)
        {
            // В консольном приложении не используется
        }
        
        // Устаревшие методы для совместимости
        public DownloadMode GetDownloadMode()
        {
            return GetDownloadModeWithValidation();
        }
        
        public Segment GetSegment()
        {
            return GetSegmentWithValidation();
        }
        
        public int GetMinChats()
        {
            return GetMinChatsWithValidation();
        }
        
        public int GetPagesToScan()
        {
            return GetPagesToScanWithValidation();
        }
        
        public int GetStartPage()
        {
            return GetStartPageWithValidation();
        }
}
using ChubDownloader.Models;

namespace ChubDownloader.Views
{
    public class ConsoleView : IMainView
    {
        public event EventHandler<DownloadEventArgs> DownloadRequested;
        
        public void Start()
        {
            Console.WriteLine("=== Chub.ai Characters JSON Downloader ===");
            
            var mode = GetDownloadModeWithValidation();
            var args = new DownloadEventArgs { Mode = mode };
            
            if (mode == DownloadMode.SegmentPages)
            {
                args.Segment = GetSegmentWithValidation();
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
                Console.WriteLine("\nВыберите режим работы:");
                Console.WriteLine("1. Скачать персонажей из лидерборда (followers)");
                Console.WriteLine("2. Скачать персонажей с обычных страниц");
                Console.Write("\nВаш выбор (1 или 2): ");
                
                var choice = Console.ReadLine()?.Trim();
                
                switch (choice)
                {
                    case "1":
                        return DownloadMode.Leaderboard;
                    case "2":
                        return DownloadMode.SegmentPages;
                    default:
                        Console.WriteLine("❌ Неверный выбор! Пожалуйста, введите 1 или 2.");
                        continue;
                }
            }
        }
        
        private Segment GetSegmentWithValidation()
        {
            while (true)
            {
                Console.WriteLine("\nВыберите сегмент:");
                Console.WriteLine("1. Quality (качественные персонажи)");
                Console.WriteLine("2. Newcomer (новые персонажи)");
                Console.WriteLine("3. Trending (популярные)");
                Console.WriteLine("4. Timeline (временная лента)");
                Console.WriteLine("5. Evergreen (вечнозеленые)");
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
                        Console.WriteLine("❌ Неверный выбор! Пожалуйста, введите число от 1 до 5.");
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
                    Console.WriteLine("❌ Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int minChats) && minChats >= 0)
                {
                    if (minChats == 0)
                    {
                        Console.WriteLine("✅ Будут скачиваться все персонажи без ограничений по чатам.");
                    }
                    else
                    {
                        Console.WriteLine($"✅ Будут скачиваться персонажи с количеством чатов от {minChats:N0}.");
                    }
                    return minChats;
                }
                
                Console.WriteLine("❌ Неверный формат! Пожалуйста, введите целое число больше или равное 0.");
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
                    Console.WriteLine("❌ Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int startPage) && startPage >= 1)
                {
                    Console.WriteLine($"✅ Начинаем сканирование с страницы {startPage}.");
                    return startPage;
                }
                
                Console.WriteLine("❌ Неверный формат! Пожалуйста, введите целое число больше 0.");
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
                    Console.WriteLine("❌ Пожалуйста, введите число.");
                    continue;
                }
                
                if (int.TryParse(input, out int pagesToScan) && pagesToScan >= 1)
                {
                    Console.WriteLine($"✅ Будет просканировано {pagesToScan} страниц.");
                    return pagesToScan;
                }
                
                Console.WriteLine("❌ Неверный формат! Пожалуйста, введите целое число больше 0.");
            }
        }
        
        public void ShowMessage(string message)
        {
            Console.WriteLine($"✅ {message}");
        }
        
        public void ShowError(string error)
        {
            Console.WriteLine($"❌ [ERROR] {error}");
        }
        
        public void UpdateProgress(string progress)
        {
            Console.WriteLine($"📊 {progress}");
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
    }
}
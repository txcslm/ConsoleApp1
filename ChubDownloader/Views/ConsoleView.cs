using ChubDownloader.Models;

namespace ChubDownloader.Views
{
    public class ConsoleView : IMainView
    {
        public event EventHandler<DownloadEventArgs> DownloadRequested;
        
        public void Start()
        {
            Console.WriteLine("=== Chub.ai Characters JSON Downloader ===");
            Console.WriteLine("\nВыберите режим работы:");
            Console.WriteLine("1. Скачать персонажей из лидерборда (followers)");
            Console.WriteLine("2. Скачать персонажей с обычных страниц");
            Console.Write("\nВаш выбор (1 или 2): ");
            
            var choice = Console.ReadLine();
            var args = new DownloadEventArgs();
            
            if (choice == "1")
            {
                args.Mode = DownloadMode.Leaderboard;
            }
            else if (choice == "2")
            {
                args.Mode = DownloadMode.SegmentPages;
                args.Segment = GetSegment();
                args.MinChats = GetMinChats();
                args.PagesToScan = GetPagesToScan();
            }
            else
            {
                ShowError("Неверный выбор!");
                return;
            }
            
            DownloadRequested?.Invoke(this, args);
        }
        
        public void ShowMessage(string message)
        {
            Console.WriteLine(message);
        }
        
        public void ShowError(string error)
        {
            Console.WriteLine($"[ERROR] {error}");
        }
        
        public void UpdateProgress(string progress)
        {
            Console.WriteLine($"[PROGRESS] {progress}");
        }
        
        public void SetEnabled(bool enabled)
        {
            // В консольном приложении не используется
        }
        
        public DownloadMode GetDownloadMode()
        {
            // Уже обработано в Start()
            return DownloadMode.Leaderboard;
        }
        
        public Segment GetSegment()
        {
            Console.WriteLine("\nВыберите сегмент:");
            Console.WriteLine("1. quality");
            Console.WriteLine("2. newcomer");
            Console.WriteLine("3. trending");
            Console.WriteLine("4. timeline");
            Console.WriteLine("5. evergreen");
            Console.Write("\nВаш выбор (1-5): ");
            
            var choice = Console.ReadLine();
            return choice switch
            {
                "1" => Segment.Quality,
                "2" => Segment.Newcomer,
                "3" => Segment.Trending,
                "4" => Segment.Timeline,
                "5" => Segment.Evergreen,
                _ => Segment.Quality
            };
        }
        
        public int GetMinChats()
        {
            Console.Write("\nМинимальное количество чатов (например, 10000): ");
            if (int.TryParse(Console.ReadLine(), out int minChats))
                return minChats;
            return 10000;
        }
        
        public int GetPagesToScan()
        {
            Console.Write("\nКоличество страниц для сканирования (например, 5): ");
            if (int.TryParse(Console.ReadLine(), out int pages))
                return pages;
            return 5;
        }
    }
}

using ChubDownloader.Views;
using ChubDownloader.Presenters;

namespace ChubDownloader
{
  class Program
  {
    static void Main(string[] args)
    {
      var view = new ConsoleView();
      var presenter = new MainPresenter(view);
            
      view.Start();
            
      Console.WriteLine("\nНажмите любую клавишу для выхода...");
      Console.ReadKey();
    }
  }
}
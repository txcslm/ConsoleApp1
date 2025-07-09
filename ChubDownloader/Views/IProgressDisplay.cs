namespace ChubDownloader.Views;

public interface IProgressDisplay
{
    void ShowMessage(string message);
    void ShowError(string error);
    void UpdateProgress(string progress);
}
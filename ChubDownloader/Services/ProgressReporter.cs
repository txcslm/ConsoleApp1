namespace ChubDownloader.Services;

public interface IProgressReporter
{
    void ReportProgress(IProgress<string> progress, string message);
    void ReportUserProgress(IProgress<string> progress, int currentUser, int totalUsers, string userName);
    void ReportPageProgress(IProgress<string> progress, int currentPage, int totalPages);
    void ReportCharacterProgress(IProgress<string> progress, string characterId, int chatCount);
    void ReportError(IProgress<string> progress, string error);
}

public sealed class ProgressReporter : IProgressReporter
{
    public void ReportProgress(IProgress<string> progress, string message)
    {
        progress.Report(message);
    }

    public void ReportUserProgress(IProgress<string> progress, int currentUser, int totalUsers, string userName)
    {
        progress.Report($"[User {currentUser}/{totalUsers}] {userName}");
    }

    public void ReportPageProgress(IProgress<string> progress, int currentPage, int totalPages)
    {
        progress.Report($"Страница {currentPage}/{totalPages} (осталось: {totalPages - currentPage + 1})");
    }

    public void ReportCharacterProgress(IProgress<string> progress, string characterId, int chatCount)
    {
        progress.Report($"{characterId} (чатов: {chatCount})");
    }

    public void ReportError(IProgress<string> progress, string error)
    {
        progress.Report($"Ошибка: {error}");
    }
}
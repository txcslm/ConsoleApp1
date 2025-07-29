using ChubDownloader.Core.Extensions;
using System.Text;

namespace ChubDownloader.Services;

public interface IProgressReporter
{
    void ReportProgress(IProgress<string> progress, string message);
    void ReportUserProgress(IProgress<string> progress, int currentUser, int totalUsers, string userName);
    void ReportPageProgress(IProgress<string> progress, int currentPage, int totalPages);
    void ReportCharacterProgress(IProgress<string> progress, string characterId, int chatCount);
    void ReportError(IProgress<string> progress, string? error);
}

public sealed class ProgressReporter : IProgressReporter
{
    private static readonly ThreadLocal<StringBuilder> ThreadLocalStringBuilder = 
        new(() => new StringBuilder(256));
    public void ReportProgress(IProgress<string> progress, string message)
    {
        progress?.Report(message ?? string.Empty);
    }

    public void ReportUserProgress(IProgress<string> progress, int currentUser, int totalUsers, string userName)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append("[User ");
        sb.Append(currentUser);
        sb.Append('/');
        sb.Append(totalUsers);
        sb.Append("] ");
        sb.Append(userName ?? "Unknown");
        progress?.Report(sb.ToString());
    }

    public void ReportPageProgress(IProgress<string> progress, int currentPage, int totalPages)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append("Страница ");
        sb.Append(currentPage);
        sb.Append('/');
        sb.Append(totalPages);
        sb.Append(" (осталось: ");
        sb.Append(totalPages - currentPage + 1);
        sb.Append(')');
        progress?.Report(sb.ToString());
    }

    public void ReportCharacterProgress(IProgress<string> progress, string characterId, int chatCount)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append(characterId ?? "Unknown");
        sb.Append(" (чатов: ");
        if (chatCount == int.MaxValue)
        {
            sb.Append("без ограничений");
        }
        else
        {
            sb.Append(chatCount);
        }
        sb.Append(')');
        progress?.Report(sb.ToString());
    }

    public void ReportError(IProgress<string> progress, string? error)
    {
        var sb = ThreadLocalStringBuilder.Value!;
        sb.Clear();
        sb.Append("Ошибка: ");
        sb.Append(error ?? "Unknown error");
        progress?.Report(sb.ToString());
    }
}
using ChubDownloader.Core.Configuration;
using ChubDownloader.Infrastructure.FileSystem;

namespace ChubDownloader.Services;

public sealed class DownloadService : IDownloadService
{
    private readonly IFileSystemService _fileSystemService;
    
    public DownloadService(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
    }

    public Task<bool> WaitForFileDownloadAsync(string tempPath, string targetPath, string characterId, string extension)
    {
        return _fileSystemService.WaitForFileDownloadAsync(tempPath, targetPath, characterId, extension);
    }


    public void ClearOldFiles(string rootPath, string extension)
    {
        _fileSystemService.ClearOldFiles(rootPath, extension);
    }
}
using System.Collections.Concurrent;

namespace ChubDownloader.Infrastructure.FileSystem;

public interface IFileSystemService
{
    Task<ConcurrentDictionary<string, string>> LoadCharacterIndexAsync(string indexFilePath);
    Task SaveCharacterIndexAsync(string indexFilePath, ConcurrentDictionary<string, string> index);
    bool CharacterExists(string[] folderPaths, string characterId, string extension);
    Task<bool> WaitForFileDownloadAsync(string tempPath, string targetPath, string characterId, string extension);
    void ClearOldFiles(string rootPath, string extension);
}
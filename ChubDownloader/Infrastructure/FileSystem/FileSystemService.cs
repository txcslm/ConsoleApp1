using System.Collections.Concurrent;
using System.Text.Json;
using ChubDownloader.Core.Configuration;

namespace ChubDownloader.Infrastructure.FileSystem;

public sealed class FileSystemService : IFileSystemService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    private readonly Dictionary<string, HashSet<string>> _fileIndexCache = new();
    private readonly object _indexLock = new();
    
    public async Task<ConcurrentDictionary<string, string>> LoadCharacterIndexAsync(string indexFilePath)
    {
        try
        {
            if (!File.Exists(indexFilePath))
                return new ConcurrentDictionary<string, string>();
                
            var json = await File.ReadAllTextAsync(indexFilePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>();
            return new ConcurrentDictionary<string, string>(dict);
        }
        catch
        {
            return new ConcurrentDictionary<string, string>();
        }
    }
    
    public async Task SaveCharacterIndexAsync(string indexFilePath, ConcurrentDictionary<string, string> index)
    {
        try
        {
            var json = JsonSerializer.Serialize(index, JsonOptions);
            await File.WriteAllTextAsync(indexFilePath, json);
        }
        catch
        {
            // Ignore
        }
    }
    
    public bool CharacterExists(string[] folderPaths, string characterId, string extension)
    {
        lock (_indexLock)
        {
            foreach (var folder in folderPaths)
            {
                if (!Directory.Exists(folder)) continue;
                
                var cacheKey = $"{folder}:{extension}";
                if (!_fileIndexCache.TryGetValue(cacheKey, out var characterIds))
                {
                    characterIds = BuildFileIndex(folder, extension);
                    _fileIndexCache[cacheKey] = characterIds;
                }
                
                if (characterIds.Contains(characterId))
                    return true;
            }
        }
        return false;
    }
    
    private HashSet<string> BuildFileIndex(string folder, string extension)
    {
        var characterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extLen = extension.Length;
        
        foreach (var filePath in Directory.EnumerateFiles(folder, $"*{extension}", SearchOption.TopDirectoryOnly))
        {
            var characterId = ExtractCharacterIdFromFilePath(filePath, extLen);
            if (!string.IsNullOrEmpty(characterId))
            {
                characterIds.Add(characterId);
            }
        }
        
        return characterIds;
    }
    
    private string? ExtractCharacterIdFromFilePath(string filePath, int extLen)
    {
        var idx1 = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        var idx2 = filePath.LastIndexOf(Path.AltDirectorySeparatorChar);
        var nameStart = Math.Max(idx1, idx2) + 1;
        var nameEnd = filePath.Length - extLen;
        
        if (nameEnd > nameStart)
        {
            return filePath.Substring(nameStart, nameEnd - nameStart);
        }
        
        return null;
    }
    
    private static bool IsCharacterFile(string filePath, string characterId, int idLen, int extLen)
    {
        var idx1 = filePath.LastIndexOf(Path.DirectorySeparatorChar);
        var idx2 = filePath.LastIndexOf(Path.AltDirectorySeparatorChar);
        var nameStart = Math.Max(idx1, idx2) + 1;
        
        return filePath.Length - nameStart - extLen == idLen &&
               string.Compare(filePath, nameStart, characterId, 0, idLen, StringComparison.OrdinalIgnoreCase) == 0;
    }
    
    public async Task<bool> WaitForFileDownloadAsync(string tempPath, string targetPath, string characterId, string extension)
    {
        try
        {
            Directory.CreateDirectory(targetPath);
            
            var maxAttempts = AppSettings.DownloadWaitMaxMs / AppSettings.DownloadCheckIntervalMs;
            var cutoffTime = DateTime.Now.AddSeconds(-AppSettings.RecentFileSeconds);
            
            for (int i = 0; i < maxAttempts; i++)
            {
                var sourceFile = GetMostRecentFile(tempPath, extension, cutoffTime);
                
                if (sourceFile != null && await IsFileStableAsync(sourceFile))
                {
                    var destFile = Path.Combine(targetPath, characterId + extension);
                    
                    if (CharacterExists([targetPath, AppSettings.CharactersFolderName], characterId, extension))
                    {
                        Console.WriteLine($"[DUPLICATE] Character {characterId} already exists. Skipping download.");
                        return false;
                    }
                    
                    File.Move(sourceFile.FullName, destFile);
                    return true;
                }
                
                await Task.Delay(AppSettings.DownloadCheckIntervalMs);
            }
            
            return false;
        }
        catch
        {
            return false;
        }
    }
    
    private static FileInfo? GetMostRecentFile(string tempPath, string extension, DateTime cutoffTime)
    {
        return Directory.GetFiles(tempPath, $"*{extension}")
            .Select(f => new FileInfo(f))
            .Where(f => f.CreationTime > cutoffTime)
            .OrderByDescending(f => f.CreationTime)
            .FirstOrDefault();
    }
    
    private static async Task<bool> IsFileStableAsync(FileInfo file)
    {
        var initialSize = file.Length;
        await Task.Delay(AppSettings.FileStableCheckDelayMs);
        file.Refresh();
        return file.Length == initialSize && initialSize > 0;
    }
    
    public void ClearOldFiles(string rootPath, string extension)
    {
        try
        {
            if (!Directory.Exists(rootPath))
                return;
                
            var cutoffTime = DateTime.Now.AddMinutes(-AppSettings.OldFileAgeMinutes);
            var oldFiles = Directory.GetFiles(rootPath, $"*{extension}")
                .Where(f => File.GetCreationTime(f) < cutoffTime);
                
            foreach (var file in oldFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore
                }
            }
        }
        catch
        {
            // Ignore
        }
    }
}
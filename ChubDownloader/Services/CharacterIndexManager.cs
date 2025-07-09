using System.Collections.Concurrent;
using System.Collections.Immutable;
using ChubDownloader.Infrastructure.FileSystem;
using ChubDownloader.Core.Configuration;

namespace ChubDownloader.Services;

public interface ICharacterIndexManager
{
    Task<bool> IsCharacterExistsAsync(string characterId);
    Task RegisterCharacterAsync(string characterId, string filePath);
    Task<ConcurrentDictionary<string, string>> LoadIndexAsync();
}

public static class CharacterIndexManagerFactory
{
    public static async Task<ICharacterIndexManager> CreateAsync(IFileSystemService fileSystemService)
    {
        var indexFilePath = Path.Combine(Environment.CurrentDirectory, AppSettings.CharacterIndexFileName);
        var index = await fileSystemService.LoadCharacterIndexAsync(indexFilePath);
        return new CharacterIndexManager(fileSystemService, indexFilePath, index);
    }
}

public sealed class CharacterIndexManager : ICharacterIndexManager, IDisposable
{
    private readonly IFileSystemService _fileSystemService;
    private readonly string _indexFilePath;
    private readonly ConcurrentDictionary<string, string> _index;
    private readonly SemaphoreSlim _saveSemaphore;
    private bool _disposed;

    public CharacterIndexManager(IFileSystemService fileSystemService, string indexFilePath, ConcurrentDictionary<string, string> index)
    {
        _fileSystemService = fileSystemService;
        _indexFilePath = indexFilePath;
        _index = index;
        _saveSemaphore = new SemaphoreSlim(1, 1);
    }

    public Task<bool> IsCharacterExistsAsync(string characterId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(_index.ContainsKey(characterId));
    }

    public async Task RegisterCharacterAsync(string characterId, string filePath)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        // Update index immediately (ConcurrentDictionary is thread-safe)
        _index[characterId] = filePath;
        
        // Serialize file saves to avoid conflicts
        await _saveSemaphore.WaitAsync();
        try
        {
            await _fileSystemService.SaveCharacterIndexAsync(_indexFilePath, _index);
        }
        finally
        {
            _saveSemaphore.Release();
        }
    }

    public async Task<ConcurrentDictionary<string, string>> LoadIndexAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return await _fileSystemService.LoadCharacterIndexAsync(_indexFilePath);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _saveSemaphore?.Dispose();
            _disposed = true;
        }
    }
}
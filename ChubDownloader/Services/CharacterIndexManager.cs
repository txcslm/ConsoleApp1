using System.Collections.Concurrent;
using ChubDownloader.Infrastructure.FileSystem;
using ChubDownloader.Core.Configuration;

namespace ChubDownloader.Services;

public interface ICharacterIndexManager
{
    Task<bool> IsCharacterExistsAsync(string characterId);
    Task RegisterCharacterAsync(string characterId, string filePath);
    Task<ConcurrentDictionary<string, string>> LoadIndexAsync();
}

public sealed class CharacterIndexManager : ICharacterIndexManager
{
    private readonly IFileSystemService _fileSystemService;
    private readonly string _indexFilePath;
    private readonly ConcurrentDictionary<string, string> _index;
    private readonly SemaphoreSlim _semaphore;

    public CharacterIndexManager(IFileSystemService fileSystemService)
    {
        _fileSystemService = fileSystemService;
        _indexFilePath = Path.Combine(Environment.CurrentDirectory, AppSettings.CharacterIndexFileName);
        _semaphore = new SemaphoreSlim(1, 1);
        _index = LoadIndexAsync().GetAwaiter().GetResult();
    }

    public async Task<bool> IsCharacterExistsAsync(string characterId)
    {
        await _semaphore.WaitAsync();
        try
        {
            return _index.ContainsKey(characterId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RegisterCharacterAsync(string characterId, string filePath)
    {
        await _semaphore.WaitAsync();
        try
        {
            _index[characterId] = filePath;
            await _fileSystemService.SaveCharacterIndexAsync(_indexFilePath, _index);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<ConcurrentDictionary<string, string>> LoadIndexAsync()
    {
        return await _fileSystemService.LoadCharacterIndexAsync(_indexFilePath);
    }
}
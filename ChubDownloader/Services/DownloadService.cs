using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace ChubDownloader.Services
{
    public class DownloadService : IDownloadService
    {
        private const int DownloadWaitMaxMs = 5000;
        private const int DownloadCheckIntervalMs = 100;
        private const int FileStableCheckDelayMs = 200;
        private const int RecentFileSeconds = 10;
        private const int OldFileAgeMinutes = 5;
        private const string CharactersFolderName = "characters";

        public bool WaitForFileDownload(string tempPath, string rootPath, string characterId, string extension)
        {
            try
            {
                // Ensure the central characters folder exists
                var charactersPath = Path.Combine(rootPath);
                Directory.CreateDirectory(charactersPath);

                int maxAttempts = DownloadWaitMaxMs / DownloadCheckIntervalMs;

                for (int i = 0; i < maxAttempts; i++)
                {
                    var files = Directory.GetFiles(tempPath, $"*{extension}")
                        .Select(f => new FileInfo(f))
                        .Where(f => f.CreationTime > DateTime.Now.AddSeconds(-RecentFileSeconds))
                        .OrderByDescending(f => f.CreationTime)
                        .ToList();

                    if (files.Any())
                    {
                        var sourceFile = files.First();

                        // Проверяем стабильность размера
                        var size1 = sourceFile.Length;
                        Thread.Sleep(FileStableCheckDelayMs);
                        sourceFile.Refresh();
                        var size2 = sourceFile.Length;

                        if (size1 == size2 && size1 > 0)
                        {
                            var destFile = Path.Combine(charactersPath, characterId + extension);

                            if (CharacterExists(charactersPath, characterId, extension))
                            {
                                Console.WriteLine($"[DUPLICATE] Character {characterId} already exists in '{CharactersFolderName}' folder. Skipping download.");
                                return false;
                            }

                            File.Move(sourceFile.FullName, destFile);
                            return true;
                        }
                    }

                    Thread.Sleep(DownloadCheckIntervalMs);
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool CharacterExists(string folderPath, string characterId, string extension)
        {
            var files = Directory.GetFiles(folderPath, $"*{extension}", SearchOption.TopDirectoryOnly);
            return files.Any(f => Path.GetFileNameWithoutExtension(f)
                .Equals(characterId, StringComparison.OrdinalIgnoreCase));
        }

        public void ClearOldFiles(string rootPath, string extension)
        {
            try
            {
                var charactersPath = Path.Combine(rootPath, CharactersFolderName);
                if (!Directory.Exists(charactersPath))
                    return;

                var oldFiles = Directory.GetFiles(charactersPath, $"*{extension}")
                    .Where(f => File.GetCreationTime(f) < DateTime.Now.AddMinutes(-OldFileAgeMinutes));

                foreach (var file in oldFiles)
                {
                    try { File.Delete(file); } catch { /* ignore */ }
                }
            }
            catch
            {
                // ignore
            }
        }
    }
}

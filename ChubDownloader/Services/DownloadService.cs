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
                string[] folders = [rootPath, "characters"];

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

                            if (CharacterExists(folders, characterId, extension))
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

        private static bool CharacterExists(string[] folderPaths, string characterId, string extension)
        {
            int extLen = extension.Length;
            int idLen  = characterId.Length;

            return (from folder in folderPaths
                from filePath in Directory.EnumerateFiles(folder, "*" + extension, SearchOption.TopDirectoryOnly)
                let idx1 = filePath.LastIndexOf(Path.DirectorySeparatorChar)
                let idx2 = filePath.LastIndexOf(Path.AltDirectorySeparatorChar)
                let nameStart = Math.Max(idx1, idx2) + 1
                where filePath.Length - nameStart - extLen == idLen
                where string.Compare(filePath, // исходная строка
                          nameStart, // смещение, откуда сравнивать
                          characterId, // что сравнивать
                          0, // смещение в characterId
                          idLen, // сколько символов сравнивать
                          StringComparison.OrdinalIgnoreCase) ==
                      0
                select filePath).Any();
        }

        public void ClearOldFiles(string rootPath, string extension)
        {
            try
            {
                var charactersPath = Path.Combine(rootPath);
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

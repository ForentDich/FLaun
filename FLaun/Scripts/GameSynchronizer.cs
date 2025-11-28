using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CmlLib.Core;
using System.Windows;
using Newtonsoft.Json;
using System.Threading;
using System.IO.Compression;
using SharpCompress.Archives.Tar;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace FLaun.Scripts
{
    public class GameSynchronizer
    {
        private string gameJsonUrl { get; set; }
        private string localGamePath { get; set; }
        private string gameBaseUrl { get; set; }
        private string gameArchivesUrl { get; set; }
        private string gameArchivesListUrl { get; set; }

        public event Action<int> ProgressChanged;
        public event Action<string> StatusChanged;

        private int semaphoreCount = 5;

        private readonly object progressLock = new object();

        public GameSynchronizer(string localGamePath, string gameJsonUrl, string gameBaseUrl, string gameArchivesUrl, string gameArchivesListUrl)
        {
            this.gameJsonUrl = gameJsonUrl;
            this.localGamePath = localGamePath;
            this.gameBaseUrl = gameBaseUrl;
            this.gameArchivesUrl = gameArchivesUrl;
            this.gameArchivesListUrl = gameArchivesListUrl;
        }

        public async Task SyncGameAsync()
        {
            await CheckMainFoldersAsync();

            if (!Directory.Exists(localGamePath))
                Directory.CreateDirectory(localGamePath);

            var remoteFiles = await GetRemoteFileListAsync(gameJsonUrl);
            var localFiles = Directory.GetFiles(localGamePath, "*.*", SearchOption.AllDirectories)
                                  .Select(path => path.Replace(localGamePath + Path.DirectorySeparatorChar, "").Replace(Path.DirectorySeparatorChar, '/'))
                                  .ToHashSet();

            var filesToDownload = remoteFiles.Except(localFiles).ToList();

            if (filesToDownload.Any())
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(semaphoreCount);
                var downloadTasks = new List<Task>();

                int processedFilesDownload = 1;


                foreach (var file in filesToDownload)
                {
                    await semaphore.WaitAsync();

                    downloadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            string fileUrl = gameBaseUrl + file;
                            string filePath = Path.Combine(localGamePath, file);

                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                            await DownloadFileAsync(fileUrl, filePath);

                            lock (progressLock)
                            {
                                StatusChanged?.Invoke($"Загрузка файлов {processedFilesDownload} из {filesToDownload.Count}...");
                                ProgressChanged?.Invoke(processedFilesDownload * 100 / filesToDownload.Count);
                                processedFilesDownload++;
                            }
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));

                }
            }
        }

        private async Task CheckMainFoldersAsync()
        {
            var requiredArchives = await GetRemoteFileListAsync(gameArchivesListUrl);

            foreach (var archive in requiredArchives)
            {
                string folderName = Path.GetFileNameWithoutExtension(archive);
                string localFolderPath = Path.Combine(localGamePath, folderName);

                if (!Directory.Exists(localFolderPath))
                {
                    StatusChanged?.Invoke($"Папка {folderName} отсутствует. Загрузка...");

                    string tempArchivePath = Path.Combine(Path.GetTempPath(), archive);
                    string archiveUrl = Path.Combine(gameArchivesUrl, archive);

                    await DownloadFileAsync(archiveUrl, tempArchivePath);
                    ExtractTarArchive(tempArchivePath, localGamePath);

                    File.Delete(tempArchivePath);

                    StatusChanged?.Invoke($"Папка {folderName} успешно загружена");
                }
            }
        }

        private void ExtractTarArchive(string archivePath, string extractPath)
        {
            using (var archive = ArchiveFactory.Open(archivePath))
                foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    entry.WriteToDirectory(extractPath, new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
        }

        private async Task<List<string>> GetRemoteFileListAsync(string url)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var json = await client.GetStringAsync(url);
                    return JsonConvert.DeserializeObject<List<string>>(json);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show($"Не удалось загрузить список файлов: {ex.Message}\nПопробуйте ещё раз...", MessageBoxButton.OK);
                    return new List<string>();
                }
            }
        }
        private async Task DownloadFileAsync(string url, string localPath)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();
                    using (var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await response.Content.CopyToAsync(fileStream);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke($"Ошибка при загрузке файла {url}: {ex.Message}");
                throw;
            }
        }
    }
}

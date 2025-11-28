using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FLaun.Scripts
{
    public class ModSynchronizer
    {
        public string localModsPath { get; set; }
        public string modsJsonUrl { get; set; }
        public string modsBaseUrl { get; set; }

        public event Action<int> ProgressChanged;
        public event Action<string> StatusChanged;

        private readonly object progressLock = new object();

        public ModSynchronizer(string localModsPath, string modsJsonUrl, string modsBaseUrl)
        {
            this.localModsPath = localModsPath;
            this.modsJsonUrl = modsJsonUrl;
            this.modsBaseUrl = modsBaseUrl;
        }


        public async Task SyncModsAsync()
        {
            if (!Directory.Exists(localModsPath))
            {
                StatusChanged?.Invoke($"Локальная папка mods не существует. Создаю папку");
                Directory.CreateDirectory(localModsPath);
            }

            var remoteMods = await GetRemoteFileListAsync(modsJsonUrl);
            var localMods = Directory.GetFiles(localModsPath, "*.jar").Select(Path.GetFileName).ToHashSet();

            var modsToDownload = remoteMods.Where(remoteMod => !localMods.Contains(Path.GetFileName(remoteMod))).ToList();
            var modsToDelete = localMods.Where(localMod => !remoteMods.Contains(localMod)).ToList();

            if (modsToDownload.Any())
            {
                int processedModsDownload = 1;
                foreach (var mod in modsToDownload)
                {
                    string modUrl = modsBaseUrl + mod;
                    string modPath = Path.Combine(localModsPath, mod);

                    await DownloadFileAsync(modUrl, modPath);

                    lock (progressLock)
                    {
                        StatusChanged?.Invoke($"Загрузка файлов {processedModsDownload} из {modsToDownload.Count}...");
                        ProgressChanged?.Invoke(processedModsDownload * 100 / modsToDownload.Count);
                        processedModsDownload++;
                    }
                }
            }

            if (modsToDelete.Any())
            {
                int processedModsDelete = 0;
                foreach (var mod in modsToDelete)
                {
                    string modPath = Path.Combine(localModsPath, mod);

                    File.Delete(modPath);
                    processedModsDelete++;
                    StatusChanged?.Invoke($"Удалён файл {processedModsDelete} из {modsToDelete.Count}...");
                    ProgressChanged?.Invoke(processedModsDelete * 100 / modsToDelete.Count);
                }
            }

            //if (modsToDownload.Count != 0 || modsToDelete.Count != 0)
            //StatusChanged?.Invoke("Синхронизация завершена.");
        }

        private async Task<HashSet<string>> GetRemoteFileListAsync(string url)
        {
            using (var client = new HttpClient())
            {
                try
                {
                    var json = await client.GetStringAsync(url);
                    return new HashSet<string>(JsonConvert.DeserializeObject<IEnumerable<string>>(json));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось загрузить список файлов: {ex.Message}", "Ошибка загрузки данных", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new HashSet<string>();
                }
            }
        }

        private static async Task DownloadFileAsync(string url, string outputPath)
        {
            using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
            {
                try
                {
                    using (var response = await client.GetAsync(url))
                    {
                        response.EnsureSuccessStatusCode();
                        using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                            await response.Content.CopyToAsync(fileStream);
                    }
                }
                catch (HttpRequestException e) { MessageBox.Show($"Ошибка скачивания {url}: {e.Message}"); }
                catch (IOException e) { MessageBox.Show($"Ошибка сохранения {outputPath}: {e.Message}"); }
                catch (TaskCanceledException e)
                { MessageBox.Show($"Таймаут загрузки для {url}: {e.Message}"); }
            }
        }
    }
}

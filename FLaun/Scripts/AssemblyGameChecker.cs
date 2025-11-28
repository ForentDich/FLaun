using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FLaun.Scripts
{
    public class AssemblyGameChecker
    {
        private const string assemblyVersionUrl = "https://genesis-cs.space/FL/launcher/assembly_version.php?type=assembly";
        private const string gameVersionUrl = "https://genesis-cs.space/FL/launcher/assembly_version.php?type=game";

        public static async Task<bool> PerformInitialChecksAsync(AppSettings settings)
        {
            string remoteGameVersion = await FetchRemoteVersionAsync(gameVersionUrl);

            if (remoteGameVersion != null)
            {
                settings.CurrentVersionOfAssembly = remoteGameVersion;
                SettingsManager.SaveSettings();
            }
            else
            {
                Console.WriteLine("Ошибка при получении версии игры.");
                return false;
            }

            string remoteVersion = await FetchRemoteVersionAsync(assemblyVersionUrl);
            string localVersion = settings.AssemblyMods;

            if (remoteVersion == null)
            {
                Console.WriteLine("NULL in AssemblyGameChecker[remoteVersion]");
                return false;
            }

            if (localVersion != remoteVersion)
            {         
                await ClearOldBuildAsync(settings.PathForGame);
                settings.AssemblyMods = remoteVersion; 
                SettingsManager.SaveSettings();
                return true;
            }

            return false;
        }

        private static async Task<string> FetchRemoteVersionAsync(string url)
        {
            using (var httpClient = new HttpClient())
            {
                try
                {
                    string version = await httpClient.GetStringAsync(url);
                    return version.Trim();
                }
                catch (Exception ex)
                {                 
                    Console.WriteLine($"Ошибка при получении версии: {ex.Message}");
                    return null;
                }
            }
        }

        private static async Task ClearOldBuildAsync(string pathForGame)
        {
            string[] directoriesToDelete = { "assets", "libraries", "versions", "runtime", "mods" };

            foreach (var dir in directoriesToDelete)
            {
                string fullPath = Path.Combine(pathForGame, dir);
                if (Directory.Exists(fullPath))
                {
                    try
                    {
                        Directory.Delete(fullPath, true);
                        Console.WriteLine($"Удалена директория: {fullPath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при удалении директории {fullPath}: {ex.Message}");
                    }
                }
            }

            await Task.CompletedTask;
        }
    }
}

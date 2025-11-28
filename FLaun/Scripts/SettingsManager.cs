using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace FLaun.Scripts
{
    public static class SettingsManager
    {
        private static AppSettings appSettings = new AppSettings();
        private const string SettingsFileName = "appsettings.json";

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            if (File.Exists(SettingsFileName))
            {
                var json = File.ReadAllText(SettingsFileName);
                var existingData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                appSettings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

                EnsureAllFieldsPresent(existingData);
            }
            else
            {
                InitializeSettings();
            }
        }

        public static void SaveSettings()
        {
            var json = JsonSerializer.Serialize(appSettings, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            File.WriteAllText(SettingsFileName, json);
        }

        public static void InitializeSettings()
        {
            string defaultGamePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".flaun", "game");

            appSettings.DefaultPathForGame = defaultGamePath;
            appSettings.PathForGame = string.IsNullOrEmpty(appSettings.PathForGame) ? defaultGamePath : appSettings.PathForGame;
            appSettings.RamSize = MemoryInfo.RecommendedRamSize;
            appSettings.IsFirstRun = true;
            appSettings.RamInSystem = MemoryInfo.TotalRamInSystem;
            appSettings.OfflineNickname = "Player";
            appSettings.AssemblyMods = "ASSEMBLY_00000000";
            appSettings.CurrentVersionOfAssembly = "1.6.4";

            SaveSettings();
        }

        public static AppSettings GetSettings()
        {
            return appSettings;
        }

        public static void UpdateSettings(Action<AppSettings> updateAction)
        {
            updateAction(appSettings);
            SaveSettings();
        }

        private static void EnsureAllFieldsPresent(Dictionary<string, JsonElement> existingData)
        {
            int saveRequired = 0;

            if (!existingData.ContainsKey(nameof(appSettings.PathForGame)))
            {
                appSettings.PathForGame = string.Empty;
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.DefaultPathForGame)))
            {
                appSettings.DefaultPathForGame = string.Empty;
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.OfflineNickname)))
            {
                appSettings.OfflineNickname = "Player";
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.AssemblyMods)))
            {
                appSettings.AssemblyMods = "ASSEMBLY_00000000";
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.CurrentVersionOfAssembly)))
            {
                appSettings.CurrentVersionOfAssembly = "1.6.4";
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.RamSize)) || appSettings.RamSize == 0)
            {
                appSettings.RamSize = MemoryInfo.RecommendedRamSize;
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.RamInSystem)) || appSettings.RamInSystem == 0)
            {
                appSettings.RamInSystem = MemoryInfo.TotalRamInSystem;
                saveRequired++;
            }
            if (!existingData.ContainsKey(nameof(appSettings.IsFirstRun)))
            {
                appSettings.IsFirstRun = true;
                saveRequired++;
            }
           
            if (saveRequired > 0)
            {
                SaveSettings();
            }
        }
    }
}

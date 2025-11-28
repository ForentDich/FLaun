using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FLaun.Scripts
{
    public static class LauncherProfileHandler
    {
        public static void CreateLauncherProfilesJson(string filePath)
        {
            var launcherProfile = new LauncherProfile
            {
                ClientToken = Guid.NewGuid().ToString(),
                Profiles = new Profiles()
            };

            string jsonString = JsonSerializer.Serialize(launcherProfile, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, jsonString);
        }

        public static void VerifyAndFixLauncherProfilesJson(string filePath)
        {
            string jsonString = File.ReadAllText(filePath);
            LauncherProfile launcherProfile = JsonSerializer.Deserialize<LauncherProfile>(jsonString);

            if (string.IsNullOrEmpty(launcherProfile.ClientToken))
            {
                Console.WriteLine("ClientToken is missing. Generating a new one.");
                launcherProfile.ClientToken = Guid.NewGuid().ToString();
                jsonString = JsonSerializer.Serialize(launcherProfile, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, jsonString);
            }
        }
    }

    public class LauncherProfile
    {
        [JsonPropertyName("clientToken")]
        public string ClientToken { get; set; }

        [JsonPropertyName("profiles")]
        public Profiles Profiles { get; set; }
    }

    public class Profiles
    {
        // Add properties for profiles if needed
    }
}

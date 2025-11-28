using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FLaun.Scripts
{
    internal class ConfigurationUrlManager
    {
        private static readonly string configUrl = "https://genesis-cs.space/FL/config.json";

        public string ModsListUrl { get; private set; }
        public string GameListUrl { get; private set; }
        public string ModsBaseUrl { get; private set; }
        public string GameArchivesUrl { get; private set; }
        public string GameBaseUrl { get; private set; }
        public string GameArchivesListUrl { get; private set; }

        public async Task LoadConfigurationAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = await client.GetAsync(configUrl);
                    response.EnsureSuccessStatusCode();
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(responseBody);

                    ModsListUrl = json["ModsListUrl"].ToString();
                    GameListUrl = json["GameListUrl"].ToString();
                    ModsBaseUrl = json["ModsBaseUrl"].ToString();
                    GameArchivesUrl = json["GameArchivesUrl"].ToString();
                    GameBaseUrl = json["GameBaseUrl"].ToString();
                    GameArchivesListUrl = json["GameArchivesListUrl"].ToString();
                }
                catch (HttpRequestException e)
                {
                    MessageBox.Show($"Ошибка сети при загрузке конфигурации: {e.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (JsonException e)
                {
                    MessageBox.Show($"Ошибка при парсинге конфигурации: {e.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

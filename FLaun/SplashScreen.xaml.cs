using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace FLaun
{
    /// <summary>
    /// Логика взаимодействия для SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen : Window
    {
        private readonly string launcherCurVer = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        private readonly string launcherExeName = AppDomain.CurrentDomain.FriendlyName;
        private readonly string launcherExePath = Process.GetCurrentProcess().MainModule.FileName;

        public SplashScreen()
        {
            InitializeComponent();
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Text = "Проверка обновлений...";
            bool updateSuccess = await PerformInitialChecksAsync();
            if (updateSuccess)
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            else
            {
                StatusTextBlock.Text = "Не удалось обновить лаунчер. Пожалуйста, попробуйте еще раз позже.";
                await Task.Delay(3000);
                Application.Current.Shutdown();
            }
        }

        private async Task<bool> PerformInitialChecksAsync()
        {
            try
            {
                await CheckLauncherVersionAsync();
                return true;
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                return false;
            }
        }

        private async Task CheckLauncherVersionAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                string versionUrl = "https://genesis-cs.space/FL/launcher/launcher_version.php";
                string latestVersion = await client.GetStringAsync(versionUrl);

                if (launcherCurVer != latestVersion)
                {
                    StatusTextBlock.Text = "Обновление лаунчера...";
                    await DownloadNewLauncherAsync();
                    StatusTextBlock.Text = "Обновление завершено. Перезапуск...";
                    await Task.Delay(2000);
                }
                else
                {
                    StatusTextBlock.Text = "Ваш лаунчер актуален.";
                }
            }
        }

        private async Task DownloadNewLauncherAsync()
        {
            string directory = Path.GetDirectoryName(launcherExePath);
            string newLauncherPath = Path.Combine(directory, "new.exe");
            string updatedLauncherPath = Path.Combine(directory, launcherExeName);

            using (HttpClient client = new HttpClient())
            {
                using (var stream = await client.GetStreamAsync("https://genesis-cs.space/FL/launcher/FLaun.exe"))
                using (var file = new FileStream(newLauncherPath, FileMode.Create))
                {
                    await stream.CopyToAsync(file);
                }
            }

            if (!File.Exists(newLauncherPath))
            {
                StatusTextBlock.Text = $"Файл не найден: {newLauncherPath}";
                return;
            }

            try
            {
                string batchFilePath = Path.Combine(Path.GetDirectoryName(launcherExePath), "update_launcher.bat");

                string batchFileContent = $@"
@echo off
timeout /t 2 /nobreak > NUL
del ""{launcherExePath}""
rename ""{newLauncherPath}"" ""{launcherExeName}.exe""
start """" ""{updatedLauncherPath}""
del ""%~f0""
";

                File.WriteAllText(batchFilePath, batchFileContent);

                // Запускаем командный файл
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{batchFilePath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });

                // Закрываем приложение
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка при обновлении лаунчера: {ex.Message}";
            }
        }
    }
}

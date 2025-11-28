using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Runtime.InteropServices;
using System.IO;
using Ookii.Dialogs.Wpf;
using FLaun.Validators;
using System.Net.Http;
using CmlLib.Core;
using CmlLib.Core.Auth;
using System.Diagnostics;
using CmlLib.Core.ProcessBuilder;
using FLaun.Scripts;
using System.Reflection;
using System.Linq;
using CmlLib.Core.Version;

namespace FLaun
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        

        private ModSynchronizer modSynchronizer;
        private GameSynchronizer gameSynchronizer;
        private ConfigurationUrlManager configManager;

        private bool isNewVersion = false;
        private readonly string folderPath = Path.Combine(Environment.GetEnvironmentVariable("appdata"), ".flaun", "game");
        private readonly ManualResetEventSlim gameExitedEvent = new ManualResetEventSlim(false);


        public MainWindow()
        {
            InitializeComponent();
            InitializeAsync();
        }
            
        private async void InitializeAsync()
        {
            if (!InternetChecker.OK())
            {
                CustomMessageBox.CustomMessageBoxResult result = CustomMessageBox.Show("Пожалуйста, проверьте интернет соединение", MessageBoxButton.OK);
                if (result == CustomMessageBox.CustomMessageBoxResult.OK || result == CustomMessageBox.CustomMessageBoxResult.None)
                    Application.Current.Shutdown();
            }

            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            SettingsManager.LoadSettings();
            var settings = SettingsManager.GetSettings();
            CheckFirstRun();

            configManager = new ConfigurationUrlManager();
            await configManager.LoadConfigurationAsync();

            var ModsLocalDirectory = System.IO.Path.Combine(settings.PathForGame, "mods");
            var GameLocalDirectory = settings.PathForGame;

            gameSynchronizer = new GameSynchronizer(GameLocalDirectory, configManager.GameListUrl, configManager.GameBaseUrl, configManager.GameArchivesUrl, configManager.GameArchivesListUrl);
            gameSynchronizer.ProgressChanged += OnSyncProgressChanged;
            gameSynchronizer.StatusChanged += OnSyncStatusChanged;

            modSynchronizer = new ModSynchronizer(ModsLocalDirectory, configManager.ModsListUrl, configManager.ModsBaseUrl);
            modSynchronizer.ProgressChanged += OnSyncProgressChanged;
            modSynchronizer.StatusChanged += OnSyncStatusChanged;

            videoBackground.Source = new Uri("./Resources/Background/background.mp4", UriKind.Relative);
            videoBackground.Play();

            LaunchButton.IsEnabled = false;

            bool wasCleaned = await AssemblyGameChecker.PerformInitialChecksAsync(settings);

            if (wasCleaned)
                CustomMessageBox.Show("Ваша версия игровой сборки не актуальна... Произведена очистка некоторых файлов.", MessageBoxButton.OK);


            LaunchButton.IsEnabled = true;

            DataContext = new
            {
                PathValidator = new PathValidator(),
                UsernameValidator = new UsernameValidator()
            };

            string launcherProfilePath = System.IO.Path.Combine(folderPath, "launcher_profiles.json");
            if (!System.IO.File.Exists(launcherProfilePath))
                LauncherProfileHandler.CreateLauncherProfilesJson(launcherProfilePath);
            else
                LauncherProfileHandler.VerifyAndFixLauncherProfilesJson(launcherProfilePath);

            
        }

        private void MainWindow_Load(object sender, RoutedEventArgs e)
        {
            MainMenuNickChange();    
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
        }

       
        private void CheckFirstRun()
        {
            var settings = SettingsManager.GetSettings();

            if (!settings.IsFirstRun)
                return;

            settings.PathForGame = folderPath;
            settings.DefaultPathForGame = folderPath;
            settings.RamSize = MemoryInfo.RecommendedRamSize;
            settings.RamInSystem = MemoryInfo.TotalRamInSystem;
            settings.OfflineNickname = "Player";


            settings.IsFirstRun = false;
            SettingsManager.SaveSettings();

        }

        private void videoBackground_MediaEnded(object sender, RoutedEventArgs e)
        {
            videoBackground.Position = TimeSpan.Zero;
            videoBackground.Play();
        }

        private async void On_LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.GetSettings();

            LaunchButton.IsEnabled = false;
            SettingsButton.IsEnabled = false;
            ProfileButton.IsEnabled = false;

            Directory.CreateDirectory(settings.PathForGame);

            LaunchButton.Content = "Синхронизация";
            SyncProgressBar.Value = 0;

            bool syncSuccess = true;

            try 
            {
                await gameSynchronizer.SyncGameAsync();
                OnSyncStatusChanged("");
                OnSyncProgressChanged(0);

                await modSynchronizer.SyncModsAsync();
                OnSyncStatusChanged("");
                OnSyncProgressChanged(0);

                DeleteFoldersExcept(Path.Combine(settings.PathForGame, "libraries\\com\\mojang\\authlib\\"), "4.0.42");
            }
            catch (Exception ex) 
            {
                syncSuccess = false;
                CustomMessageBox.Show($"Ошибка: {ex.Message}\n Попробуйте ещё раз начать игру...", MessageBoxButton.OK);
                LaunchButton.IsEnabled = true;
                SettingsButton.IsEnabled = true;
                ProfileButton.IsEnabled = true;
                LaunchButton.Content = "Начать игру";
            }

            if (syncSuccess)
            {
                LaunchButton.Content = "Запуск";
                gameExitedEvent.Reset();
                LaunchGame();
            }          
        }


        private void On_SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.GetSettings();

            MainMenuBorder.Visibility = Visibility.Collapsed;

            RamSlider.Maximum = settings.RamInSystem;
            RamTextBox.Text = (settings.RamSize).ToString();
            PathTextBox.Text = settings.PathForGame;
            UpdateSettingsButton();

            SettingsMenuBorder.Visibility = Visibility.Visible;
        }

        private void On_ProfileButton_Click(object sender, RoutedEventArgs e)
        {

            var settings = SettingsManager.GetSettings();
            MainMenuBorder.Visibility = Visibility.Collapsed; 
            
            OfflineNicknameTextBox.Text = settings.OfflineNickname;
            UpdateProfileButton();

            ProfileMenuBorder.Visibility = Visibility.Visible;
        }

        private void On_BackToMainMenuFromSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsMenuBorder.Visibility = Visibility.Collapsed;
            MainMenuBorder.Visibility = Visibility.Visible;           
        }

        private void On_BackToMainMenuFromProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileMenuBorder.Visibility = Visibility.Collapsed;
            MainMenuBorder.Visibility = Visibility.Visible;
        }

        private void On_SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.GetSettings();

            settings.RamSize = int.Parse(RamTextBox.Text);
            settings.PathForGame = PathTextBox.Text;

            SettingsManager.SaveSettings();

            SettingsMenuBorder.Visibility = Visibility.Collapsed;
            MainMenuBorder.Visibility = Visibility.Visible;
        }

        private void On_FileDialogButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.GetSettings();

            var dialog = new VistaFolderBrowserDialog
            {
                SelectedPath = settings.PathForGame
            };

            if (dialog.ShowDialog() == true)
            {
                PathTextBox.Text = dialog.SelectedPath;
            }

        }

        private void On_ResetSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CustomMessageBox.CustomMessageBoxResult result = CustomMessageBox.Show("Восстановить настройки по умолчанию?", MessageBoxButton.OKCancel);
            if (result == CustomMessageBox.CustomMessageBoxResult.OK)
            {
                var settings = SettingsManager.GetSettings();
                Directory.CreateDirectory(folderPath);

                settings.PathForGame = folderPath;
                settings.RamSize = MemoryInfo.RecommendedRamSize;
                SettingsManager.SaveSettings();

                PathTextBox.Text = folderPath;
                RamTextBox.Text = (settings.RamSize).ToString();
            }
        }

        private void PathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateSettingsButton();
        }

        private void UpdateSettingsButton() => SaveSettingsButton.IsEnabled = !Validation.GetHasError(PathTextBox);

        private void On_SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.GetSettings();

            var selectedTab = ProfileTabControl.SelectedItem as TabItem;
            settings.OfflineNickname = OfflineNicknameTextBox.Text;
            SettingsManager.SaveSettings();

            ProfileMenuBorder.Visibility = Visibility.Collapsed;
            MainMenuNickChange();
            MainMenuBorder.Visibility = Visibility.Visible;
        }

        private void ProfileTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProfileButton();
        }

        private void UpdateProfileButton()
        {
            var selectedTab = ProfileTabControl.SelectedItem as TabItem;
            if (selectedTab == OfflineTab)
                SaveProfileButton.IsEnabled = !Validation.GetHasError(OfflineNicknameTextBox);
            
        }

        private void MainMenuNickChange()
        {
            var settings = SettingsManager.GetSettings();
            MainMenuNickBlock.Text = settings.OfflineNickname;
        }

        private void OnSyncProgressChanged(int progress)
        {
            Dispatcher.Invoke(() => {
                UpdateProgressBar(progress);
            });
        }

        private void OnSyncStatusChanged(string status)
        {
            Dispatcher.Invoke(() => {
                SyncStatusLabel.Content = status;
            });
        }

        private void UpdateProgressBar(double value)
        {
            DoubleAnimation animation = new DoubleAnimation
            {
                To = value,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            SyncProgressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        static void DeleteFoldersExcept(string path, string excludeFolder)
        {
            foreach (string directory in Directory.GetDirectories(path))
            {
                if (new DirectoryInfo(directory).Name != excludeFolder)
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private async void LaunchGame()
        {
            var settings = SettingsManager.GetSettings();
            
            var path = new MinecraftPath(settings.PathForGame);
            var launcher = new MinecraftLauncher(path);

            var version = (await launcher.GetVersionAsync(settings.CurrentVersionOfAssembly)).ToMutableVersion();
            version.LibraryList.Add(new MLibrary("com.mojang:authlib:4.0.42")
            {
                Artifact = new CmlLib.Core.Files.MFileMetadata
                {
                    Path = "com/mojang/authlib/4.0.42/authlib-4.0.42.jar",
                    Sha1 = "", 
                    Url = "" 
                }
            });

            //await launcher.InstallAsync(version);
            var gameProcess = launcher.BuildProcess(version, new MLaunchOption
            {
                Session = MSession.CreateOfflineSession(settings.OfflineNickname),
                MaximumRamMb = settings.RamSize,

            });

            gameProcess.Start();
            gameProcess.EnableRaisingEvents = true;
            gameProcess.Exited += GameProcess_Exited;

            videoBackground.Stop();
            Hide();
            ShowInTaskbar = false;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                gameExitedEvent.Wait();
                Dispatcher.Invoke(() =>
                {
                    OnSyncStatusChanged("");
                    OnSyncProgressChanged(0);
                    Show();
                    videoBackground.Play();
                    WindowState = WindowState.Normal;
                    ShowInTaskbar = true;
                    LaunchButton.IsEnabled = true;
                    SettingsButton.IsEnabled = true;
                    ProfileButton.IsEnabled = true;

                    LaunchButton.Content = "Начать игру";
                });
            });
        }

        private void GameProcess_Exited(object sender, EventArgs e)
        {
            gameExitedEvent.Set();
        }

        async Task<string> HttpResponse(string line)
        {
            using(var net =  new HttpClient())
            {
                var response = await net.GetAsync(line);
                return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : null;
            }
        }


    }

}

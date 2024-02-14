using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using Newtonsoft.Json;

namespace Launcher2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private int version;
        private int localVersion;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void CheckUpdate()
        {
            // Get the version by fetching version.txt from https://gf.deneria.net/version.txt
            var version = new HttpClient().GetAsync("https://gf.deneria.net/version.txt").Result.Content.ReadAsStringAsync().Result;
            // Get the local version from the file version.txt
            var localVersion = "0";
            try
            {
                localVersion = System.IO.File.ReadAllText("version.txt");
            } catch (System.IO.FileNotFoundException)
            {
                System.IO.File.WriteAllText("version.txt", localVersion);
            }

            this.localVersion = Convert.ToInt32(localVersion);
            this.version = Convert.ToInt32(version);
        }
    
        private class UpdateInfo
        {
            public string[] files { get; set; }
        }

        private UpdateInfo DownloadUpdateInfo(int n)
        {
            var json = new HttpClient().GetAsync("https://gf.deneria.net/updates/" + n + ".json").Result.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<UpdateInfo>(json);
        }

        private bool DownloadFile(string file)
        {
            var data = new HttpClient().GetAsync("https://gf.deneria.net/game/" + file).Result.Content.ReadAsByteArrayAsync().Result;
            try
            {
                System.IO.File.WriteAllBytes(file, data);
            } catch (System.IO.DirectoryNotFoundException)
            {
                Label.Content = "Error: Directory not found!";
                return false;
            }

            return true;
        }
    
        private async Task Update(int version, int localVersion)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate { ProgressBar.Value = 0; }));
            var changedFiles = new List<string>();
            for (var i = localVersion + 1; i <= version; i++)
            {
                var updateInfo = DownloadUpdateInfo(i);
                if (updateInfo.files.Length <= 0)
                    continue;
                changedFiles.AddRange(updateInfo.files);
            }
            // Remove duplicates from the list
            changedFiles = changedFiles.Distinct().ToList();
            var j = 0;
            foreach (var file in changedFiles)
            {
                j++;
                if (!DownloadFile(file)) return;
                Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
                {
                    ProgressBar.Value = ((j + 1) / changedFiles.Count) * 100;
                }));
            }
            System.IO.File.WriteAllText("version.txt", version.ToString());
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                ProgressBar.Value = 100;
                Label.Content = "Ready to play!";
                Start.IsEnabled = true;
                Scan.IsEnabled = false;
            }));
        }
    
        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var start = new ProcessStartInfo
                {
                    FileName = "GrandFantasia.exe",
                    Arguments = "EasyFun"
                };
                Process.Start(start);
            } catch (System.ComponentModel.Win32Exception)
            {
                MessageBox.Show("Grand Fantasia not found! Please install the game first.");
            }
        }

        private void Scan_OnClick(object sender, RoutedEventArgs e)
        {
            Start.IsEnabled = false;
            Scan.IsEnabled = false;
            Label.Content = "Updating...";
            Update(version, localVersion);
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Not implemented yet!");
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            CheckUpdate();
            if (version != localVersion)
            {
                Scan.IsEnabled = true;
                Label.Content = "Update available!";
            } else
            {
                Start.IsEnabled = true;
                Scan.IsEnabled = false;
                ProgressBar.Value = 100;
                Label.Content = "Ready to play!";
            }
        }
    }
}
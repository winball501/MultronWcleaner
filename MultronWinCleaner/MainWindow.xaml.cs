using Hardcodet.Wpf.TaskbarNotification;
using MFK;
using Microsoft.VisualBasic.Logging;
using MultronWinCleaner;
using MultronWinCleaner.Processes;
using Ookii.Dialogs.Wpf;
using System;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using WindowsInput.Events.Sources;
using static Multron_Win_Cleaner.MainWindow;
using static MultronWinCleaner.Processes.Clean;
using static MultronWinCleaner.Processes.Scan;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;


namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {
        private string diskModel = string.Empty;
        public Settings settings;
        public MultronWinCleaner.Processes.Scan.MainViewModel viewModel = new MultronWinCleaner.Processes.Scan.MainViewModel();
        public MultronWinCleaner.Processes.Scan.MainViewModel viewModelbac = new MultronWinCleaner.Processes.Scan.MainViewModel();
        public List<string> logfiles = new List<string>();
        public SolidColorBrush brush;
        public List<CheckBox> checkboxes2 = new List<CheckBox>();
        public List<StackPanel> stackpanels = new List<StackPanel>();
        public List<Expander> expanders = new List<Expander>();
        public List<ListBox> listboxes = new List<ListBox>();
        public List<ComboBox> comboboxlist = new List<ComboBox>();
        public Dictionary<string, bool> databaseDefaults = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public List<string> paths = new List<string>();
        public CancellationTokenSource dismcancel = new CancellationTokenSource();
        public Utilities utilities;
        public byte autoclean = 0;
        public byte startupclean = 0;
        public byte startupscan = 0;
        public byte onclean = 0;
        public byte killer = 0;
        public readonly TrayIconAnimator _trayAnimator ;
        public string extensions = ".log.etl.dmp.trace.tmp.temp.bak.swp";
        string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
        private DispatcherTimer diskSpaceTimer;
        public MainWindow()
        {
       

            InitializeComponent();
            _trayAnimator = new TrayIconAnimator(TrayIcon, "pack://application:,,,/MultronWinCleaner;component/Assets/mwc_icon.ico", "pack://application:,,,/MultronWinCleaner;component/Assets/TrayFrames/frame{0:00}.ico", frameCount: 12);

            if (!System.IO.File.Exists(settingsPath))
            {
                string defaultSettings = "minutes:0" + Environment.NewLine +
                                         "autoclean:0" + Environment.NewLine +
                                         "trayicon:0" + Environment.NewLine +
                                         "themes:0";
                System.IO.File.WriteAllText(settingsPath, defaultSettings);
            }

            string fileContent = System.IO.File.ReadAllText(settingsPath);

            string themePath = fileContent.Contains("themes:1") ? "Themes/Dark.xaml" : "Themes/Light.xaml";
            ToggleThemeSwitch.IsChecked = fileContent.Contains("themes:1");

            themeselector.selector(new Uri(themePath, UriKind.Relative));

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };
            brush = (SolidColorBrush)resourceDictionary["Text"];
            Window.GetWindow(this)?.DragMove();

            label1_Copy.Text = "Ready for the scan";

       
            string systemDrive = System.IO.Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\";
            _ = Task.Run(() => GetDiskModel(systemDrive)).ContinueWith(t =>
            {
                diskModel = t.Result;
                Dispatcher.BeginInvoke(new Action(UpdateDiskSpace));
            }, TaskScheduler.Default);

            diskSpaceTimer = new DispatcherTimer();
            diskSpaceTimer.Interval = TimeSpan.FromSeconds(5);
            diskSpaceTimer.Tick += DiskSpaceTimer_Tick;
            diskSpaceTimer.Start();
             
            UpdateDiskSpace();
        }

        
        private string GetDiskModel(string driveLetter)
        {
            try
            {
              
                char letter = driveLetter[0];

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                 
                    Arguments = $"-Command \"(Get-Partition -DriveLetter '{letter}' | Get-Disk).FriendlyName\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();

                    return string.IsNullOrWhiteSpace(output) ? "Disk" : output;
                }
            }
            catch
            {
                return "Disk";
            }
        }
        private void DiskSpaceTimer_Tick(object sender, EventArgs e)
        {
            UpdateDiskSpace();
        }
        private void UpdateDiskSpace()
        {
            try
            {
                string driveLetter = System.IO.Path.GetPathRoot(Environment.SystemDirectory);
                DriveInfo drive = new DriveInfo(driveLetter);

                if (drive.IsReady)
                {
                    double freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
                    double totalSizeGB = drive.TotalSize / (1024.0 * 1024 * 1024);

                    string model = string.IsNullOrEmpty(diskModel) ? "Disk" : diskModel;
                    labelDiskSize.Text = $"[{model}] Drive {driveLetter} Free: {freeSpaceGB:F2} GB / Total: {totalSizeGB:F2} GB";
                }
            }
            catch (Exception)
            {
                labelDiskSize.Text = "Disk size could not be read.";
            }
        }
        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        { 
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/winball501",
                UseShellExecute = true
            });
        } 
        private void LoadingOverlay_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (LoadingOverlay.IsVisible)
            {
                LoadingSpinnerRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.8)) { RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever });
                StatusLoad.BeginAnimation(UIElement.OpacityProperty,
                    new System.Windows.Media.Animation.DoubleAnimation(1.0, 0.3, TimeSpan.FromSeconds(0.8)) { AutoReverse = true, RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever });
            }
            else
            {
                LoadingSpinnerRotation.BeginAnimation(System.Windows.Media.RotateTransform.AngleProperty, null);
                StatusLoad.BeginAnimation(UIElement.OpacityProperty, null);
            }
        }

        private void LoadingOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        { 
            if (e.LeftButton == MouseButtonState.Pressed)
            {
         
                this.DragMove();
            }
        }
        public void AnimateIcon(bool isCleaning)
        {
            Dispatcher.Invoke(() =>
            {
                if (isCleaning)
                {
                    _trayAnimator.Start();
                    cleanIcon.StartSpinning();
                }
                else
                {
                    _trayAnimator.Stop();
                    cleanIcon.StopSpinning();
                }
            });
        }

        public const string OfflineModeSettingKey = "offlinemode";
         
        public static bool IsOfflineModeEnabled()
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
                if (!System.IO.File.Exists(path)) return false;
                return System.IO.File.ReadAllLines(path)
                    .Any(line => line.Trim().Equals(OfflineModeSettingKey + ":1", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public const string AutoSaveSelectionsSettingKey = "autosaveselections";
        private bool autoSaveSelections = IsAutoSaveSelectionsEnabled();

        public void SetAutoSaveSelections(bool enabled)
        {
            autoSaveSelections = enabled;
            SaveSettings.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;
        }
        private readonly Dictionary<string, bool?> pendingSelections = new Dictionary<string, bool?>(StringComparer.OrdinalIgnoreCase);
        private readonly object selectionsFileLock = new object();
        private DispatcherTimer selectionsAutoSaveTimer;

        public const string AllBrowserProfilesSettingKey = "allbrowserprofiles";

        public static bool IsAutoSaveSelectionsEnabled() => IsSettingOnByDefault(AutoSaveSelectionsSettingKey);

        public static bool IsCleanAllBrowserProfilesEnabled() => IsSettingOnByDefault(AllBrowserProfilesSettingKey);

        private static bool IsSettingOnByDefault(string key)
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
                if (!System.IO.File.Exists(path)) return true;
                return !System.IO.File.ReadAllLines(path)
                    .Any(line => line.Trim().Equals(key + ":0", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return true;
            }
        }

        public void ReloadDatabaseIfIdle()
        {
            if (ReloadDb.IsEnabled && buttonStartScan.IsEnabled)
                ReloadDatabase_Click(ReloadDb, new RoutedEventArgs());
        }

        private void QueueSelectionAutoSave(CheckBox checkBox, string path)
        {
            if (!autoSaveSelections || path == null || !databaseDefaults.TryGetValue(path, out bool databaseDefault)) return;

            bool isChecked = checkBox.IsChecked == true;
            lock (pendingSelections)
            {
                pendingSelections[path] = isChecked == databaseDefault ? null : isChecked;
            }

            if (selectionsAutoSaveTimer == null)
            {
                selectionsAutoSaveTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher) { Interval = TimeSpan.FromMilliseconds(500) };
                selectionsAutoSaveTimer.Tick += async (s, e) =>
                {
                    selectionsAutoSaveTimer.Stop();
                    try
                    {
                        await Task.Run(FlushPendingSelections);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Auto-save of selections failed: " + ex.Message);
                    }
                };
            }
            selectionsAutoSaveTimer.Stop();
            selectionsAutoSaveTimer.Start();
        }

        private void SaveCommandSelection(string key, bool isChecked)
        {
            lock (pendingSelections)
            {
                pendingSelections[key] = isChecked ? true : null;
            }
            _ = Task.Run(() =>
            {
                try
                {
                    FlushPendingSelections();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Could not save the command selection: " + ex.Message);
                }
            });
        }

        private void FlushPendingSelections()
        {
            Dictionary<string, bool?> changes;
            lock (pendingSelections)
            {
                if (pendingSelections.Count == 0) return;
                changes = new Dictionary<string, bool?>(pendingSelections, StringComparer.OrdinalIgnoreCase);
                pendingSelections.Clear();
            }

            lock (selectionsFileLock)
            {
                Dictionary<string, bool> selections = MultronWinCleaner.Processes.Load.ReadSelections();
                foreach (var change in changes)
                {
                    if (change.Value.HasValue) selections[change.Key] = change.Value.Value;
                    else selections.Remove(change.Key);
                }
                MultronWinCleaner.Processes.Load.WriteSelections(selections);
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit += (s, args) =>
            {
                try { FlushPendingSelections(); } catch { }
            };
            SetAutoSaveSelections(autoSaveSelections);
            utilities = new Utilities(this);
            settings = new Settings(utilities.memcleaner, utilities, this, utilities.startupmanager);
            utilities.Show();
            utilities.Hide();
            settings.Show();
            settings.Hide();
            await Task.Run(() => { var trayIconTask = new OpenTrayIcon(this).run();  });
            if (App.LaunchedFromStartup)
            {
                this.Visibility = Visibility.Hidden;
            }
             
            bool offlineMode = IsOfflineModeEnabled();
            if (offlineMode)
            {
                StatusLoad.Text = "Offline mode, skipping online checks...";
            }
            else if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    this.Visibility = Visibility.Visible;
                    LoadingOverlay.Visibility = Visibility.Visible;

                    const int networkTimeoutSeconds = 10;
                    for (int remaining = networkTimeoutSeconds; remaining > 0 && !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable(); remaining--)
                    {
                        StatusLoad.Text = $"Waiting for internet connection... ({remaining}s)";
                        await Task.Delay(1000);
                    }

                    StatusLoad.Text = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()
                        ? "Connected, loading..."
                        : "No internet connection, continuing offline...";
                }
         
            bool isOnline = !offlineMode && System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            Environment.CurrentDirectory = baseDirectory;



            LoadingOverlay.Visibility = Visibility.Visible;

            if (isOnline)
            {
                MultronWinCleaner.Processes.Updater updater = new MultronWinCleaner.Processes.Updater(this);
                await Task.Run(() => updater.run());

                string updaterfile = System.IO.Path.Combine(baseDirectory, "Update", "mwc", "Updater.exe");
                string updatesfolder = System.IO.Path.Combine(baseDirectory, "Update", "mwc");
                string targetUpdater = System.IO.Path.Combine(baseDirectory, "Updater.exe");

                if (System.IO.File.Exists(updaterfile))
                {
                    System.IO.File.Copy(updaterfile, targetUpdater, overwrite: true);
                }

                if (Directory.Exists(updatesfolder))
                {
                    foreach (string file in Directory.GetFiles(updatesfolder))
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch (Exception)
                        {
                        }
                    }

                    if (Directory.Exists(updatesfolder))
                    {
                        try
                        {
                            Directory.Delete(updatesfolder);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            await this.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);

            await this.Dispatcher.InvokeAsync(() =>
            {
                dataGridGroups.Visibility = Visibility.Hidden;
                Datagridscroll.Visibility = Visibility.Hidden;
            });

            string databasePath = System.IO.Path.Combine(baseDirectory, "database.txt");

            if (System.IO.File.Exists(databasePath))
            {
                await this.Dispatcher.InvokeAsync(() =>
                {
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                });
                await this.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                var load = new MultronWinCleaner.Processes.Load(this);
                await Task.Run(() => load.RunAsync());

                this.previousWidth = this.Width;
                this.previousHeight = this.Height;
                this.previousLeft = this.Left;
                this.previousTop = this.Top;
            }
            else if (offlineMode)
            { 
                label1_Copy.Text = "You are running in offline mode and the database could not be found.";
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("The database.txt file could not be found. This may be due to an internet connectivity issue, as the program attempts to download the latest database.txt file from GitHub but was unable to do so.\n\nWould you like to be redirected to the GitHub page to manually download the latest database.txt file?", "Multron Windows Cleaner", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/winball501/MultronWcleaner-Database",
                        UseShellExecute = true
                    });
                }
            }
       
       
            TrayIcon.TrayMouseDoubleClick += TrayIcon_MouseDoubleClick;

            LoadingOverlay.Visibility = Visibility.Collapsed;

            autoCleaner = new AutoClean(this);
            _ = autoCleaner.RunAsync();
            await startup();

         


        }
        public class OpenTrayIcon
        {
            MainWindow main;
            public OpenTrayIcon(MainWindow mainWindow) { 
                main = mainWindow;
            }
            public async Task run()
            {
                while(true)
                {
                    await main.Dispatcher.InvokeAsync(() => {

                        if (main.settings.chkTrayIcon.IsChecked == true && main.TrayIcon.Visibility != Visibility.Visible)
                        {

                            main.TrayIcon.Visibility = Visibility.Visible;
                        }
                        else if (main.settings.chkTrayIcon.IsChecked == false && main.TrayIcon.Visibility == Visibility.Visible)
                        {
                            main.TrayIcon.Visibility = Visibility.Hidden;
                        }

                    });
                  
                    await Task.Delay(1250);  
                }
            }
        }


        public async Task loadothers2()
        {
            CheckBox malscan = new CheckBox
            {
                Content = "Malware Scan=Scans and cleans your system from malicious software=malscan",
                Margin = new Thickness(10),
                IsChecked = false,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = brush,
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkboxes2.Add(malscan);


            ComboBox scanOptionsComboBox = new ComboBox
            {
                Margin = new Thickness(10, 0, 10, 10),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Colors.Black),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                IsEnabled = true,
                ItemsSource = new List<string> { "Full Scan", "Quick Scan", "Custom Scan" },
                SelectedIndex = 0
            };


            malscan.Checked += (s, e) => scanOptionsComboBox.IsEnabled = true;
            malscan.Unchecked += (s, e) => scanOptionsComboBox.IsEnabled = false;


            StackPanel groupBoxContent2 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(groupBoxContent2);


            groupBoxContent2.Children.Add(malscan);
            groupBoxContent2.Children.Add(scanOptionsComboBox);


            Expander newExpander1 = new Expander
            {
                Header = "Malware Scan (Coming Soon)",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                IsEnabled = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            newExpander1.Content = groupBoxContent2;
            expanders.Add(newExpander1);
            try
            {
                wrapPanel1.Children.Add(newExpander1);
            }
            catch (Exception)
            {
            }

            AddDuplicateScanOption();
            AddFirewallScanOption();
            AddShortcutScanOption();
            AddSecurityScanOption();
          
        }

        public AutoClean autoCleaner;
        public string AutoCleanStatus = "";

        public void SetAutoCleanStatus(string text)
        {
            AutoCleanStatus = text ?? "";
            labelAutoClean.Text = AutoCleanStatus;
            labelAutoClean.Visibility = AutoCleanStatus.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

            bool scan = settings?.chkEnableStartupScan.IsChecked == true;
            bool clean = settings?.chkEnableStartupClean.IsChecked == true;
            labelStartupClean.Text = clean ? "Startup Clean is on · scans and cleans when Windows starts"
                : scan ? "Startup Scan is on · scans when Windows starts" : "";
            labelStartupClean.Visibility = clean || scan ? Visibility.Visible : Visibility.Collapsed;
        }

        public async Task<bool> StartAutoCleanAsync()
        {
            if (autoclean != 0 || !Equals(buttonStartScan.Content, "Scan") || LockedFilesWindowOverlay.Visibility == Visibility.Visible)
                return false;
            if (!buttonStartScan.IsEnabled)
            {
                if (reset != 1)
                    return false;
                ButtonReset_Click(this, null);
            }
            if (checkboxes2.All(cb => cb.IsChecked != true))
                return false;

            autoclean = 1;
            await startscan();
            return true;
        }

        public const string DuplicateScanSettingKey = "duplicatescanwithmain";
        private CheckBox? duplicateScanCheckBox;
        private bool duplicateReportRunning;

        private void AddDuplicateScanOption()
        {
            CheckBox dupscan = new CheckBox
            {
                Content = "Find duplicate files after the scan (nothing is deleted automatically)",
                Margin = new Thickness(10),
                IsChecked = IsSettingOnByDefault(DuplicateScanSettingKey),
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                ToolTip = "After a scan you start yourself, identical files in Desktop, Documents, Downloads, Pictures, Videos and Music are searched in the background and shown as a warning above the results."
            };
            dupscan.Checked += (s, e) => utilities?.savesettings(DuplicateScanSettingKey + ":1");
            dupscan.Unchecked += (s, e) => utilities?.savesettings(DuplicateScanSettingKey + ":0");
            duplicateScanCheckBox = dupscan;
            dupscan.SetResourceReference(Control.ForegroundProperty, "Text");

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(content);
            content.Children.Add(dupscan);

            Expander expander = new Expander
            {
                Header = "Duplicate Files",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = content
            };
            expanders.Add(expander);
            wrapPanel1.Children.Add(expander);
        }

        public const string OkColor = "#16A34A";
        public const string InfoColor = "#0078D4";
        public const string WarningColor = "#E67E22";
        public const string ProblemColor = "#DC3545";

        public static void SetResultPanelColor(Border panel, System.Windows.Shapes.Ellipse icon, string hex)
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
            icon.Fill = new SolidColorBrush(color);
            panel.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x66, color.R, color.G, color.B));
            panel.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x14, color.R, color.G, color.B));
        }

        private (Border Panel, System.Windows.Shapes.Ellipse Icon, TextBlock Status, CheckBox Run) SystemCheckControls(string id) => id switch
        {
            "store" => (ComponentStorePanel, ComponentStoreIcon, ComponentStoreStatus, ComponentStoreRun),
            "health" => (ComponentHealthPanel, ComponentHealthIcon, ComponentHealthStatus, ComponentHealthRun),
            _ => (SystemFilesPanel, SystemFilesIcon, SystemFilesStatus, SystemFilesRun)
        };

        private void SetSystemCheck(string id, string color, string text, bool showRun, bool runChecked)
        {
            var c = SystemCheckControls(id);
            c.Panel.Visibility = Visibility.Visible;
            SetResultPanelColor(c.Panel, c.Icon, color);
            c.Status.Text = text;
            c.Run.Visibility = showRun ? Visibility.Visible : Visibility.Collapsed;
            c.Run.IsChecked = runChecked;
        }

        private void HideSystemChecks()
        {
            foreach (string id in new[] { "store", "health", "sfc" })
            {
                var c = SystemCheckControls(id);
                c.Panel.Visibility = Visibility.Collapsed;
                c.Run.IsChecked = false;
            }
        }

        public void ShowSystemCheckRunning(string id)
        {
            string text = id switch
            {
                "store" => "Analyzing the component store...",
                "health" => "Checking the component store for corruption...",
                _ => "Checking Windows system files (sfc /verifyonly)..."
            };
            SetSystemCheck(id, InfoColor, text, false, false);
        }

        public void ShowComponentStoreResult(SystemChecks.ComponentStoreInfo info)
        {
            if (!info.Parsed)
            {
                SetSystemCheck("store", WarningColor, "Dism.exe did not report the component store size. Details are in dism_scan.log. You can still run the cleanup.", true, false);
                return;
            }

            var details = new List<string>();
            if (info.ActualSize >= 0) details.Add("size " + formatsize(info.ActualSize));
            details.Add($"reclaimable {formatsize(info.Reclaimable)} (backups {formatsize(Math.Max(0, info.Backups))}, cache {formatsize(Math.Max(0, info.Cache))})");
            details.Add($"{info.ReclaimablePackages} reclaimable {(info.ReclaimablePackages == 1 ? "package" : "packages")}");
            if (!string.IsNullOrEmpty(info.LastCleanup)) details.Add("last cleanup " + info.LastCleanup);
            string detailText = string.Join(" · ", details);
            if (info.RestartPending) detailText += " · a restart is pending";

            bool recommended = info.CleanupRecommended == true || info.ReclaimablePackages > 0;
            if (recommended)
                SetSystemCheck("store", WarningColor, "Cleanup is recommended: " + detailText, true, true);
            else
                SetSystemCheck("store", OkColor, "No cleanup needed: " + detailText, true, false);
        }

        public void ShowComponentHealthResult(SystemChecks.HealthState state, string detail)
        {
            switch (state)
            {
                case SystemChecks.HealthState.Healthy:
                    SetSystemCheck("health", OkColor, "No component store corruption was found.", false, false);
                    break;
                case SystemChecks.HealthState.Repairable:
                    SetSystemCheck("health", ProblemColor, "Corruption was found. Restore Health can repair it (an internet connection may be needed).", true, true);
                    break;
                case SystemChecks.HealthState.NotRepairable:
                    SetSystemCheck("health", ProblemColor, "Corruption was found that DISM reports as not repairable. Restore Health will try to download fresh files from Windows Update.", true, true);
                    break;
                default:
                    SetSystemCheck("health", WarningColor, "The result could not be read: " + detail + " (details in dism_health.log)", true, false);
                    break;
            }
        }

        public void ShowSystemFilesResult(SystemChecks.SfcState state, string detail)
        {
            switch (state)
            {
                case SystemChecks.SfcState.Clean:
                    SetSystemCheck("sfc", OkColor, "Windows Resource Protection found no integrity violations.", false, false);
                    break;
                case SystemChecks.SfcState.Unknown:
                    SetSystemCheck("sfc", WarningColor, "The result could not be read: " + detail + " (details in sfc_verify.log)", true, false);
                    break;
                default:
                    SetSystemCheck("sfc", ProblemColor, "Integrity violations were found in Windows system files. sfc /scannow can repair them.", true, true);
                    break;
            }
        }

        public (bool Cleanup, bool Restore, bool Sfc) GetSystemRepairChoices()
        {
            bool Selected(string id)
            {
                var c = SystemCheckControls(id);
                return c.Panel.Visibility == Visibility.Visible && c.Run.Visibility == Visibility.Visible && c.Run.IsChecked == true;
            }
            return (Selected("store"), Selected("health"), Selected("sfc"));
        }

        public const string FirewallScanSettingKey = "firewallscanwithmain";
        private CheckBox? firewallScanCheckBox;
        private bool firewallReportRunning;
        private List<MultronWinCleaner.Processes.FirewallRules.InvalidRule> foundFirewallRules = new List<MultronWinCleaner.Processes.FirewallRules.InvalidRule>();

        private void AddFirewallScanOption()
        {
            CheckBox fwscan = new CheckBox
            {
                Content = "Find broken firewall rules after the scan",
                Margin = new Thickness(10),
                IsChecked = IsSettingOnByDefault(FirewallScanSettingKey),
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                ToolTip = "After a scan, Windows Firewall rules for programs that no longer exist on this PC are listed above the results. They are only removed when you click Remove Rules."
            };
            fwscan.Checked += (s, e) => utilities?.savesettings(FirewallScanSettingKey + ":1");
            fwscan.Unchecked += (s, e) => utilities?.savesettings(FirewallScanSettingKey + ":0");
            firewallScanCheckBox = fwscan;
            fwscan.SetResourceReference(Control.ForegroundProperty, "Text");

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(content);
            content.Children.Add(fwscan);

            Expander expander = new Expander
            {
                Header = "Firewall Rules",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = content
            };
            expanders.Add(expander);
            wrapPanel1.Children.Add(expander);
        }

        public const string ShortcutScanSettingKey = "shortcutscanwithmain";
        private CheckBox? shortcutScanCheckBox;
        private bool shortcutReportRunning;
        private List<MultronWinCleaner.Processes.ShortcutFixer.BrokenShortcut> foundShortcuts = new List<MultronWinCleaner.Processes.ShortcutFixer.BrokenShortcut>();

        private void AddShortcutScanOption()
        {
            CheckBox scscan = new CheckBox
            {
                Content = "Find broken shortcuts after the scan",
                Margin = new Thickness(10),
                IsChecked = IsSettingOnByDefault(ShortcutScanSettingKey),
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                ToolTip = "After a scan, Desktop and Start Menu shortcuts whose program no longer exists are listed above the results. They are only changed when you click Fix Shortcuts."
            };
            scscan.Checked += (s, e) => utilities?.savesettings(ShortcutScanSettingKey + ":1");
            scscan.Unchecked += (s, e) => utilities?.savesettings(ShortcutScanSettingKey + ":0");
            shortcutScanCheckBox = scscan;
            scscan.SetResourceReference(Control.ForegroundProperty, "Text");

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(content);
            content.Children.Add(scscan);

            Expander expander = new Expander
            {
                Header = "Shortcuts",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = content
            };
            expanders.Add(expander);
            wrapPanel1.Children.Add(expander);
        }

        public const string SecurityScanSettingKey = "securityscanwithmain";
        private CheckBox? securityScanCheckBox;
        private bool securityReportRunning;
        private List<MultronWinCleaner.Processes.SecurityCheck.Issue> foundSecurityIssues = new List<MultronWinCleaner.Processes.SecurityCheck.Issue>();

        private void AddSecurityScanOption()
        {
            CheckBox secscan = new CheckBox
            {
                Content = "Check Windows security settings after the scan",
                Margin = new Thickness(10),
                IsChecked = IsSettingOnByDefault(SecurityScanSettingKey),
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                ToolTip = "After a scan, settings that make Windows less secure are listed above the results, for example UAC or Windows Firewall turned off, AutoPlay on for USB drives or Defender turned off by a policy. They are only changed when you click Fix Selected."
            };
            secscan.Checked += (s, e) => utilities?.savesettings(SecurityScanSettingKey + ":1");
            secscan.Unchecked += (s, e) => utilities?.savesettings(SecurityScanSettingKey + ":0");
            securityScanCheckBox = secscan;
            secscan.SetResourceReference(Control.ForegroundProperty, "Text");

            StackPanel content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(content);
            content.Children.Add(secscan);

            Expander expander = new Expander
            {
                Header = "Security",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Content = content
            };
            expanders.Add(expander);
            wrapPanel1.Children.Add(expander);
        }

        public static string DescribeSecurityIssues(List<MultronWinCleaner.Processes.SecurityCheck.Issue> issues, out string color)
        {
            int high = issues.Count(i => i.Severity == MultronWinCleaner.Processes.SecurityCheck.Level.High);
            int medium = issues.Count(i => i.Severity == MultronWinCleaner.Processes.SecurityCheck.Level.Medium);
            int low = issues.Count - high - medium;
            color = high > 0 ? ProblemColor : medium > 0 ? WarningColor : issues.Count > 0 ? InfoColor : OkColor;
            if (issues.Count == 0)
                return "No insecure Windows settings were found.";
            var parts = new List<string>();
            if (high > 0) parts.Add($"{high} high");
            if (medium > 0) parts.Add($"{medium} medium");
            if (low > 0) parts.Add($"{low} low");
            return $"{issues.Count} {(issues.Count == 1 ? "setting makes" : "settings make")} Windows less secure ({string.Join(", ", parts)} risk).";
        }

        private void ShowSecurityIssues(List<MultronWinCleaner.Processes.SecurityCheck.Issue> issues, string suffix)
        {
            foundSecurityIssues = issues;
            SecurityResultsList.ItemsSource = issues;
            string status = DescribeSecurityIssues(issues, out string color);
            SecurityResultsStatus.Text = suffix.Length > 0 ? status + " " + suffix : status;
            SetResultPanelColor(SecurityResultsPanel, SecurityResultsIcon, color);
            bool any = issues.Count > 0;
            SecurityResultsToggle.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
            SecurityFixButton.Visibility = issues.Any(i => i.CanFix) ? Visibility.Visible : Visibility.Collapsed;
            if (!any)
                SecurityResultsList.Visibility = Visibility.Collapsed;
            SecurityResultsToggle.Content = SecurityResultsList.Visibility == Visibility.Visible ? "Hide Issues" : "Show Issues";
        }

        public async Task RunSecurityReportAsync()
        {
            if (securityReportRunning || securityScanCheckBox?.IsChecked != true || cancelstatus.IsCancellationRequested)
                return;

            bool automatic = autoclean == 1 || startupscan == 1;
            securityReportRunning = true;
            SecurityResultsPanel.Visibility = Visibility.Visible;
            SecurityResultsToggle.Visibility = Visibility.Collapsed;
            SecurityFixButton.Visibility = Visibility.Collapsed;
            SecurityResultsList.Visibility = Visibility.Collapsed;
            SecurityResultsStatus.Text = "Checking Windows security settings...";
            SetResultPanelColor(SecurityResultsPanel, SecurityResultsIcon, InfoColor);
            try
            {
                var step = await RunScanStepAsync("Checking Windows security settings",
                    WithFallback(utilities?.RunSecurityReportAsync(), MultronWinCleaner.Processes.SecurityCheck.Scan),
                    SecurityStatusButton, SecurityCancelButton);
                if (step.Stopped)
                {
                    SecurityResultsStatus.Text = "The security check was skipped.";
                    SetResultPanelColor(SecurityResultsPanel, SecurityResultsIcon, WarningColor);
                    return;
                }
                var issues = step.Result;
                ShowSecurityIssues(issues, issues.Count > 0 ? "Nothing was changed." : "");
                int high = issues.Count(i => i.Severity == MultronWinCleaner.Processes.SecurityCheck.Level.High);
                if (automatic && high > 0)
                {
                    new Notify("Security Warning",
                        $"{high} high risk Windows {(high == 1 ? "setting" : "settings")} found. Click to review.",
                        issues.First().Title,
                        () => utilities?.ShowSecurityCheck()).Show();
                }
            }
            catch (Exception ex)
            {
                SecurityResultsStatus.Text = "Could not check the security settings: " + ex.Message;
                SetResultPanelColor(SecurityResultsPanel, SecurityResultsIcon, WarningColor);
            }
            finally
            {
                securityReportRunning = false;
            }
        }

        private void SecurityResultsToggle_Click(object sender, RoutedEventArgs e)
        {
            bool show = SecurityResultsList.Visibility != Visibility.Visible;
            SecurityResultsList.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            SecurityResultsToggle.Content = show ? "Hide Issues" : "Show Issues";
        }

        private async void SecurityFixButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = foundSecurityIssues.Where(i => i.IsSelected && i.CanFix).ToList();
            if (selected.Count == 0)
            {
                SecurityResultsList.Visibility = Visibility.Visible;
                SecurityResultsToggle.Content = "Hide Issues";
                SecurityResultsStatus.Text = "Tick the settings you want to fix.";
                return;
            }

            if (!await FixSecurityIssuesAsync(selected, SecurityFixButton, text => SecurityResultsStatus.Text = text))
                return;
            var issues = await Task.Run(MultronWinCleaner.Processes.SecurityCheck.Scan);
            ShowSecurityIssues(issues, lastSecurityFixMessage);
        }

        private string lastSecurityFixMessage = "";

        public async Task<bool> FixSecurityIssuesAsync(List<MultronWinCleaner.Processes.SecurityCheck.Issue> selected, System.Windows.Controls.Button button, Action<string> setStatus)
        {
            string list = string.Join("\n", selected.Take(15).Select(i => "• " + i.Title)) + (selected.Count > 15 ? $"\n• ... and {selected.Count - 15} more" : "");
            bool restart = selected.Any(i => i.NeedsRestart);
            var answer = MessageBox.Show($"Fix {selected.Count} security {(selected.Count == 1 ? "setting" : "settings")}?\n\n{list}\n\nThe current values are saved first, so you can undo the changes in Utilities > Security Check." + (restart ? "\n\nSome changes need a restart." : ""),
                "Security Check", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return false;

            button.IsEnabled = false;
            setStatus("Fixing the selected settings...");
            try
            {
                var result = await Task.Run(() => MultronWinCleaner.Processes.SecurityCheck.Fix(selected));
                lastSecurityFixMessage = $"{result.Fixed} fixed" + (result.Failed.Count > 0 ? $", {result.Failed.Count} could not be changed." : ".") + (result.NeedsRestart ? " Restart the PC to finish." : "");
                setStatus(lastSecurityFixMessage);
                if (result.Failed.Count > 0)
                    MessageBox.Show("These settings could not be changed:\n\n" + string.Join("\n", result.Failed.Select(f => "• " + f)), "Security Check", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            catch (Exception ex)
            {
                setStatus("Could not fix the settings: " + ex.Message);
                return false;
            }
            finally
            {
                button.IsEnabled = true;
            }
        }

        private TaskCompletionSource<bool>? scanStepStop;

        private static async Task<T> WithFallback<T>(Task<T?>? primary, Func<T> fallback) where T : class
        {
            T? result = primary == null ? null : await primary;
            return result ?? await Task.Run(fallback);
        }

        private async Task<(bool Stopped, T Result)> RunScanStepAsync<T>(string label, Task<T> work,
            System.Windows.Controls.Button statusButton, System.Windows.Controls.Button cancelButton)
        {
            var stop = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            scanStepStop = stop;
            bool owner = Equals(buttonStartScan.Content, "Clean");
            bool wasEnabled = buttonStartScan.IsEnabled;
            if (owner)
            {
                buttonStartScan.Content = "Cancel";
                buttonStartScan.IsEnabled = true;
                buttonStartScan.ToolTip = "Skip this step: " + label.ToLowerInvariant() + ".";
                label1_Copy.Text = label + "...";
                label1_Copy.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(InfoColor));
            }
            statusButton.Visibility = Visibility.Visible;
            cancelButton.IsEnabled = true;
            cancelButton.Visibility = Visibility.Visible;
            try
            {
                var finished = await Task.WhenAny(work, stop.Task);
                if (finished != work)
                    return (true, default!);
                return (false, await work);
            }
            finally
            {
                statusButton.Visibility = Visibility.Collapsed;
                cancelButton.Visibility = Visibility.Collapsed;
                if (scanStepStop == stop)
                    scanStepStop = null;
                if (owner)
                {
                    buttonStartScan.Content = "Clean";
                    buttonStartScan.IsEnabled = wasEnabled;
                    buttonStartScan.ToolTip = null;
                }
            }
        }

        private void StopScanStep()
        {
            scanStepStop?.TrySetResult(true);
        }

        private void ScanStepCancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button)
                button.IsEnabled = false;
            StopScanStep();
        }

        private void FirewallStatusButton_Click(object sender, RoutedEventArgs e) => utilities?.ShowFirewallCard();

        private void ShortcutStatusButton_Click(object sender, RoutedEventArgs e) => utilities?.ShowShortcutCard();

        private void SecurityStatusButton_Click(object sender, RoutedEventArgs e) => utilities?.ShowSecurityCheck();

        public async Task RunShortcutReportAsync()
        {
            if (shortcutReportRunning || shortcutScanCheckBox?.IsChecked != true || cancelstatus.IsCancellationRequested)
                return;

            shortcutReportRunning = true;
            ShortcutResultsPanel.Visibility = Visibility.Visible;
            ShortcutResultsToggle.Visibility = Visibility.Collapsed;
            ShortcutFixButton.Visibility = Visibility.Collapsed;
            ShortcutResultsList.Visibility = Visibility.Collapsed;
            ShortcutResultsStatus.Text = "Checking Desktop and Start Menu shortcuts...";
            SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, InfoColor);
            try
            {
                var step = await RunScanStepAsync("Checking Desktop and Start Menu shortcuts",
                    WithFallback(utilities?.RunShortcutReportAsync(), MultronWinCleaner.Processes.ShortcutFixer.FindBrokenShortcuts),
                    ShortcutStatusButton, ShortcutCancelButton);
                if (step.Stopped)
                {
                    ShortcutResultsStatus.Text = "The shortcut check was skipped.";
                    SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, WarningColor);
                    return;
                }
                foundShortcuts = step.Result;
                ShortcutResultsList.ItemsSource = foundShortcuts;
                if (foundShortcuts.Count == 0)
                {
                    ShortcutResultsStatus.Text = "No broken shortcuts were found.";
                    SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, OkColor);
                }
                else
                {
                    int repairable = foundShortcuts.Count(s => s.RepairTarget != null);
                    ShortcutResultsStatus.Text = $"{foundShortcuts.Count} {(foundShortcuts.Count == 1 ? "shortcut points" : "shortcuts point")} to programs that no longer exist ({repairable} can be repaired). Nothing was changed.";
                    ShortcutResultsToggle.Content = "Show Shortcuts";
                    ShortcutResultsToggle.Visibility = Visibility.Visible;
                    ShortcutFixButton.Visibility = Visibility.Visible;
                    SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, WarningColor);
                }
            }
            catch (Exception ex)
            {
                ShortcutResultsStatus.Text = "Could not check the shortcuts: " + ex.Message;
                SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, ProblemColor);
            }
            finally
            {
                shortcutReportRunning = false;
            }
        }

        private void ShortcutResultsToggle_Click(object sender, RoutedEventArgs e)
        {
            bool show = ShortcutResultsList.Visibility != Visibility.Visible;
            ShortcutResultsList.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            ShortcutResultsToggle.Content = show ? "Hide Shortcuts" : "Show Shortcuts";
        }

        private async void ShortcutFixButton_Click(object sender, RoutedEventArgs e)
        {
            var items = foundShortcuts;
            if (items.Count == 0)
                return;

            var answer = MessageBox.Show($"Fix {items.Count} broken {(items.Count == 1 ? "shortcut" : "shortcuts")}?\n\nShortcuts whose program moved to a new version folder are repaired. The others are moved to the Recycle Bin, so you can restore them.",
                "Shortcuts", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            ShortcutFixButton.IsEnabled = false;
            try
            {
                var result = await Task.Run(() => MultronWinCleaner.Processes.ShortcutFixer.Fix(items));
                foundShortcuts = result.Failed;
                ShortcutResultsList.ItemsSource = foundShortcuts;
                ShortcutResultsStatus.Text = $"{result.Repaired} repaired, {result.Removed} moved to the Recycle Bin" + (result.Failed.Count > 0 ? $", {result.Failed.Count} could not be changed." : ".");
                if (result.Failed.Count == 0)
                {
                    SetResultPanelColor(ShortcutResultsPanel, ShortcutResultsIcon, OkColor);
                    ShortcutResultsList.Visibility = Visibility.Collapsed;
                    ShortcutResultsToggle.Visibility = Visibility.Collapsed;
                    ShortcutFixButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                ShortcutResultsStatus.Text = "Could not fix the shortcuts: " + ex.Message;
            }
            finally
            {
                ShortcutFixButton.IsEnabled = true;
            }
        }

        public async Task RunFirewallReportAsync()
        {
            if (firewallReportRunning || firewallScanCheckBox?.IsChecked != true || cancelstatus.IsCancellationRequested)
                return;

            firewallReportRunning = true;
            FirewallResultsPanel.Visibility = Visibility.Visible;
            FirewallResultsToggle.Visibility = Visibility.Collapsed;
            FirewallRemoveButton.Visibility = Visibility.Collapsed;
            FirewallRulesList.Visibility = Visibility.Collapsed;
            FirewallResultsStatus.Text = "Checking Windows Firewall rules...";
            SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, InfoColor);
            try
            {
                var step = await RunScanStepAsync("Checking Windows Firewall rules",
                    WithFallback(utilities?.RunFirewallReportAsync(), MultronWinCleaner.Processes.FirewallRules.FindInvalidRules),
                    FirewallStatusButton, FirewallCancelButton);
                if (step.Stopped)
                {
                    FirewallResultsStatus.Text = "The firewall rule check was skipped.";
                    SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, WarningColor);
                    return;
                }
                foundFirewallRules = step.Result;
                FirewallRulesList.ItemsSource = foundFirewallRules;
                if (foundFirewallRules.Count == 0)
                {
                    FirewallResultsStatus.Text = "No broken firewall rules were found.";
                    SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, OkColor);
                }
                else
                {
                    FirewallResultsStatus.Text = $"{foundFirewallRules.Count} firewall {(foundFirewallRules.Count == 1 ? "rule points" : "rules point")} to programs that no longer exist on this PC. Nothing was removed.";
                    FirewallResultsToggle.Content = "Show Rules";
                    SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, ProblemColor);
                    FirewallResultsToggle.Visibility = Visibility.Visible;
                    FirewallRemoveButton.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                FirewallResultsStatus.Text = "Could not read the firewall rules: " + ex.Message;
                SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, WarningColor);
            }
            finally
            {
                firewallReportRunning = false;
            }
        }

        private void FirewallResultsToggle_Click(object sender, RoutedEventArgs e)
        {
            bool show = FirewallRulesList.Visibility != Visibility.Visible;
            FirewallRulesList.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            FirewallResultsToggle.Content = show ? "Hide Rules" : "Show Rules";
        }

        private async void FirewallRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var rules = foundFirewallRules;
            if (rules.Count == 0)
                return;

            var answer = MessageBox.Show($"Remove {rules.Count} Windows Firewall {(rules.Count == 1 ? "rule" : "rules")} for programs that no longer exist on this PC?",
                "Firewall Rules", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            FirewallRemoveButton.IsEnabled = false;
            try
            {
                var result = await Task.Run(() => MultronWinCleaner.Processes.FirewallRules.RemoveRules(rules));
                foundFirewallRules = result.Failed;
                FirewallRulesList.ItemsSource = foundFirewallRules;
                FirewallResultsStatus.Text = result.Failed.Count == 0
                    ? $"{result.Removed} firewall {(result.Removed == 1 ? "rule" : "rules")} removed."
                    : $"{result.Removed} firewall {(result.Removed == 1 ? "rule" : "rules")} removed. {result.Failed.Count} could not be removed (they may be managed by your organization).";
                if (result.Failed.Count == 0)
                {
                    SetResultPanelColor(FirewallResultsPanel, FirewallResultsIcon, OkColor);
                    FirewallRulesList.Visibility = Visibility.Collapsed;
                    FirewallResultsToggle.Visibility = Visibility.Collapsed;
                    FirewallRemoveButton.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                FirewallResultsStatus.Text = "Could not remove the firewall rules: " + ex.Message;
            }
            finally
            {
                FirewallRemoveButton.IsEnabled = true;
            }
        }

        public async Task RunDuplicateReportAsync()
        {
            if (duplicateReportRunning || duplicateScanCheckBox?.IsChecked != true || cancelstatus.IsCancellationRequested)
                return;
            bool automatic = autoclean == 1 || startupscan == 1;

            Duplicate_File_Finder finder = utilities?.GetDuplicateFinder();
            if (finder == null || finder.IsBusy)
                return;

            duplicateReportRunning = true;
            DuplicateResultsPanel.Visibility = Visibility.Visible;
            DuplicateResultsButton.Content = "Show Status";
            DuplicateResultsButton.Visibility = Visibility.Visible;
            DuplicateCancelButton.IsEnabled = true;
            DuplicateCancelButton.Visibility = Visibility.Visible;
            DuplicateResultsStatus.Text = "Looking for identical files in your personal folders...";
            SetResultPanelColor(DuplicateResultsPanel, DuplicateResultsIcon, InfoColor);
            EnterDuplicateCancelMode();
            progressBar1.Value = 0;
            ShowDuplicateProgress(DuplicateResultsStatus.Text);

            var statusTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            statusTimer.Tick += (s, e) =>
            {
                string status = finder.CurrentStatus;
                if (status.Length == 0)
                    return;
                if (DuplicateCancelButton.IsEnabled)
                    DuplicateResultsStatus.Text = status;
                ShowDuplicateProgress(status);
            };
            statusTimer.Start();
            try
            {
                var result = await finder.RunReportScanAsync();
                statusTimer.Stop();
                DuplicateResultsButton.Content = "Show Files";
                DuplicateResultsButton.Visibility = Visibility.Collapsed;
                if (finder.WasCanceled)
                {
                    DuplicateResultsStatus.Text = result.Copies > 0
                        ? $"The duplicate search was stopped. {result.Copies} duplicate {(result.Copies == 1 ? "copy" : "copies")} found so far ({formatsize(result.Bytes)}). Nothing was deleted."
                        : "The duplicate search was stopped.";
                    DuplicateResultsButton.Visibility = result.Copies > 0 ? Visibility.Visible : Visibility.Collapsed;
                    SetResultPanelColor(DuplicateResultsPanel, DuplicateResultsIcon, WarningColor);
                }
                else if (result.Copies == 0)
                {
                    DuplicateResultsStatus.Text = "No duplicate files were found in your personal folders.";
                    SetResultPanelColor(DuplicateResultsPanel, DuplicateResultsIcon, OkColor);
                }
                else
                {
                    DuplicateResultsStatus.Text = $"{result.Copies} duplicate {(result.Copies == 1 ? "copy" : "copies")} of {result.Groups} {(result.Groups == 1 ? "file" : "files")} found ({formatsize(result.Bytes)} could be freed). Nothing was deleted.";
                    DuplicateResultsButton.Visibility = Visibility.Visible;
                    SetResultPanelColor(DuplicateResultsPanel, DuplicateResultsIcon, WarningColor);
                    if (automatic)
                    {
                        new Notify("Duplicate Files Found",
                            $"{result.Copies} duplicate {(result.Copies == 1 ? "copy" : "copies")} in your personal folders. Click to review.",
                            formatsize(result.Bytes),
                            () => utilities?.ShowDuplicateFinder()).Show();
                    }
                }
            }
            catch (Exception ex)
            {
                DuplicateResultsStatus.Text = "The duplicate file search failed: " + ex.Message;
                DuplicateResultsButton.Visibility = Visibility.Collapsed;
                SetResultPanelColor(DuplicateResultsPanel, DuplicateResultsIcon, ProblemColor);
            }
            finally
            {
                statusTimer.Stop();
                duplicateReportRunning = false;
                DuplicateCancelButton.Visibility = Visibility.Collapsed;
                bool wasWaiting = duplicateCancelMode;
                LeaveDuplicateCancelMode();
                if (wasWaiting && Equals(buttonStartScan.Content, "Clean"))
                {
                    progressBar1.Value = 100;
                    label1_Copy.Text = DuplicateResultsStatus.Text;
                    label1_Copy.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(InfoColor));
                }
            }
        }

        private bool duplicateCancelMode;
        private bool duplicateCancelWasEnabled;

        private void EnterDuplicateCancelMode()
        {
            if (duplicateCancelMode || !Equals(buttonStartScan.Content, "Clean"))
                return;
            duplicateCancelMode = true;
            duplicateCancelWasEnabled = buttonStartScan.IsEnabled;
            buttonStartScan.Content = "Cancel";
            buttonStartScan.IsEnabled = true;
            buttonStartScan.ToolTip = "Stop the duplicate file search.";
        }

        private void LeaveDuplicateCancelMode()
        {
            if (!duplicateCancelMode)
                return;
            duplicateCancelMode = false;
            buttonStartScan.Content = "Clean";
            buttonStartScan.IsEnabled = duplicateCancelWasEnabled;
            buttonStartScan.ToolTip = null;
        }

        private void ShowDuplicateProgress(string status)
        {
            if (!duplicateCancelMode)
                return;
            label1_Copy.Text = "Duplicate files: " + status;
            label1_Copy.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(InfoColor));
        }

        private void CancelDuplicateReport()
        {
            if (!duplicateReportRunning)
                return;
            DuplicateCancelButton.IsEnabled = false;
            DuplicateResultsStatus.Text = "Stopping the duplicate file search...";
            utilities?.GetDuplicateFinder()?.CancelScan();
        }

        private void DuplicateCancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelDuplicateReport();
        }

        private void DuplicateResultsButton_Click(object sender, RoutedEventArgs e)
        {
            utilities?.ShowDuplicateFinder();
        }

        public async Task startup()
        {
            if (App.LaunchedFromStartup)
            {
                await Task.Delay(3000);

                bool scanEnabled = false;
                bool cleanEnabled = false;

                await this.Dispatcher.InvokeAsync(() =>
                {
                    scanEnabled = settings.chkEnableStartupScan.IsChecked == true;
                    cleanEnabled = settings.chkEnableStartupClean.IsChecked == true;
                });

                if (scanEnabled || cleanEnabled)
                {
                  
                    this.startupscan = 1;
                    if (cleanEnabled)
                      this.startupclean = 1;
                   
                    await startscan();
                
                }
            }
        }
        public async Task loadothers()
        {
            await createshortcut("cleanmgr.exe=/d C:=(Opens Disk Cleanup for a specific drive)=shortcut_0", "cleanmgr.exe Commands");
            await createshortcut("cleanmgr.exe=/sagerun:1=(Configures advanced cleanup settings for auto-run)=shortcut1", "cleanmgr.exe Commands");
            await createshortcut("cleanmgr.exe=/lowdisk=(Prompts to clean default unnecessary files)=shortcut2", "cleanmgr.exe Commands");
            await createshortcut("cleanmgr.exe=/verylowdisk=(Silently clears default unnecessary files)=shortcut3", "cleanmgr.exe Commands");
             
            await createshortcut("Dism.exe=/Online /Cleanup-Image /StartComponentCleanup=warning=(No Warning)=winsxs", "Dism.exe Commands");
            await createshortcut("Dism.exe=/Online /Cleanup-Image /RestoreHealth=warning=(No Warning)=health","Dism.exe Commands");
            await createshortcut("sfc.exe=sfc /scannow=warning=(No Warning)=sfc", "SFC Commands");
            await createshortcut("Deep Log Files Scan=C:\\=warning=Its can take long time.=logscan", "Deep Log Files Scan");

        }

        public async Task createshortcut(string checkboxname, string expander)
        { 
            CheckBox checkbox = new CheckBox
            {
                Content = checkboxname,
                Margin = new Thickness(10),
                IsChecked = false,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = brush, 
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkbox.Checked += CheckBox2_Checked;
            checkbox.Unchecked += CheckBox2_Unchecked;
            string commandKey = "command:" + checkboxname.Split('=').Last();
            checkbox.IsChecked = MultronWinCleaner.Processes.Load.ReadSelections().TryGetValue(commandKey, out bool savedCommand) && savedCommand;
            checkbox.Checked += (s, e) => SaveCommandSelection(commandKey, true);
            checkbox.Unchecked += (s, e) => SaveCommandSelection(commandKey, false);
            checkboxes2.Add(checkbox);

            Expander existingExpander = expanders.FirstOrDefault(e => e.Header != null && e.Header.ToString() == expander);

            if (existingExpander != null)
            {
                
                if (existingExpander.Content is StackPanel existingPanel)
                {
                    existingPanel.Children.Add(checkbox);
                }
            }
            else
            {
               
                StackPanel groupBoxContent2 = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                stackpanels.Add(groupBoxContent2);

                groupBoxContent2.Children.Add(checkbox);

                Expander newExpander1 = new Expander
                {
                    Header = expander,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = brush,
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(2),
                    FontSize = 14,
                    FontWeight = FontWeights.Regular,
                    IsEnabled = true,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };

                newExpander1.Content = groupBoxContent2;
                expanders.Add(newExpander1);

                try
                {
                    wrapPanel1.Children.Add(newExpander1);
                }
                catch (Exception)
                {
                }
            }
        }

        private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox == null || checkBox.Content == null) return;

            string content = checkBox.Content.ToString();
             
            if (content.Contains("Dism.exe") || content.Contains("cleanmgr.exe") || content.Contains("Deep Log Files Scan") || content.Contains("sfc.exe"))
            {
                database.Insert(0, content);
            }
        }
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)

        {


            CheckBox checkBox = sender as CheckBox;

            string content = checkBox.Content.ToString();

            if (content.Contains("Dism.exe") || content.StartsWith("cleanmgr.exe") || content.Contains("Deep Log Files Scan") || content.Contains("sfc.exe"))

            {
                        database.Remove(content);

            }

        }
        private void ProgressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateArc(progressBar1.Value);
        }


        private System.Windows.Shapes.Path _arcPath;
        public void UpdateArc(double percent)
        {

            percent = Math.Max(0, Math.Min(100, percent));

            double angle = 360 * (percent / 100);
            double radius = progressBar1.ActualWidth / 2 - 7.5;
            var center = new System.Windows.Point(progressBar1.ActualWidth / 2, progressBar1.ActualHeight / 2);

            if (_arcPath == null)
                _arcPath = (System.Windows.Shapes.Path)progressBar1.Template.FindName("PART_Indicator", progressBar1);

            if (_arcPath == null) return;

            Geometry g;

            if (percent <= 0)
            {
                g = Geometry.Empty;
            }
            else if (percent >= 99.999)
            {
                g = new EllipseGeometry(center, radius, radius);
            }
            else
            {
                var start = new System.Windows.Point(center.X, center.Y - radius);

                double rad = (Math.PI / 180) * (angle - 90);
                var end = new System.Windows.Point(
                    center.X + radius * Math.Cos(rad),
                    center.Y + radius * Math.Sin(rad));

                bool largeArc = angle > 180;

                var fig = new PathFigure { StartPoint = start };
                fig.Segments.Add(new ArcSegment(
                    end,
                    new System.Windows.Size(radius, radius),
                    0,
                    largeArc,
                    SweepDirection.Clockwise,
                    true));

                g = new PathGeometry(new[] { fig });
            }

            _arcPath.Data = g;
        }


        private void TrayIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {

            if (this.Visibility == Visibility.Hidden)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                this.Visibility = Visibility.Visible;
            }

            this.Activate();
        }
        private void HideApp_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            settings.Hide();
        }
        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }



        public string stringtokenizer(string input, string token, int index)
        {

            string[] tokens = input.Split(token);

            if (index >= 0 && index < tokens.Length)
            {
                return (tokens[index]);
            }
            else
            {
                return (null);
            }
        }
        public List<string> database = new List<string>();
        public static int scanstatus = 0;
        public CancellationTokenSource cancelstatus = new CancellationTokenSource();

  

        public string formatsize(long size)
        {
            if (size < 0)
                return "0 Byte";

            string[] sizes = { "Byte", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        private void FilesListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox;
            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += FilesListBox_ScrollChanged;
            }
        }

        private bool isLoading = false;

        private async void FilesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                if (sender is ScrollViewer scrollViewer && scrollViewer.DataContext is MultronWinCleaner.Processes.Scan.GroupViewModel vm)
                {
                    if (isLoading) return;

                    isLoading = true;
                    await vm.LoadMoreFilesAsync();
                    isLoading = false;
                }
            }
        }

        public T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                    return t;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }
        public static T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is T t)
                    return t;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }




        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (settings.chkTrayIcon.IsChecked == true && TrayIcon.Visibility == Visibility.Visible)
            {
                this.Hide();
                settings.Hide();
            }
            else
            {
                Environment.Exit(0);
            }

        }
       
     


        public void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {

                string content = checkBox.Content.ToString();

            
               string name = stringtokenizer(content, "=", 0);
               string path = stringtokenizer(content, "=", 1);

               database.Add(name + "=" + path);
               QueueSelectionAutoSave(checkBox, path);


            }
        }
        public void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                string content = checkBox.Content.ToString();
                string name = stringtokenizer(content, "=", 0);
                string path = stringtokenizer(content, "=", 1);
                database.Remove(name + "=" + path);
                QueueSelectionAutoSave(checkBox, path);
           }





          
        }
        private async void ScrollViewerWrap_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {

            if (sender is not ScrollViewer scrollViewer) return;

            double verticalOffset = scrollViewer.VerticalOffset;
            double viewportHeight = scrollViewer.ViewportHeight;
            double extentHeight = scrollViewer.ExtentHeight;




            const double threshold = 20.0;
            bool isAtBottom = (extentHeight - (verticalOffset + viewportHeight)) <= threshold;

            if (!isAtBottom) return;


            foreach (Expander expander in expanders)
            {
                if (expander.Parent == null && !wrapPanel1.Children.Contains(expander))
                {
                    wrapPanel1.Children.Add(expander);
                    break;
                }
            }

        }
        private double previousWidth, previousHeight, previousLeft, previousTop;

        private bool isMaximized = false;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isMaximized)
            {
                this.WindowState = WindowState.Normal;
                this.Width = previousWidth;
                this.Height = previousHeight;
                this.Left = previousLeft;
                this.Top = previousTop;
                isMaximized = false;
                buttonMaximize.Content = "↔";
            }
            else
            {

                previousWidth = this.Width;
                previousHeight = this.Height;
                previousLeft = this.Left;
                previousTop = this.Top;


                this.WindowState = WindowState.Normal;
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
                this.Width = SystemParameters.WorkArea.Width;
                this.Height = SystemParameters.WorkArea.Height;

                isMaximized = true;
                buttonMaximize.Content = "↔";
            }
        }
        public static byte[] getrandombytes(long size)
        {
            byte[] randombs = new byte[size];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetNonZeroBytes(randombs);
            return randombs;
        }


        public int cancelclean = 0;
        public int reset = 0;

        public static class SystemActions
        {

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

            const uint EWX_LOGOFF = 0x00000000;
            const uint EWX_SHUTDOWN = 0x00000001;
            const uint EWX_REBOOT = 0x00000002;
            const uint EWX_FORCE = 0x00000004;
            const uint EWX_FORCEIFHUNG = 0x00000010;


            public static bool LogOff()
            {
                return ExitWindowsEx(EWX_LOGOFF | EWX_FORCE, 0);
            }


            public static void Restart()
            {
                Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }

            public static void Shutdown()
            {
                Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
        }
        public class Kill
        {
            private MainWindow main;
            private long totalsize = 0;
            private CancellationTokenSource cts = new CancellationTokenSource();

            public Kill(MainWindow main)
            {
                this.main = main;
            }

            public async Task ScandotsAsync(string text, CancellationTokenSource cancellationToken)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = text + ".";
                            main.label1_Copy.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7"));
                        });
                        await Task.Delay(1000, cancellationToken.Token);

                        await main.Dispatcher.InvokeAsync(() => { main.label1_Copy.Text = text + ".."; });
                        await Task.Delay(1000, cancellationToken.Token);

                        await main.Dispatcher.InvokeAsync(() => { main.label1_Copy.Text = text + "..."; });
                        await Task.Delay(1000, cancellationToken.Token);
                    }
                }
                catch (TaskCanceledException) { }
            }
            void HideChildrenOfAllDockPanels(Panel parent)
            {
                foreach (UIElement child in parent.Children)
                {
                    if (child is DockPanel dockPanel)
                    {
                        foreach (UIElement dpChild in dockPanel.Children)
                        {
                            dpChild.Visibility = Visibility.Hidden;
                        }
                    }

                    if (child is Panel nestedPanel)
                    {
                        HideChildrenOfAllDockPanels(nestedPanel);
                    }
                }
            }
            public async Task run()
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    HideChildrenOfAllDockPanels(main.Panel);
                    main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                    main.wrapPanelDirectories.Visibility = Visibility.Visible;
                    main.progressBar1.Minimum = 0;
                    main.progressBar1.Maximum = main.paths.Count;
                    main.progressBar1.Value = 0;

                    main.wrapPanelDirectories.Children.Add(new TextBlock
                    {
                        Text = "Started...",
                        Foreground = System.Windows.Media.Brushes.Black,
                        Margin = new Thickness(5)
                    });
                });

                var dotsTask = ScandotsAsync("Killing", cts);
                int progress = 0;
                int killed = 0;
                int deleted = 0;
                List<string> lockedPaths = await main.Dispatcher.InvokeAsync(() =>
                    main.paths.Select(p => main.stringtokenizer(p, "=", 0)).Where(p => p.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

                for (int i = 0; i < lockedPaths.Count; i++)
                {
                    if (main.cancelstatus.IsCancellationRequested) break;

                    string path = lockedPaths[i];

                    if (System.IO.File.Exists(path))
                    {
                        long fileSize = 0;
                        string error = TryDeleteLocked(path, ref fileSize, out bool inUse);

                        if (error != null && inUse)
                        {
                            foreach (var locker in whousef.GetLockers(path))
                            {
                                if (whousef.IsProtectedProcess(locker.Id, locker.Name))
                                {
                                    await AddKillMessage($"Skipped system process: {locker.Name} (PID {locker.Id}) for {path}", System.Windows.Media.Brushes.Goldenrod);
                                    continue;
                                }

                                try
                                {
                                    await AddKillMessage($"Killing: {locker.Name} (PID {locker.Id}) for {path}", System.Windows.Media.Brushes.Red);
                                    using (Process proc = Process.GetProcessById(locker.Id))
                                    {
                                        proc.Kill();
                                        proc.WaitForExit(5000);
                                    }
                                    killed++;
                                }
                                catch (ArgumentException)
                                {
                                }
                                catch (Exception ex)
                                {
                                    await AddKillMessage($"Cannot Kill: {locker.Name} (PID {locker.Id}) - {ex.Message}", System.Windows.Media.Brushes.Goldenrod);
                                }
                            }

                            for (int attempt = 0; attempt < 3 && error != null && inUse; attempt++)
                            {
                                await Task.Delay(300);
                                error = TryDeleteLocked(path, ref fileSize, out inUse);
                            }
                        }

                        if (error == null)
                        {
                            deleted++;
                            totalsize += fileSize;
                            await AddKillMessage($"Deleted: {path} ({main.formatsize(fileSize)})", System.Windows.Media.Brushes.Green);
                        }
                        else
                        {
                            await AddKillMessage($"Cannot Delete: {path} - {error}", System.Windows.Media.Brushes.Goldenrod);
                        }
                    }
                    else
                    {
                        await AddKillMessage($"File no longer exists: {path}", InfoBrush);
                    }

                    progress++;
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        double percent = (progress * 100.0) / lockedPaths.Count;
                        main.progressBar1.Value = percent;
                        main.UpdateArc(percent);
                    });
                }

                await cts.CancelAsync();
                await dotsTask;

                await main.Dispatcher.InvokeAsync(() =>
                {
                    string message;
                    System.Windows.Media.Brush labelColor;

                    if (main.cancelstatus.IsCancellationRequested)
                    {
                        message = $"Killing Canceled. {killed} Process Killed, {deleted} Files Deleted. Freed Space: {main.formatsize(totalsize)}";
                        labelColor = System.Windows.Media.Brushes.Goldenrod;
                    }
                    else
                    {
                        message = $"Killing Done! {killed} Process Killed, {deleted} Files Deleted. Freed Space: {main.formatsize(totalsize)}";
                        labelColor = System.Windows.Media.Brushes.Red;
                    }

                    main.label1_Copy.Text = message;
                    main.label1_Copy.Foreground = labelColor;
                    main.buttonReset.Visibility = Visibility.Visible;
                    main.buttonStartScan.Content = "Scan";
                    main.buttonStartScan.IsEnabled = false;
                    main.paths.Clear();
                });
            }

            private static readonly System.Windows.Media.SolidColorBrush InfoBrush = CreateFrozenBrush("#0078d7");

            private static System.Windows.Media.SolidColorBrush CreateFrozenBrush(string hex)
            {
                var brush = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                brush.Freeze();
                return brush;
            }

            private static string TryDeleteLocked(string path, ref long fileSize, out bool inUse)
            {
                inUse = false;
                try
                {
                    FileInfo info = new FileInfo(path);
                    if (!info.Exists)
                        return null;
                    fileSize = info.Length;
                    if (info.IsReadOnly)
                        info.IsReadOnly = false;
                    info.Delete();
                    return null;
                }
                catch (IOException ex) when ((ex.HResult & 0xFFFF) == 0x20 || (ex.HResult & 0xFFFF) == 0x21)
                {
                    inUse = true;
                    return ex.Message;
                }
                catch (UnauthorizedAccessException)
                {
                    return "Access denied. Only Windows can delete this file.";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }

            private async Task AddKillMessage(string text, System.Windows.Media.Brush brush)
            {
                if (brush.CanFreeze && !brush.IsFrozen)
                    brush.Freeze();
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.wrapPanelDirectories.Children.Add(new TextBlock
                    {
                        Text = text,
                        Foreground = brush,
                        FontSize = 16,
                        Margin = new Thickness(5)
                    });
                });
            }
        }
        public ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel> LockedFileGroups { get; } = new();
        public CollectionViewSource groupedProcesses = new CollectionViewSource();
        public List<MultronWinCleaner.Processes.Clean.LockedProcessViewModel> _allLockedFileGroups = new List<MultronWinCleaner.Processes.Clean.LockedProcessViewModel>();
        public ObservableCollection<MultronWinCleaner.Processes.Clean.LockedProcessViewModel> LockedProcesses = new ObservableCollection<MultronWinCleaner.Processes.Clean.LockedProcessViewModel>();
        public int _itemsLoaded = 0;
        public int _pageSize = 50;


        public async Task startscan()
        {
            if (buttonStartScan.Content.Equals("Clean"))
            {
                cancelstatus = new CancellationTokenSource();
                dismcancel = new CancellationTokenSource();
                scanstatus = 0;
                cancelclean = 0;
             



                buttonStartScan.Content = "Cancel"; 

                wrapPanelDirectories.Children.Clear();
                MultronWinCleaner.Processes.Clean clean = new MultronWinCleaner.Processes.Clean(this);
                await Task.Run(() => clean.run());
            }
            else if (buttonStartScan.Content.Equals("Scan"))
            {
                DuplicateResultsPanel.Visibility = Visibility.Collapsed;
                FirewallResultsPanel.Visibility = Visibility.Collapsed;
                ShortcutResultsPanel.Visibility = Visibility.Collapsed;
                SecurityResultsPanel.Visibility = Visibility.Collapsed;
                HideSystemChecks();
                cancelstatus.Dispose();
                dismcancel.Dispose();
                cancelstatus = new CancellationTokenSource();
                dismcancel = new CancellationTokenSource();



                scanstatus = 0;
                cancelclean = 0;
                MultronWinCleaner.Processes.Scan scan = new MultronWinCleaner.Processes.Scan(this);

                buttonStartScan.Content = "Cancel";

                await Task.Run(() => scan.run());

            }
            else if (buttonStartScan.Content.Equals("Cancel"))
            {
                StopScanStep();
                CancelDuplicateReport();
                cancelstatus.Cancel();
                dismcancel.Cancel();
                cancelclean = 2;
                buttonReset.Visibility = Visibility.Visible;

                
            }
            else if (buttonStartScan.Content.Equals("Kill"))
            {
                if (paths.Count != 0)
                {
                    killer = 1;
                    cancelstatus.Dispose();
                    dismcancel.Dispose();
                    cancelstatus = new CancellationTokenSource();
                    dismcancel = new CancellationTokenSource();
                    cancelclean = 0;
                    LockedFilesWindowOverlay.Visibility = Visibility.Collapsed;
                    wrapPanelDirectories.Children.Clear();
              
                    buttonStartScan.Content = "Cancel";
                    Kill kill = new Kill(this);
                    await Task.Run(() => kill.run());
                }
                else
                {
                    label1_Copy.Text = "No Process Selected.";
                    label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                }
            }

       
            if (scanstatus == 1 || scanstatus == 2)
            {
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Children.Clear();
                buttonStartScan.IsEnabled = true;

                wrapPanel1.Visibility = Visibility.Visible;
                buttonStartScan.Content = "Scan";

                
                 
                scanstatus = 0;
            }
            else if (cancelclean == 1)
            {
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Children.Clear();
                buttonStartScan.IsEnabled = true;

                wrapPanel1.Visibility = Visibility.Visible;
                cancelclean = 2;
                buttonStartScan.Content = "Scan";
            }
        }
        public bool IsScanOrCleanBusy => Equals(buttonStartScan.Content, "Cancel");

        public void CancelScanOrClean()
        {
            if (IsScanOrCleanBusy && buttonStartScan.IsEnabled)
                ButtonStartScan_Click(buttonStartScan, new RoutedEventArgs());
        }

        private async void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
            if (scanStepStop != null)
            {
                buttonStartScan.IsEnabled = false;
                StopScanStep();
                return;
            }
            if (duplicateCancelMode)
            {
                buttonStartScan.IsEnabled = false;
                duplicateCancelWasEnabled = true;
                label1_Copy.Text = "Stopping the duplicate file search...";
                label1_Copy.Foreground = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(WarningColor));
                CancelDuplicateReport();
                return;
            }
            
            bool allUnchecked = !Equals(buttonStartScan.Content, "Cancel") && checkboxes2.All(cb => cb.IsChecked != true);

            if (allUnchecked)
            {

                label1_Copy.Text = "Nothing is selected!";
                label1_Copy.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7"));
                return;
            }
             
            await startscan();
        }
        public void OpenDirectory_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);
                try
                {
                    if (System.IO.File.Exists(wrapdir))
                    {
                        string dir = System.IO.Path.GetDirectoryName(wrapdir);
                        if (dir != null && Directory.Exists(dir))
                        {
                            Process.Start("explorer.exe", dir);
                        }

                        else
                        {
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }


                    else
                    {
                        if (wrapdir != null && Directory.Exists(wrapdir))
                            Process.Start("explorer.exe", wrapdir);
                        else
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


        }
        public void OpenFileLocation_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);
                try
                {
                    if (System.IO.File.Exists(wrapdir))
                        Process.Start("explorer.exe", $"/select,\"{wrapdir}\"");
                    else
                    {
                        string? dir = System.IO.Path.GetDirectoryName(wrapdir);
                        if (dir != null && Directory.Exists(dir))
                            Process.Start("explorer.exe", dir);
                        else
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


        }

        public void CopyPath_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);

                try
                {
                    if (wrapdir != null)
                    {
                        Clipboard.SetText(wrapdir);
                    }
                    else
                    {
                        MessageBox.Show("Directory returned null.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.DataContext is not Scan.FileItem fileItem) return;

            string path = fileItem.Path;
            try
            {
                if (System.IO.File.Exists(path))
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                else
                {
                    string? dir = System.IO.Path.GetDirectoryName(path);
                    if (dir != null && Directory.Exists(dir))
                        Process.Start("explorer.exe", dir);
                    else
                        MessageBox.Show("Path not found:\n" + path, "Open File Location",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToExceptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.DataContext is Scan.FileItem fileItem)
            {
                settings.addexception(fileItem.Path);
                fileItem.IsChecked = false;
                label1_Copy.Text = "Added to Exceptions: " + fileItem.Path;
            }
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is Scan.FileItem fileItem)
            {
                Clipboard.SetText(fileItem.Path);
            }
        }

        private void OpenDiscord_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://discord.gg/xXmQw3MUAR";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {

            settings.Show();
            settings.WindowState = WindowState.Normal;
        }

        int doit = 0;
        private void UtilitiesButton_Click(object sender, RoutedEventArgs e)
        {

            utilities.Show();
            settings.WindowState = WindowState.Normal;


        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }





        public class LoadLockedFiles
        {
            MainWindow main;
            public LoadLockedFiles(MainWindow main)
            {
                this.main = main;
            }

            private sealed class LockedFileInfo
            {
                public string Path;
                public string GroupName;
                public string SizeText;
                public List<(string Id, string Name)> Processes;
            }

            private async Task<List<LockedFileInfo>> CheckWhoUsesMultipleAsync(List<string> entries)
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7"));
                });

                var files = new Dictionary<string, (string Id, string Name, string Group)>(StringComparer.OrdinalIgnoreCase);
                foreach (string entry in entries)
                {
                    string path = main.stringtokenizer(entry, "=", 0);
                    if (path.Length > 0 && !files.ContainsKey(path))
                        files[path] = (main.stringtokenizer(entry, "=", 1), main.stringtokenizer(entry, "=", 2), main.stringtokenizer(entry, "=", 3));
                }

                var results = new ConcurrentBag<LockedFileInfo>();
                int total = files.Count;
                int processedCount = 0;
                var reportTimer = Stopwatch.StartNew();

                await Task.Run(() => Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 8 }, file =>
                {
                    try
                    {
                        var info = new FileInfo(file.Key);
                        if (!info.Exists)
                            return;

                        var processes = whousef.GetLockers(file.Key).Select(p => (p.Id.ToString(), p.Name)).ToList();
                        if (processes.Count == 0)
                            processes.Add((file.Value.Id, file.Value.Name));

                        results.Add(new LockedFileInfo
                        {
                            Path = file.Key,
                            GroupName = file.Value.Group,
                            SizeText = main.formatsize(info.Length),
                            Processes = processes
                        });
                    }
                    catch (Exception) { }
                    finally
                    {
                        int done = Interlocked.Increment(ref processedCount);
                        if (done == total || reportTimer.ElapsedMilliseconds > 150)
                        {
                            reportTimer.Restart();
                            main.Dispatcher.InvokeAsync(() => main.label1_Copy.Text = $"Loading locked files: {done}/{total}");
                        }
                    }
                }));

                return results.OrderBy(r => r.GroupName).ThenBy(r => r.Path).ToList();
            }

            public async Task run()
            {
                List<string> entries = await main.Dispatcher.InvokeAsync(() => main.paths.ToList());
                List<LockedFileInfo> lockedFiles = await CheckWhoUsesMultipleAsync(entries);

                var allItems = new List<LockedProcessViewModel>();
                var added = new HashSet<string>();
                foreach (var file in lockedFiles)
                {
                    foreach (var proc in file.Processes)
                    {
                        if (!added.Add($"{proc.Id}|{file.Path}"))
                            continue;

                        allItems.Add(new LockedProcessViewModel
                        {
                            FilePath = file.Path + " " + file.SizeText,
                            FilePathWithoutSize = file.Path,
                            GroupName = file.GroupName,
                            DisplayName = proc.Id == "0" ? "No locking process found (delete will be retried)" : $"{proc.Name} (PID {proc.Id})",
                            Id = proc.Id,
                            IsChecked = true,
                            OnCheckedChanged = (model, state) =>
                            {
                                string key = model.FilePathWithoutSize + "=" + model.Id + "=" + model.DisplayName;
                                if (state)
                                {
                                    if (!main.paths.Contains(key))
                                        main.paths.Add(key);
                                }
                                else
                                {
                                    main.paths.RemoveAll(p =>
                                        main.stringtokenizer(p, "=", 0).Equals(model.FilePathWithoutSize, StringComparison.OrdinalIgnoreCase));
                                }
                            }
                        });
                    }
                }

                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.LockedFileGroups.Clear();
                    main._allLockedFileGroups = allItems;
                    main.LockedProcesses = new ObservableCollection<LockedProcessViewModel>(allItems);
                    main._itemsLoaded = allItems.Count;

                    var cvs = new CollectionViewSource { Source = main.LockedProcesses };
                    cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LockedProcessViewModel.GroupName)));

                    var view = cvs.View;
                    string search = (main.SearchBox.Text ?? "").Trim().ToLower();

                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        view.Filter = item =>
                        {
                            if (item is LockedProcessViewModel p)
                                return p.FilePathWithoutSize?.ToLower().Contains(search) == true ||
                                       p.GroupName?.ToLower().Contains(search) == true ||
                                       p.DisplayName?.ToLower().Contains(search) == true;
                            return false;
                        };
                    }

                    main.groupedProcesses = cvs;
                    main.listBoxProcesses.ItemsSource = view;

                    var stillLockedPaths = lockedFiles.Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    main.paths.RemoveAll(p => !stillLockedPaths.Contains(main.stringtokenizer(p, "=", 0)));

                    main.label1_Copy.Text = $"Locked Files: {lockedFiles.Count} files, {allItems.Count} processes";
                });
            }
        }

        private async void ButtonLocked_Click(object sender, RoutedEventArgs e)
        {
            wrapPanelDirectories.Visibility = Visibility.Hidden;
            dataGridGroups.Visibility = Visibility.Hidden;
            ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
            Datagridscroll.Visibility = Visibility.Hidden;
            ScrollViewerDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Children.Clear();
            viewModel.Groups.Clear();
            Datagridscroll.Visibility = Visibility.Hidden;
            dataGridGroups.Visibility = Visibility.Hidden;
            ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
            wrapPanel1.Visibility = Visibility.Hidden;
            ButtonLockedFiles.Visibility = Visibility.Hidden;
            buttonStartScan.IsEnabled = true;
            buttonStartScan.Content = "Kill";
            LockedFilesWindowOverlay.Visibility = Visibility.Visible;
            LoadLockedFiles lockedfiles = new LoadLockedFiles(this);
            await Task.Run(() => lockedfiles.run());

        }
        private void CloseLockedFiles_Click(object sender, RoutedEventArgs e)
        {
            LockedFilesWindowOverlay.Visibility = Visibility.Collapsed;
        }

        private void CloseLockedFiles_MouseDown(object sender, MouseButtonEventArgs e)
        {
          
            LockedFilesWindowOverlay.Visibility = Visibility.Collapsed;
        }

        private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
            wrapPanelDirectories.Visibility = Visibility.Hidden;

            dataGridGroups.Visibility = Visibility.Hidden;
            ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
            Datagridscroll.Visibility = Visibility.Hidden;
            ScrollViewerDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Children.Clear();
            viewModel.Groups.Clear();
            Datagridscroll.Visibility = Visibility.Hidden;
            dataGridGroups.Visibility = Visibility.Hidden;
            buttonReset.Visibility = Visibility.Hidden;
            ScrollViewerDetectedFiles.Visibility = Visibility.Visible;
            wrapPanel1.Visibility = Visibility.Visible;
            ButtonLockedFiles.Visibility = Visibility.Hidden;
            buttonStartScan.Content = "Scan";
            buttonStartScan.IsEnabled = true;


            progressBar1.Value = 0;
            startupscan = 0;
            startupclean = 0;
            autoclean = 0;
            cancelclean = 0;
            scanstatus = 0;
            reset = 0;
        }
        int backuped = 0;

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string searchText = textBox.Text.Trim().ToLower();

            if (backuped == 0)
            {
                viewModelbac.Groups.Clear();
                foreach (var group in viewModel.Groups)
                    viewModelbac.Groups.Add(group);

                backuped = 1;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                viewModel.Groups.Clear();
                foreach (var group in viewModelbac.Groups)
                    viewModel.Groups.Add(group);

                backuped = 0;
                return;
            }


            var filteredList = await Task.Run(() =>
                viewModelbac.Groups
                    .Where(g => !string.IsNullOrEmpty(g.Path) && g.Path.ToLower().Contains(searchText))
                    .ToList());

            viewModel.Groups.Clear();
            foreach (var group in filteredList)
                viewModel.Groups.Add(group);
        }


        private void ToggleThemeSwitch_Checked(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
            string fileContent = System.IO.File.ReadAllText(path);

            fileContent = fileContent.Replace("themes:0", "").TrimEnd().ToLower();

            if (!fileContent.Contains("themes:1"))
            {
                if (!fileContent.EndsWith(Environment.NewLine))
                    fileContent += Environment.NewLine;

                fileContent += "themes:1" + Environment.NewLine;
                System.IO.File.WriteAllText(path, fileContent);
            }


            string themePath = "Themes/Dark.xaml";
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };
            brush = (SolidColorBrush)resourceDictionary["Text"];

            themeselector.selector(new Uri(themePath, UriKind.Relative));

            foreach (CheckBox box in checkboxes2)
                box.Foreground = brush;

            foreach (Expander er in expanders)
                er.Foreground = brush;
        }

        private void ToggleThemeSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");


            string fileContent = System.IO.File.ReadAllText(path);

            fileContent = fileContent.Replace("themes:1", "").TrimEnd();


            if (!fileContent.Contains("themes:0"))
            {

                if (!fileContent.EndsWith(Environment.NewLine))
                    fileContent += Environment.NewLine;

                fileContent += "themes:0" + Environment.NewLine;
                System.IO.File.WriteAllText(path, fileContent);
            }


            string themePath = "Themes/Light.xaml";


            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };


            brush = (SolidColorBrush)resourceDictionary["Text"];


            themeselector.selector(new Uri(themePath, UriKind.Relative));


            foreach (CheckBox box in checkboxes2)
                box.Foreground = brush;

            foreach (Expander er in expanders)
                er.Foreground = brush;
        }

        private ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel> _backupLockedFileGroups = new ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel>();
        private async void ProcessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = (sender as TextBox)?.Text?.Trim().ToLower() ?? "";

            await UpdateProcessViewModel(searchText);
        }
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = (sender as TextBox)?.Text?.Trim().ToLower() ?? "";

            await UpdateListViewAsync(searchText);
        }
        private async Task UpdateProcessViewModel(string searchText)
        {

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    groupedProcesses.View.Filter = null;

                }
                else
                {

                    groupedProcesses.View.Filter = item =>
                    {
                        if (item is MultronWinCleaner.Processes.Clean.LockedProcessViewModel proc)
                        {
                            return proc.DisplayName?.ToLower().Contains(searchText) == true;
                        }
                        return false;
                    };
                }
                listBoxProcesses.ItemsSource = groupedProcesses.View;
            });
        }
        private async Task UpdateListViewAsync(string searchText)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    groupedProcesses.View.Filter = null;
                }
                else
                {
                    string search = searchText.ToLower();
                    groupedProcesses.View.Filter = item =>
                    {
                        if (item is MultronWinCleaner.Processes.Clean.LockedProcessViewModel proc)
                        {
                            return proc.FilePathWithoutSize?.ToLower().Contains(search) == true ||
                                   proc.GroupName?.ToLower().Contains(search) == true ||
                                   proc.DisplayName?.ToLower().Contains(search) == true;
                        }
                        return false;
                    };
                }

                groupedProcesses.View.Refresh();
                listBoxProcesses.ItemsSource = groupedProcesses.View;
            });
        }
        private bool _isLoadingMore = false;

        private async void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 10)
            {
                if (!_isLoadingMore)
                {
                    _isLoadingMore = true;
                    await LoadMoreItemsAsync();
                    _isLoadingMore = false;
                }
            }
        }

        private async void SelectAll_wpanel_Click(object sender, RoutedEventArgs e)
        {
            var dialogResult = await ShowCustomDialogAsync(
      "Warning",
      "Selecting all items across all groups may cause critical like browser history & downloads and recent files to be permanently deleted during cleanup.\n\nAre you sure you want to select everything?",
      CustomDialogIcon.Warning,
      CustomDialogButtons.YesNo);

            if (dialogResult != CustomDialogResult.Yes)
            {
                return;
            }
            int i = 0;
            foreach (CheckBox box in checkboxes2)
            {

                box.IsChecked = true;
                i++;
            }

        }


        private void UnselectAll_wpanel_Click(object sender, RoutedEventArgs e)
        {
       
            foreach (CheckBox box in checkboxes2)
            {

                box.IsChecked = false;
            }

        }
        private void SelectAll_AllGroups_Click(object sender, RoutedEventArgs e)
        {
           
       

            foreach (var item in dataGridGroups.Items)
            {
                if (item is GroupViewModel group)
                {
                    foreach (var file in group.allFiles)
                    {
                        file.IsChecked = true;
                    }

                    var sp = stackpanels.FirstOrDefault(s => s.DataContext == group);
                    if (sp != null)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is CheckBox cb)
                            {
                                cb.IsChecked = true;
                            }
                        }
                    }
                }
            }
        }
        #region Custom Dialog Infrastructure

        public enum CustomDialogIcon
        {
            Info,
            Warning,
            Error,
            Question
        }

        public enum CustomDialogButtons
        {
            Ok,
            YesNo
        }

        public enum CustomDialogResult
        {
            None,
            Ok,
            Yes,
            No
        }

        private TaskCompletionSource<CustomDialogResult> _dialogTcs;

        /// <summary>
        /// XAML tabanlı modern uyarı dialogunu gösterir.
        /// </summary>
        public Task<CustomDialogResult> ShowCustomDialogAsync(
            string title,
            string message,
            CustomDialogIcon icon = CustomDialogIcon.Info,
            CustomDialogButtons buttons = CustomDialogButtons.Ok)
        {
            _dialogTcs = new TaskCompletionSource<CustomDialogResult>();

            Dispatcher.Invoke(() =>
            {
                DialogTitleText.Text = title;
                DialogMessageText.Text = message;
                 
                switch (icon)
                {
                    case CustomDialogIcon.Info:
                        DialogIconText.Text = "ℹ️";
                        DialogIconBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#200078D4"));
                        break;
                    case CustomDialogIcon.Warning:
                        DialogIconText.Text = "⚠️";
                        DialogIconBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#20FFB900"));
                        break;
                    case CustomDialogIcon.Error:
                        DialogIconText.Text = "❌";
                        DialogIconBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#20E81123"));
                        break;
                    case CustomDialogIcon.Question:
                        DialogIconText.Text = "❓";
                        DialogIconBorder.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#200078D4"));
                        break;
                }

                if (buttons == CustomDialogButtons.Ok)
                {
                    DialogOkButton.Visibility = Visibility.Visible;
                    DialogYesButton.Visibility = Visibility.Collapsed;
                    DialogNoButton.Visibility = Visibility.Collapsed;
                }
                else if (buttons == CustomDialogButtons.YesNo)
                {
                    DialogOkButton.Visibility = Visibility.Collapsed;
                    DialogYesButton.Visibility = Visibility.Visible;
                    DialogNoButton.Visibility = Visibility.Visible;
                }

                CustomDialogOverlay.Visibility = Visibility.Visible;
            });

            return _dialogTcs.Task;
        }

        private void DialogOkButton_Click(object sender, RoutedEventArgs e)
        {
            CustomDialogOverlay.Visibility = Visibility.Collapsed;
            _dialogTcs?.TrySetResult(CustomDialogResult.Ok);
        }

        private void DialogYesButton_Click(object sender, RoutedEventArgs e)
        {
            CustomDialogOverlay.Visibility = Visibility.Collapsed;
            _dialogTcs?.TrySetResult(CustomDialogResult.Yes);
        }

        private void DialogNoButton_Click(object sender, RoutedEventArgs e)
        {
            CustomDialogOverlay.Visibility = Visibility.Collapsed;
            _dialogTcs?.TrySetResult(CustomDialogResult.No);
        }

        #endregion

        private void UnselectAll_AllGroups_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in dataGridGroups.Items)
            {
                if (item is GroupViewModel group)
                {
                    foreach (var file in group.allFiles)
                    {
                        file.IsChecked = false;
                    }

                    var sp = stackpanels.FirstOrDefault(s => s.DataContext == group);
                    if (sp != null)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is CheckBox cb)
                            {
                                cb.IsChecked = false;
                            }
                        }
                    }
                }
            }
        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is GroupViewModel groupVm)
                {
                    foreach (var file in groupVm.allFiles.Concat(groupVm.Files.Except(groupVm.Files)))
                    {
                        file.IsChecked = true;
                    }
                }
                else if (button.Tag is PathGroup group)
                {
                    foreach (var file in group.Files)
                    {
                        file.IsChecked = true;
                    }
                }
            }
        }

        private void ResetSelections_Click(object sender, RoutedEventArgs e)
        {
            if (!ReloadDb.IsEnabled || !buttonStartScan.IsEnabled)
            {
                MessageBox.Show("Please wait until the current scan or clean has finished.", "Reset Selections", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var answer = MessageBox.Show("Reset all your selections to the database defaults?\n\nThis deletes selections.txt, including your cleanmgr, Dism.exe, SFC and Deep Log Files Scan choices.",
                "Reset Selections", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes)
                return;

            selectionsAutoSaveTimer?.Stop();
            lock (pendingSelections)
            {
                pendingSelections.Clear();
            }
            lock (selectionsFileLock)
            {
                try
                {
                    if (System.IO.File.Exists(MultronWinCleaner.Processes.Load.SelectionsPath))
                        System.IO.File.Delete(MultronWinCleaner.Processes.Load.SelectionsPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not reset the selections:\n" + ex.Message, "Reset Selections", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            ReloadDatabase_Click(ReloadDb, new RoutedEventArgs());
        }

        private async void ReloadDatabase_Click(object sender, RoutedEventArgs e)
        {
            ReloadDb.IsEnabled = false;
            selectionsAutoSaveTimer?.Stop();
            await Task.Run(FlushPendingSelections);
            wrapPanel1.Children.Clear();
            wrapPanelDirectories.Children.Clear();
            ScrollViewerDirectories.Content = null;
            checkboxes2.Clear();
            listboxes.Clear();
            expanders.Clear();
            stackpanels.Clear();
            comboboxlist.Clear();
            databaseDefaults.Clear();
            database.Clear();
          
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {

                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;

                var load = new MultronWinCleaner.Processes.Load(this);
                await Task.Run(() => load.RunAsync());
                ReloadDb.IsEnabled = true;



            }
            else
            {
                MessageBox.Show("Any database file not found, Program closing...", "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
        public async Task savetodatabase(bool showMessage = true)
        {
            Dictionary<string, bool> checkBoxStates = null;


            await Dispatcher.InvokeAsync(() =>
            {
                checkBoxStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (CheckBox box in checkboxes2)
                {
                    string path = stringtokenizer(box.Content?.ToString() ?? "", "=", 1);
                    if (path != null) checkBoxStates[path] = box.IsChecked == true;
                }
            });


            await Task.Run(() =>
            {
                lock (selectionsFileLock)
                {
                    Dictionary<string, bool> selections = MultronWinCleaner.Processes.Load.ReadSelections();

                    foreach (var kvp in checkBoxStates)
                    {
                        if (!databaseDefaults.TryGetValue(kvp.Key, out bool databaseDefault)) continue;

                        if (kvp.Value == databaseDefault) selections.Remove(kvp.Key);
                        else selections[kvp.Key] = kvp.Value;
                    }

                    MultronWinCleaner.Processes.Load.WriteSelections(selections);
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                if (showMessage) MessageBox.Show("Selections saved successfully.");
                SaveSettings.IsEnabled = true;
                SaveSettings.Content = "Save Selections";
            });
        }
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings.IsEnabled = false;
            SaveSettings.Content = "Saving...";
            await savetodatabase();
        }

        private async void ApplySelectionsToDatabase_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult answer = MessageBox.Show(
                "This writes your saved selections into database.txt.\n\n" +
                "Selections written into database.txt are lost when a newer database is downloaded. " +
                "Selections kept in selections.txt are not.\n\n" +
                "Continue?",
                "Multron Windows Cleaner", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            ApplySelections.IsEnabled = false;
            try
            {
                SaveSettings.IsEnabled = false;
                await savetodatabase(showMessage: false);

                int applied = await Task.Run(() =>
                {
                    Dictionary<string, bool> selections = MultronWinCleaner.Processes.Load.ReadSelections();
                    if (selections.Count == 0) return 0;

                    string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "database.txt");
                    string[] lines = System.IO.File.ReadAllLines(filePath);
                    var appliedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    string profileRoot = null;
                    List<string> profileFolders = null;

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (line.StartsWith("{") || line.StartsWith("}"))
                        {
                            profileRoot = null;
                            profileFolders = null;
                            continue;
                        }
                        if (line.StartsWith("#profile#="))
                        {
                            profileRoot = (stringtokenizer(line, "=", 1) ?? "").Replace("{##}", Environment.UserName);
                            profileFolders = Directory.Exists(profileRoot)
                                ? Directory.GetDirectories(profileRoot).Select(System.IO.Path.GetFileName).ToList()
                                : new List<string>();
                            continue;
                        }

                        string path = null;
                        if (line.Contains("#profileget#"))
                        {
                            if (profileRoot == null) continue;
                            string subPath = stringtokenizer(line, "=", 2);
                            path = profileFolders.Select(folder => profileRoot + folder + subPath).FirstOrDefault(selections.ContainsKey);
                        }
                        else
                        {
                            string linePath = stringtokenizer(line.Replace("{##}", Environment.UserName), "=", 1);
                            if (linePath != null && selections.ContainsKey(linePath)) path = linePath;
                        }
                        if (path == null) continue;

                        lines[i] = selections[path] ? line.Replace("=false", "=true") : line.Replace("=true", "=false");
                        appliedPaths.Add(path);
                    }

                    string tempPath = filePath + ".tmp";
                    System.IO.File.WriteAllLines(tempPath, lines);
                    System.IO.File.Move(tempPath, filePath, overwrite: true);

                    foreach (string path in appliedPaths) selections.Remove(path);
                    MultronWinCleaner.Processes.Load.WriteSelections(selections);
                    return appliedPaths.Count;
                });

                MessageBox.Show(applied + " selections were written to database.txt.", "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
                if (applied > 0) ReloadDatabase_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not write the selections to database.txt: " + ex.Message, "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ApplySelections.IsEnabled = true;
            }
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is GroupViewModel groupVm)
                {
                    foreach (var file in groupVm.allFiles.Concat(groupVm.Files.Except(groupVm.Files)))
                    {
                        file.IsChecked = false;
                    }
                }
                else if (button.Tag is PathGroup group)
                {
                    foreach (var file in group.Files)
                    {
                        file.IsChecked = false;
                    }
                }
            }
        }
        private async Task LoadMoreItemsAsync()
        {

            var toAdd = _allLockedFileGroups.Skip(_itemsLoaded).Take(_pageSize).ToList();

            if (toAdd.Count == 0)
                return;

            await this.Dispatcher.InvokeAsync(() =>
            {
                foreach (var proc in toAdd)
                    LockedProcesses.Add(proc);
            });

            _itemsLoaded += toAdd.Count;
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
 
                 base.OnClosing(e);
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

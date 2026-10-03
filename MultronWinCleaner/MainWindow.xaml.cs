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

                if (scanEnabled)
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
            else
            {
                string file = stringtokenizer(content, "=", 1);
                settings.removeexception(file);
            }
        }
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)

        {


            CheckBox checkBox = sender as CheckBox;

            string content = checkBox.Content.ToString();

            if (content.Contains("Dism.exe") || content.StartsWith("cleanmgr.exe") || content.Contains("Deep Log Files Scan"))

            {
                        database.Remove(content);

            }

            else

            {

                string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);

                settings.addexception(file);

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

                        string error = null;
                        long fileSize = 0;
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                FileInfo info = new FileInfo(path);
                                if (!info.Exists)
                                    break;
                                fileSize = info.Length;
                                if (info.IsReadOnly)
                                    info.IsReadOnly = false;
                                info.Delete();
                                error = null;
                                break;
                            }
                            catch (Exception ex)
                            {
                                error = ex.Message;
                                await Task.Delay(300);
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
                        await AddKillMessage($"File no longer exists: {path}", new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#0078d7")));
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
                    main.paths.Clear();
                });
            }

            private async Task AddKillMessage(string text, System.Windows.Media.Brush brush)
            {
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
                cancelstatus.Dispose();
                dismcancel.Dispose();
                cancelstatus = new CancellationTokenSource();
                dismcancel = new CancellationTokenSource();



                scanstatus = 0;
                cancelclean = 0;
                MultronWinCleaner.Processes.Scan scan = new MultronWinCleaner.Processes.Scan(this);

                await Task.Run(() => scan.run());
                buttonStartScan.Content = "Cancel";

            }
            else if (buttonStartScan.Content.Equals("Cancel"))
            {
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
        private async void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
            
            bool allUnchecked = checkboxes2.All(cb => cb.IsChecked != true);

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

using Microsoft.Win32;
using Multron_Win_Cleaner;
using MultronWinCleaner;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MultronWinCleaner
{
    public partial class Utilities : System.Windows.Window
    {
        List<INetFwRule> invalidrules = new List<INetFwRule>();
        public LargeFileFinder largefilefinder;
        public MainWindow window;
        public MemCleaner memcleaner;
        public StartupManager startupmanager;
        public Duplicate_File_Finder Duplicate_File_Finder;
        public Configure configure;

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr procHandle, int min, int max);

        public bool isSettingsLoaded = false;

        public Utilities(MainWindow window)
        {
            InitializeComponent();
            this.window = window;
            memcleaner = new MemCleaner(window);
            configure = new Configure(this);
            startupmanager = new StartupManager(this);
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
         

            LoadSettings();
            StartStatusTask();

            if (chkTrayIconUtil.IsChecked == true)
                SetupTrayIcon();

            if (memcleaner.chkStartWithWinCleaner.IsChecked == true)
            {
                memcleaner.Show();
                memcleaner.Hide();
            }
            startupmanager.Show();
            startupmanager.Hide();
        }

 

        public void StartStatusTask()
        {
        

            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            try
                            {
                                bool isLargeFile = IsToolRunning(largefilefinder);
                                lblStatusLargeFile.Visibility = isLargeFile ? Visibility.Visible : Visibility.Collapsed;
                                btnCloseLargeFile.Visibility = isLargeFile ? Visibility.Visible : Visibility.Collapsed;

                                bool isDuplicate = IsToolRunning(Duplicate_File_Finder);
                                lblStatusDuplicate.Visibility = isDuplicate ? Visibility.Visible : Visibility.Collapsed;
                                btnCloseDuplicate.Visibility = isDuplicate ? Visibility.Visible : Visibility.Collapsed;

                                bool isMem = IsToolRunning(memcleaner);
                                lblStatusMemCleaner.Visibility = isMem ? Visibility.Visible : Visibility.Collapsed;
                                btnCloseMemCleaner.Visibility = isMem ? Visibility.Visible : Visibility.Collapsed;

                                bool isStartup = IsToolRunning(startupmanager);
                                lblStatusStartup.Visibility = isStartup ? Visibility.Visible : Visibility.Collapsed;
                                btnCloseStartup.Visibility = isStartup ? Visibility.Visible : Visibility.Collapsed;


                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"UI Update Error: {ex.Message}");
                            }
                        });

                        await Task.Delay(500);
                    }
                }
                catch (TaskCanceledException)
                {
                    System.Diagnostics.Debug.WriteLine("Status Task was canceled.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Status Task Critical Error: {ex.Message}");
                }
            });
        }

        private bool IsToolRunning(System.Windows.Window toolWindow)
        {
            if (toolWindow == null)
                return false;
            return  toolWindow.IsLoaded && toolWindow.IsVisible || toolWindow.Visibility == Visibility.Collapsed;
        }

        private void CloseLargeFile_Click(object sender, RoutedEventArgs e)
        {
            largefilefinder.Close();
            largefilefinder = null;

        }

        private void CloseDuplicate_Click(object sender, RoutedEventArgs e)
        {
            Duplicate_File_Finder.Close();
            Duplicate_File_Finder = null;

        }

        private void CloseMemCleaner_Click(object sender, RoutedEventArgs e)
        {
            if(memcleaner.memorymon != null) 
               memcleaner.memorymon.Close();
            memcleaner.Close();
            memcleaner = null;


        }

        private void CloseStartup_Click(object sender, RoutedEventArgs e)
        {
            startupmanager.Close();
            startupmanager = null;
        
        }

        private System.Windows.Forms.NotifyIcon trayIcon;

        private void SetupTrayIcon()
        {
            if (trayIcon == null)
            {
                trayIcon = new System.Windows.Forms.NotifyIcon();

                try
                {
                    Uri iconUri = new Uri("pack://application:,,,/Assets/mwc_utilities.ico", UriKind.Absolute);
                    var streamInfo = Application.GetResourceStream(iconUri);
                    if (streamInfo != null)
                    {
                        trayIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                    }
                    else
                    {
                        trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }
                }
                catch
                {
                    trayIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location);
                }

                trayIcon.Text = "Multron Win Cleaner Utilities";

                trayIcon.DoubleClick += (s, e) =>
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };

                var contextMenu = new System.Windows.Forms.ContextMenuStrip();
                contextMenu.Items.Add("Show Utilities", null, (s, e) => { this.Show(); this.WindowState = WindowState.Normal; });
                contextMenu.Items.Add("Hide Utilities", null, (s, e) => { this.Hide(); });

                trayIcon.ContextMenuStrip = contextMenu;
                trayIcon.Visible = true;
            }
        }

        private void LoadSettings()
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Settings.txt");
                if (System.IO.File.Exists(filePath))
                {
                    string[] lines = System.IO.File.ReadAllLines(filePath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("utilitiestrayicon:", StringComparison.OrdinalIgnoreCase))
                        {
                            chkTrayIconUtil.IsChecked = line.Split(':')[1] == "1";
                        }
                        else if (line.StartsWith("turboboost:", StringComparison.OrdinalIgnoreCase))
                        {
                            turboBoostActive = line.Split(':')[1] == "1";
                        }
                    }
                }
            }
            catch { }
            finally
            {
                isSettingsLoaded = true;
            }
        }

        private void chkTrayIconUtil_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (chkTrayIconUtil.IsChecked == true)
            {
                SetupTrayIcon();
            }
            else if (trayIcon != null)
            {
                trayIcon.Visible = false;
            }
            savesettings($"utilitiestrayicon:{(chkTrayIconUtil.IsChecked == true ? "1" : "0")}");
        }

        private void FirewallReset_Click(object sender, RoutedEventArgs e)
        {
            FirewallInvalidRulesList.Items.Clear();
            FirewallInvalidRulesList.Visibility = Visibility.Collapsed;
            FirewallScanResultLabel.Content = "";
            FirewallScanResultLabel.Visibility = Visibility.Collapsed;
            FirewallProgress.Value = 0;
            FirewallProgress.Visibility = Visibility.Collapsed;
            FirewallScan.Content = "Start Scan";
            FirewallScan.IsEnabled = true;
            FirewallReset.Visibility = Visibility.Collapsed;
        }

        private void FirewallClean_Click(object sender, RoutedEventArgs e)
        {
            if (FirewallScan.Content.Equals("Start Scan") || FirewallScan.Content.Equals("ReScan"))
            {
                FirewallScan.Content = "Clean";
                FirewallScan.IsEnabled = false;
                scanforrules();
                FirewallReset.Visibility = Visibility.Visible;
            }
            else
            {
                FirewallScan.IsEnabled = false;
                deleterules();
                FirewallReset.Visibility = Visibility.Visible;
            }
        }

        public async void scanforrules()
        {
            FirewallProgress.Visibility = Visibility.Visible;
            FirewallProgress.Value = 0;
            FirewallInvalidRulesList.Items.Clear();

            await Task.Run(() =>
            {
                var rules = GetFirewallRules();
                int count = rules.Count;
                int index = 0;

                foreach (var rule in rules)
                {
                    index++;
                    Dispatcher.Invoke(() =>
                    {
                        FirewallProgress.Value = ((double)index / count) * 100;
                    });

                    string path = rule.ApplicationName;
                    if (!string.IsNullOrEmpty(path) && (path.StartsWith(@"C:\") || path.StartsWith(@"\\") || path.StartsWith("/")))
                    {
                        try
                        {
                            string expanded = Environment.ExpandEnvironmentVariables(path);
                            if (!System.IO.File.Exists(expanded))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    invalidrules.Add(rule);
                                    FirewallInvalidRulesList.Items.Add($"Invalid Path: {path}");
                                });
                            }
                        }
                        catch { }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    if (FirewallInvalidRulesList.Items.Count > 0)
                    {
                        FirewallInvalidRulesList.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Content = $"{invalidrules.Count} invalid firewall rule(s) found.";
                        FirewallScanResultLabel.Foreground = Brushes.OrangeRed;
                        FirewallScan.IsEnabled = true;
                    }
                    else
                    {
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallProgress.Visibility = Visibility.Collapsed;
                        FirewallScanResultLabel.Content = "No invalid firewall rules found.";
                        FirewallScanResultLabel.Foreground = Brushes.LimeGreen;
                        FirewallScan.Content = "ReScan";
                        FirewallScan.IsEnabled = true;
                    }
                });
            });
        }

        private List<INetFwRule> GetFirewallRules()
        {
            var list = new List<INetFwRule>();
            try
            {
                Type type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                INetFwPolicy2 fwPolicy = (INetFwPolicy2)Activator.CreateInstance(type);
                foreach (INetFwRule rule in fwPolicy.Rules)
                {
                    list.Add(rule);
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Failed to read firewall rules: " + ex.Message);
                });
            }
            return list;
        }

        private async void deleterules()
        {
            FirewallProgress.Visibility = Visibility.Visible;
            FirewallProgress.Value = 0;
            FirewallInvalidRulesList.Items.Clear();

            await Task.Run(() =>
            {
                try
                {
                    Type netFwPolicy2Type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
                    INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(netFwPolicy2Type);

                    int total = invalidrules.Count;
                    int current = 0;

                    foreach (var rule in invalidrules)
                    {
                        firewallPolicy.Rules.Remove(rule.Name);
                        Dispatcher.Invoke(() =>
                        {
                            FirewallInvalidRulesList.Items.Add("deleted: " + rule.ApplicationName);
                            FirewallProgress.Value = ((double)++current / total) * 100;
                        });
                    }
                }
                catch { }
            });

            FirewallProgress.Visibility = Visibility.Collapsed;
            FirewallInvalidRulesList.Visibility = Visibility.Visible;
            FirewallScanResultLabel.Content = $"{FirewallInvalidRulesList.Items.Count} invalid firewall rule(s) deleted.";
            FirewallScanResultLabel.Foreground = Brushes.LimeGreen;
            FirewallScanResultLabel.Visibility = Visibility.Visible;
            FirewallScan.Content = "ReScan";
            FirewallScan.IsEnabled = true;
        }

        private void LargeFilesScan_Click(object sender, RoutedEventArgs e)
        {
            if (largefilefinder == null)
            {
                largefilefinder = new LargeFileFinder();
                window.settings.Close();
                window.settings = new Settings(memcleaner, window.utilities, window, startupmanager);
                window.settings.Show();
                window.settings.Hide();
          
            }
                
            
            
            largefilefinder.Show();
        }
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void DuplicateFilesScan_Click(object sender, RoutedEventArgs e) {
            
            if(Duplicate_File_Finder == null)
            {
                Duplicate_File_Finder = new Duplicate_File_Finder();
                window.settings.Close();
                window.settings = new Settings(memcleaner, window.utilities, window, startupmanager);
                window.settings.Show();
                window.settings.Hide();
        
            }
        
            
            Duplicate_File_Finder.Show();  
        
        }


        private void OpenMemoryCleaner_Click(object sender, RoutedEventArgs e) { 
            if(memcleaner == null)
            {
                memcleaner = new MemCleaner(window);
                window.settings.Close();
                window.settings = new Settings(memcleaner, window.utilities, window, startupmanager);
                window.settings.Show();
                window.settings.Hide();
            
            }
         
            
            memcleaner.Show();  
        
        }

        public bool turboBoostActive = false;
        public ObservableCollection<ServiceItem> turboBoostServices = new ObservableCollection<ServiceItem>
        { 
            new ServiceItem { ServiceName = "SysMain" }, 
            new ServiceItem { ServiceName = "WSearch" }, 
            new ServiceItem { ServiceName = "DiagTrack" }, 
            new ServiceItem { ServiceName = "dmwappushservice" }, 
            new ServiceItem { ServiceName = "WerSvc" }, 
            new ServiceItem { ServiceName = "PcaSvc" },
            new ServiceItem { ServiceName = "TrkWks" },  
             
            new ServiceItem { ServiceName = "XblGameSave" },
            new ServiceItem { ServiceName = "XblAuthManager" },
            new ServiceItem { ServiceName = "XboxNetApiSvc" },
            new ServiceItem { ServiceName = "XboxGipSvc" },
             
            new ServiceItem { ServiceName = "Spooler" },
            new ServiceItem { ServiceName = "PrintNotify" },
            new ServiceItem { ServiceName = "Fax" },
             
            new ServiceItem { ServiceName = "SSDPSRV" }, 
            new ServiceItem { ServiceName = "upnphost" },  
            new ServiceItem { ServiceName = "FDResPub" }, 
            new ServiceItem { ServiceName = "MapsBroker" },  
            new ServiceItem { ServiceName = "WMPNetworkSvc" }, 
             
            new ServiceItem { ServiceName = "TouchKeyboardAndHandwritingPanelService" }, 
            new ServiceItem { ServiceName = "RemoteRegistry" },  
            new ServiceItem { ServiceName = "RetailDemo" }, 
            new ServiceItem { ServiceName = "BluetoothSupport" } 
        };

        public void savesettings(string setting)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Settings.txt");
                List<string> lines = System.IO.File.Exists(filePath)
                    ? System.IO.File.ReadAllLines(filePath).ToList()
                    : new List<string>();

                if (setting.StartsWith("selectedservice:"))
                {
                    string[] parts = setting.Split(':');
                    if (parts.Length < 3) return;

                    string serviceName = parts[1];
                    string newValue = parts[2];
                    int index = lines.FindIndex(line =>
                        line.StartsWith("selectedservice:") &&
                        line.Split(':').Length >= 3 &&
                        line.Split(':')[1] == serviceName);

                    if (index >= 0)
                    {
                        lines[index] = $"selectedservice:{serviceName}:{newValue}";
                    }
                    else
                    {
                        lines.Add($"selectedservice:{serviceName}:{newValue}");
                    }
                }
                else
                {
                    string key = setting.Split(':')[0];
                    int index = lines.FindIndex(line => line.StartsWith(key + ":"));
                    if (index >= 0)
                        lines[index] = setting;
                    else
                        lines.Add(setting);
                }

                System.IO.File.WriteAllLines(filePath, lines);
            }
            catch
            {
            }
        }

        private void ConfigureTurboBoost_Click(object sender, RoutedEventArgs e)
        {
            configure.Show();
        }

        private async void ActivateTurboBoost_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            btn.IsEnabled = false;

            btn.Content = turboBoostActive ? "Deactivating..." : "Applying...";

            try
            {
                await Task.Run(async () =>
                {
                    if (!turboBoostActive)
                    {
                        foreach (var service in turboBoostServices)
                            StopStartService(service.ServiceName, false);

                        await Dispatcher.Invoke(async () => await memcleaner.cleanmemory());
                    }
                    else
                    {
                        foreach (var service in turboBoostServices)
                            StopStartService(service.ServiceName, true);
                    }
                });

                turboBoostActive = !turboBoostActive;

                btn.Content = turboBoostActive ? "Undo Optimization" : "Apply Optimization";

                MessageBox.Show(turboBoostActive ? "Optimization applied" : "Optimization Reversed");

                savesettings($"turboboost:{(turboBoostActive ? "1" : "0")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Optimization failed:\n" + ex.Message);
            }
            finally
            {
                btn.IsEnabled = true;
            }
        }

        private void StopStartService(string serviceName, bool start)
        {
            try
            {
                ServiceController sc = new ServiceController(serviceName);
                if (start)
                {
                    if (sc.Status != ServiceControllerStatus.Running && sc.Status != ServiceControllerStatus.StartPending)
                    {
                        sc.Start();
                        sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }
                else
                {
                    if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                    {
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Service {serviceName} error: {ex.Message}");
            }
        }

        private void StartupManager_Click(object sender, RoutedEventArgs e)
        {
            if(startupmanager == null)
            {
                startupmanager = new StartupManager(this);
                window.settings.Close();
                window.settings = new Settings(memcleaner, window.utilities, window, startupmanager);
                window.settings.Show();
                window.settings.Hide();
            }
            startupmanager.Show();
        }
    }
}
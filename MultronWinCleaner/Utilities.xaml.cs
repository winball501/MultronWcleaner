using Microsoft.Win32;
using Multron_Win_Cleaner;
using NetFwTypeLib;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MultronWinCleaner
{
    public partial class Utilities : Window
    {
        List<INetFwRule> invalidrules = new List<INetFwRule>();
        public LargeFileFinder largefilefinder = new LargeFileFinder();
        public MainWindow window;
        public MemCleaner memcleaner;
        public StartupManager startupmanager;
        public Duplicate_File_Finder Duplicate_File_Finder = new Duplicate_File_Finder();
        public Configure configure;
     
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr procHandle, int min, int max);

        public Utilities(MainWindow window)
        {
            InitializeComponent();
            memcleaner = new MemCleaner(window);
            this.window = window;
            configure = new Configure(this);
            startupmanager = new StartupManager();
        }

        private void FirewallClean_Click(object sender, RoutedEventArgs e)
        {
            if (FirewallScan.Content.Equals("Start Scan") || FirewallScan.Content.Equals("ReScan"))
            {
                FirewallScan.Content = "Clean";
                FirewallScan.IsEnabled = false;
                scanforrules();
            }
            else
            {
                FirewallScan.IsEnabled = false;
                deleterules();
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
                    FirewallScan.IsEnabled = true;
                    if (FirewallInvalidRulesList.Items.Count > 0)
                    {
                        FirewallInvalidRulesList.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Content = $"{invalidrules.Count} invalid firewall rule(s) found.";
                        FirewallScanResultLabel.Foreground = Brushes.OrangeRed;
                         
                    }
                    else
                    {
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallProgress.Visibility = Visibility.Collapsed;
                        FirewallScanResultLabel.Content = "No invalid firewall rules found.";
                        FirewallScanResultLabel.Foreground = Brushes.LimeGreen;
                        FirewallScan.Content = "ReScan";
                 
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
        
        }
  
        private void LargeFilesScan_Click(object sender, RoutedEventArgs e) => largefilefinder.Show();
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();
        private void DuplicateFilesScan_Click(object sender, RoutedEventArgs e) => Duplicate_File_Finder.Show();
        private void FileGuardianDownloadButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/drwellss/MultronFguardian", UseShellExecute = true });
        private void HydraDownloadButton_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/HydraDragonAntivirus/HydraDragonAntivirus/releases/tag/Beta2.2", UseShellExecute = true });
        private void OpenMemoryCleaner_Click(object sender, RoutedEventArgs e) => memcleaner.Show();

        public bool turboBoostActive = false;
        public List<string> turboBoostServices = new()
{
    "SysMain",                            
    "Fax",                              
    "BluetoothSupport",                 
    "Spooler",                       
    "MapsBroker",                      
    "PrintNotify",                     
    "XblGameSave",                    
    "WMPNetworkSvc",                   
    "TouchKeyboardAndHandwritingPanelService",  
    "RemoteRegistry",                  
    "DiagTrack",                      
    "RetailDemo",                    
    "WSearch",                      
    "dmwappushservice",           
    "WerSvc",                       
    "DeviceInstall",                
    "iphlpsvc",                   
    "RemoteAccess",              
    "TabletInputService",         
    "WpnService",                
    "Themes",                    
    "XblAuthManager"              
};

        public void savesettings(string setting)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Settings.txt");
                string key = setting.Split(':')[0];
                List<string> lines = System.IO.File.Exists(filePath) ? System.IO.File.ReadAllLines(filePath).ToList() : new();
                int index = lines.FindIndex(line => line.StartsWith(key + ":"));
                if (index >= 0) lines[index] = setting; else lines.Add(setting);
                System.IO.File.WriteAllLines(filePath, lines);
            }
            catch { }
        }
        private void ConfigureTurboBoost_Click(object sender, RoutedEventArgs e)
        {
            configure.Show();
        }
        private async void ActivateTurboBoost_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            btn.Content = turboBoostActive ? "Deactivating..." : "Boosting...";

            try
            {
                await Task.Run(async () =>
                {
                    if (!turboBoostActive)
                    {
                        foreach (var service in turboBoostServices)
                            StopStartService(service, false);
                        await Dispatcher.Invoke(async () => await memcleaner.cleanmemory());
                    }
                    else
                    {
                        foreach (var service in turboBoostServices)
                            StopStartService(service, true);
                    }
                });

                turboBoostActive = !turboBoostActive;
                btn.Content = turboBoostActive ? "Deactivate Turbo Boost" : "Activate Turbo Boost";
                MessageBox.Show(turboBoostActive ? "Turbo Boost activated!" : "Turbo Boost deactivated.");
                savesettings($"turboboost:{(turboBoostActive ? "1" : "0")}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Turbo Boost failed:\n" + ex.Message);
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
                ServiceController sc = new(serviceName);
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

        RegCleaner regcleaner = new RegCleaner();
        private void OpenRegistryCleaner_Click(object sender, RoutedEventArgs e) => regcleaner.Show();

        private void StartupManager_Click(object sender, RoutedEventArgs e)
        {
            startupmanager.Show();
        }
    }
}
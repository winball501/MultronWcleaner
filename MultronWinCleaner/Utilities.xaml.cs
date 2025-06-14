using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NetFwTypeLib;
namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for Utilities.xaml
    /// </summary>
    public partial class Utilities : Window
    {
        List<INetFwRule> invalidrules = new List<INetFwRule>();
        public LargeFileFinder largefilefinder = new LargeFileFinder();
        public Duplicate_File_Finder Duplicate_File_Finder = new Duplicate_File_Finder();
        public Utilities()
        {
            InitializeComponent();
        }
        public void windowsfirewallscanner()
        {
        

        }

        private void FirewallClean_Click(object sender, RoutedEventArgs e)
        {
            if(FirewallScan.Content.Equals("Start Scan"))
            {
                FirewallScan.Content = "Clean";
                FirewallScan.IsEnabled = false;
                scanforrules();
            } else
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
            await Task.Run(async () =>
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
                    catch
                    {
                    }
                }
            }
                await this.Dispatcher.InvokeAsync(() =>
                {
                    FirewallScan.IsEnabled = true;
                    if(FirewallInvalidRulesList.Items.Count > 0)
                    {
                        FirewallInvalidRulesList.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallScanResultLabel.Content = $"{invalidrules.Count} invalid firewall rule(s) found.";
                        FirewallScanResultLabel.Foreground = Brushes.OrangeRed;

                    } else
                    {
                        FirewallScanResultLabel.Visibility = Visibility.Visible;
                        FirewallProgress.Visibility = Visibility.Collapsed;
                        FirewallScanResultLabel.Content = "No invalid firewall rules found.";
                        FirewallScanResultLabel.Foreground = Brushes.LimeGreen;
                        FirewallScan.Visibility = Visibility.Collapsed;
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
                MessageBox.Show("Failed to read firewall rules: " + ex.Message);
            }

            return list;
        }
        private async void deleterules()
        {

            await Dispatcher.InvokeAsync(() =>
            {
                FirewallProgress.Visibility = Visibility.Visible;
                FirewallProgress.Value = 0;
                FirewallInvalidRulesList.Items.Clear();
            });
             


            await Task.Run(async() =>
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
                        await Dispatcher.InvokeAsync(() =>
                        {
                            FirewallInvalidRulesList.Items.Add("deleted: " + rule.ApplicationName);
                        });
                         

                        current++;
                       await Dispatcher.InvokeAsync(() =>
                        {
                            FirewallProgress.Value = ((double)current / total) * 100;
                        });
                    }
                }
                catch (Exception ex)
                {
                   
                }
            });

          
            await Dispatcher.InvokeAsync(() =>
            {
                FirewallProgress.Visibility = Visibility.Collapsed;

                FirewallInvalidRulesList.Visibility = Visibility.Visible;

                FirewallScanResultLabel.Content = $"{FirewallInvalidRulesList.Items.Count} invalid firewall rule(s) deleted.";
                FirewallScanResultLabel.Foreground = Brushes.LimeGreen;
                FirewallScanResultLabel.Visibility = Visibility.Visible;
            });
        }
        private void LargeFilesScan_Click(object sender, RoutedEventArgs e)
        {
            largefilefinder.Show();
        }
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

       
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void DuplicateFilesScan_Click(object sender, RoutedEventArgs e)
        {
            Duplicate_File_Finder.Show();
        }
    }
}

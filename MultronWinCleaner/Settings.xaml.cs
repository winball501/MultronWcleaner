using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Multron_Win_Cleaner;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using IWshRuntimeLibrary;
using System;
using System.Windows;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static Multron_Win_Cleaner.MainWindow;
namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
  
        public HashSet<string> excludedfiles = new HashSet<string>();
        string excludedfilesdir = Environment.CurrentDirectory + "\\excluded.txt";
        MemCleaner memcleaner;
        Utilities Utilities;
        MainWindow mainWindow;
        StartupManager manager;
        public Settings(MemCleaner memcleaner, Utilities utilities, MainWindow mainWindow, StartupManager manager)
        {
            InitializeComponent();
            this.memcleaner = memcleaner;
            Utilities = utilities;
            getusers();
            this.mainWindow = mainWindow;
            this.manager = manager;
        }


    

       

       

        public async Task getusers()
        {
            await Task.Run(async() =>
            {
                await this.Dispatcher.InvokeAsync(() =>
                {
                    comboBoxUserSelection.Items.Clear();

                    string currentUser = WindowsIdentity.GetCurrent().Name;
                    string currentUserShort = currentUser.Contains("\\") ? currentUser.Split('\\')[1] : currentUser;

                    using (PrincipalContext ctx = new PrincipalContext(ContextType.Machine))
                    {
                        UserPrincipal userPrincipal = new UserPrincipal(ctx);
                        PrincipalSearcher searcher = new PrincipalSearcher(userPrincipal);

                        foreach (var result in searcher.FindAll())
                        {
                            UserPrincipal user = result as UserPrincipal;
                            if (user != null && !string.IsNullOrEmpty(user.SamAccountName))
                            {
                                comboBoxUserSelection.Items.Add(user.SamAccountName);


                                if (user.SamAccountName.Equals(currentUserShort, StringComparison.OrdinalIgnoreCase))
                                {
                                    comboBoxUserSelection.SelectedItem = user.SamAccountName;
                                }
                            }
                        }
                    }

                });
            
            });
            
        }

        public class SettingsTasks
        {
            Settings settings;

            public SettingsTasks(Settings settings)
            {
                this.settings = settings;
            }

            public async void run()
            {
                while (true)
                {
                    await settings.Dispatcher.InvokeAsync(() =>
                    {
                        DateTime time = DateTime.Now;
                        
                        switch(settings.cmbScheduleType.SelectedIndex)
                        {
                            case 0:
                                settings.txtStat.Text = "Status: " + time + " Selected = Only Minutes";
                                break;

                            case 1:
                                settings.txtStat.Text = "Status: " + time + " Selected = Daily Default Interval Min 120 : " + settings.txtStartTime.Text + " " + settings.txtEndTime.Text;
                                break;

                            case 2:
                                settings.txtStat.Text = "Status: " + time + " Selected = Weekly Default Interval Min 120 : " + settings.txtStartTime.Text + " " + settings.txtEndTime.Text;
                                break;
                            case 3:
                                settings.txtStat.Text = "Status: " + time + " Selected = Custom";
                                break;

                        }
                    });

                    await Task.Delay(1000);
                }
            }
       
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsTasks settingsManager = new SettingsTasks(this);
            Thread t = new Thread(settingsManager.run);
            t.Start();
            string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "Settings.txt";
            if (!System.IO.File.Exists(excludedfilesdir))
                System.IO.File.Create(excludedfilesdir).Close();

            foreach (string line in System.IO.File.ReadLines(excludedfilesdir))
            {
                lstExceptions.Items.Add(line);
                excludedfiles.Add(line);
            }

            if (!System.IO.File.Exists(settingsPath))
            {
                System.IO.File.Create(settingsPath).Close();
                return;
            }

            var lines = System.IO.File.ReadAllLines(settingsPath);
            Dictionary<string, string> settings = lines
              .Where(l => l.Contains(":") && !l.StartsWith("selectedservice:", StringComparison.OrdinalIgnoreCase))
              .Select(l => l.Split(new[] { ':' }, 2))
              .ToDictionary(parts => parts[0].Trim().ToLower(), parts => parts[1].Trim());

            string Get(string key) => settings.TryGetValue(key.ToLower(), out string val) ? val : null;
            bool GetBool(string key) => Get(key) == "1";

          
        

           
            chkAutoClean.IsChecked = GetBool("autoclean");
            chkTrayIcon.IsChecked = GetBool("trayicon");
            OnlyLowCPU.IsChecked = GetBool("onlylowcpu");
            RunIfInactive.IsChecked = GetBool("runifactive");
            SkipBattery.IsChecked = GetBool("batterylow");
 
            txtStartTime.Text = Get("starttime") ?? "08:00";
            txtEndTime.Text = Get("endtime") ?? "22:00";
            txtCleaningInterval.Text = Get("minutes") ?? "0";
            if (settings.TryGetValue("mon", out string value))
            {
                memcleaner.chkSmartRAM.IsChecked = (value == "1");
            }
            if (settings.TryGetValue("automemclean", out string value2))
            {
                memcleaner.chkEnableAutoClean.IsChecked = (value2 == "1");
            }
            if (settings.TryGetValue("sendnotify", out string value3))
            {
                memcleaner.chkShowNotification.IsChecked = (value3 == "1");
            }
            if (settings.TryGetValue("skipiflow", out string value4))
            {
                memcleaner.chkSkipOnLowBattery.IsChecked = (value4 == "1");
            }
            if (settings.TryGetValue("customaccess", out string customaccess))
            {
                txtCustomAccess.Text = customaccess;
            }
            if (settings.TryGetValue("customday", out string customday))
            {
                 txtCustomDay.Text = customday;
            }
            if (settings.TryGetValue("access_scan", out string access_scan))
            {
                AccessScan.IsChecked = (access_scan == "1");
            }
           
            if (settings.TryGetValue("pluggedin", out string plug))
            {
                OnlyBattery.IsChecked = (plug == "1");
            }
            if (settings.TryGetValue("oldscan", out string oldscan))
            {
                OldScan.IsChecked = (oldscan == "1");
            }
            if (settings.TryGetValue("turboboost", out string value5))
            {
                if(value5 == "1")
                {
                    Utilities.Turbo.Content = "Apply Optimization";
                    Utilities.turboBoostActive = true;
                } else
                {
                    Utilities.Turbo.Content = "Undo Optimization";
                    Utilities.turboBoostActive = false;
                }
            }
            if (settings.TryGetValue("topmost", out string value6))
            {
                memcleaner.chkTopMostMonitor.IsChecked = (value6 == "1");
            }
            if (settings.TryGetValue("startupwarning", out string value7))
            {
                manager.tglShowNotifications.IsChecked = (value7 == "1");
            }

            string scanindex2 = Get("accessscanindex");
            if (!string.IsNullOrEmpty(scanindex2))
            {
                foreach (ComboBoxItem item in cmbAccessPreset.Items)
                {
                    if (item.Content.ToString().Equals(scanindex2, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbAccessPreset.SelectedItem = item;
                        break;
                    }
                }
            }

            string scanindex1 = Get("oldscanindex");
            if (!string.IsNullOrEmpty(scanindex1))
            {
                foreach (ComboBoxItem item in cmbAgePreset.Items)
                {
                    if (item.Content.ToString().Equals(scanindex1, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbAgePreset.SelectedItem = item;
                        break;
                    }
                }
            }

            string schedule = Get("scheduletype");
            if (!string.IsNullOrEmpty(schedule))
            {
                foreach (ComboBoxItem item in cmbScheduleType.Items)
                {
                    if (item.Content.ToString().Equals(schedule, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbScheduleType.SelectedItem = item;
                        break;
                    }
                }
            }

       
            string postAction = Get("postaction");
            if (!string.IsNullOrEmpty(postAction))
            {
                foreach (ComboBoxItem item in cmbPostCleanupAction.Items)
                {
                    if (item.Content.ToString().Equals(postAction, StringComparison.OrdinalIgnoreCase))
                    {
                        cmbPostCleanupAction.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void txtCleaningInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
       
        }
        private void txtCleaningInterval_TextChanged_1(object sender, TextChangedEventArgs e)
        {
 
        }
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
       
            SaveButton.IsEnabled = false;
            for(int c = 0; c < 32; c++)
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt";
                var lines = System.IO.File.Exists(path) ? System.IO.File.ReadAllLines(path).ToList() : new List<string>();

                void Upsert(string key, string value)
                {
                    int index = lines.FindIndex(l => l.StartsWith(key + ":"));
                    if (index != -1)
                        lines[index] = $"{key}:{value}";
                    else
                        lines.Add($"{key}:{value}");
                }
                System.IO.File.Delete(path);
                Upsert("customday", txtCustomDay.Text.Trim());
                Upsert("customaccess", txtCustomAccess.Text.Trim());
                Upsert("endtime", txtEndTime.Text.Trim());
                Upsert("minutes", txtCleaningInterval.Text.Trim());
                Upsert("autoclean", chkAutoClean.IsChecked == true ? "1" : "0");
                Upsert("trayicon", chkTrayIcon.IsChecked == true ? "1" : "0");
                Upsert("oldscan", OldScan.IsChecked == true ? "1" : "0");
                Upsert("access_scan", AccessScan.IsChecked == true ? "1" : "0");
                Upsert("onlylowcpu", OnlyLowCPU.IsChecked == true ? "1" : "0");
                Upsert("runifactive", RunIfInactive.IsChecked == true ? "1" : "0");
                Upsert("pluggedin", OnlyBattery.IsChecked == true ? "1" : "0");
                Upsert("batterylow", SkipBattery.IsChecked == true ? "1" : "0");
                if (cmbAccessPreset.SelectedItem is ComboBoxItem selectedaccess)
                    Upsert("accessscanindex", selectedaccess.Content.ToString());
                if (cmbAgePreset.SelectedItem is ComboBoxItem selectedindex)
                    Upsert("oldscanindex", selectedindex.Content.ToString());
                if (cmbScheduleType.SelectedItem is ComboBoxItem selectedSchedule)
                    Upsert("scheduletype", selectedSchedule.Content.ToString());

                if (cmbPostCleanupAction.SelectedItem is ComboBoxItem selectedAction)
                    Upsert("postaction", selectedAction.Content.ToString());

                for (int i = 1; i <= 7; i++)
                {
                    string controlName = $"Day{i}";
                    string keyName = $"day{i}";

                    if (this.FindName(controlName) is CheckBox dayBox)
                        Upsert(keyName, dayBox.IsChecked == true ? "1" : "0");
                }

                System.IO.File.WriteAllLines(path, lines);
            }
            SaveButton.IsEnabled = true;


            MessageBox.Show("Settings Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void txtCleaningInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
    
            e.Handled = !IsNumeric(e.Text);
        }
 
        private bool IsNumeric(string text)
        {
            int result;
            return int.TryParse(text, out result);
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void chkAutoClean_Checked(object sender, RoutedEventArgs e)
        {
        
        }

        public void addexception(string path)
        {
         
            if (!string.IsNullOrWhiteSpace(path) && !lstExceptions.Items.Contains(path))
            {
                lstExceptions.Items.Add(path);
                excludedfiles.Add(path);
                txtExceptionPath.Clear();
              
                using (StreamWriter writer = new StreamWriter(excludedfilesdir, append: true))
                {
                    writer.WriteLine(path);
                }
            }
        }
        public void removeexception(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            
            foreach (var item in lstExceptions.Items.Cast<string>().ToList())
            {
                if (item.Equals(path, StringComparison.OrdinalIgnoreCase))
                {
                    lstExceptions.Items.Remove(item);
                    break;
                }
            }

           
            excludedfiles.Remove(path);

           
            if (System.IO.File.Exists(excludedfilesdir))
            {
                var lines = System.IO.File.ReadAllLines(excludedfilesdir);
                var updatedLines = lines
                    .Where(line => !line.Equals(path, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                System.IO.File.WriteAllLines(excludedfilesdir, updatedLines);
            }
        }
        private void AddException_Click(object sender, RoutedEventArgs e)
        {
            if(System.IO.File.Exists(txtExceptionPath.Text))
            {
                addexception(txtExceptionPath.Text.Trim());
            } else
            {
                MessageBox.Show("Path is not correct or file not exists anymore");
            }
               
        }

        private void RemoveException_Click(object sender, RoutedEventArgs e)
        {
            removeexception(lstExceptions.SelectedItem.ToString());
        }

        private void RemoveExceptionAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (string selected in lstExceptions.Items.Cast<string>().ToList())
            {
                removeexception(selected);
            }
        }

        private void chkTrayIcon_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void chkRunOnStartup_Checked(object sender, RoutedEventArgs e)
        {
            
        }

        private void chkRunOnStartup_Unchecked(object sender, RoutedEventArgs e)
        {
            
        }

        private void allusersChecked(object sender, RoutedEventArgs e)
        {
            comboBoxUserSelection.IsEnabled = false;
         
        }
        private void allusersUnchecked(object sender, RoutedEventArgs e)
        {
            comboBoxUserSelection.IsEnabled = true;
          
        }

        private void comboBoxUserSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
           
              
            
           
        }

        private async void ApplyUserSelection_Click(object sender, RoutedEventArgs e)
        {
             
            mainWindow.database.Clear();
            mainWindow.wrapPanel1.Children.Clear();
            var load = new MultronWinCleaner.Processes.Load(mainWindow);
            await Task.Run(() => load.RunAsync());


        }
        public bool IsInStartup_Act()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "schtasks.exe";
            proc.StartInfo.Arguments = "/query /tn \"MultronWCleaner\"";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();
            proc.WaitForExit();

            return proc.ExitCode == 0;
        }
        public void RemoveFromStartup_Act()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "schtasks.exe";
            proc.StartInfo.Arguments = "/delete /tn \"MultronWCleaner\" /f";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
        }
        public void AddToStartup_Act()
        {
            string appPath = Process.GetCurrentProcess().MainModule.FileName;

            string command = $"/create /tn \"MultronWCleaner\" /tr \"\\\"{appPath}\\\"\" /sc onlogon /rl highest /f";

            Process proc = new Process();
            proc.StartInfo.FileName = "schtasks.exe";
            proc.StartInfo.Arguments = command;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            proc.WaitForExit();
        }
        private void AddToStartup_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInStartup_Act())
            {
                AddToStartup_Act();
                MessageBox.Show("Successfully added to startup!", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            } else
            {
                MessageBox.Show("It has already been added to startup.", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveFromStartup_Click(object sender, RoutedEventArgs e)
        {
            if (!IsInStartup_Act())
            {
                RemoveFromStartup_Act();
                MessageBox.Show("Successfully added to startup!", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("It is not already added to startup.", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OldScan_Checked(object sender, RoutedEventArgs e)
        {
            AccessScan.IsChecked = false;
        }
        private void btnAutoCleanHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Once you configure your settings, automatic cleaning will start if the checkmark above is enabled. However, if you close and reopen the software, it will reset. It needs to keep running continuously, this way it will no longer create files or registry in the system. I may think about improving this later, but for now this approach seemed better.", "Auto Clean Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void btnScannerHelp_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("These settings are intended for high-end systems. If you are using an older system, there is no need to modify these settings. You don't need to use in old systems because cache files cause slowdowns and fill up storage space on older systems. However, high-end systems can reduce power consumption and allow applications to open faster by using cache files and similar optimizations. The recommended setting is 30 days, but you can adjust it according to your own knowledge or situation.", "Scanner Help",  MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void AccessScan_Checked(object sender, RoutedEventArgs e)
        {
            OldScan.IsChecked = false;
        }
    }
}

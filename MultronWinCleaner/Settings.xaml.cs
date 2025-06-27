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


        private const string AppName = "MultronWinCleaner";

        public void SetStartupWithShortcut(bool enable)
        {
            string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string shortcutPath = System.IO.Path.Combine(startupFolderPath, "MultronWinCleaner.lnk");
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            if (enable)
            {
                try
                {
                    WshShell shell = new WshShell();
                    IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(exePath);
                    shortcut.WindowStyle = 1;
                    shortcut.Description = "Multron Win Cleaner Auto Start";
                    shortcut.Save();
                    MessageBox.Show("Startup shortcut created.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create startup shortcut: " + ex.Message);
                }
            }
            else
            {
                try
                {
                    if (System.IO.File.Exists(shortcutPath))
                    {
                        System.IO.File.Delete(shortcutPath);
                        MessageBox.Show("Startup shortcut removed.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to remove startup shortcut: " + ex.Message);
                }
            }
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

        public class SettingsManager
        {
            Settings settings;

            public SettingsManager(Settings settings)
            {
                this.settings = settings;
            }

            public async void run()
            {
                while (true)
                {
                    await settings.Dispatcher.InvokeAsync(() =>
                    {
                        bool autoClean = settings.chkAutoClean.IsChecked == true;

                        settings.cmbScheduleType.IsEnabled = autoClean;
                        settings.txtCleaningInterval.IsEnabled = autoClean;
                        settings.cmbPostCleanupAction.IsEnabled = autoClean;

                     
                        var selectedItem = settings.cmbScheduleType.SelectedItem as ComboBoxItem;
                        string selectedContent = selectedItem?.Content?.ToString() ?? "";

                       
                        bool enableDaysAndTime = autoClean && selectedContent != "Instant";
                        bool enableIntervalTime = autoClean && (selectedContent != "Daily" && selectedContent != "Weekly" && selectedContent != "Custom Days");
                        settings.txtStartTime.IsEnabled = enableDaysAndTime;
                        settings.txtEndTime.IsEnabled = enableDaysAndTime;
                        settings.txtCleaningInterval.IsEnabled = enableIntervalTime;
                        for (int i = 1; i <= 7; i++)
                        {
                            var checkBox = settings.FindName("Day" + i) as CheckBox;
                            if (checkBox != null)
                                checkBox.IsEnabled = enableDaysAndTime;
                        }

                       
                        if (selectedContent != "Custom Days")
                        {
                            for (int i = 1; i <= 7; i++)
                            {
                                var checkBox = settings.FindName("Day" + i) as CheckBox;
                                if (checkBox != null)
                                    checkBox.IsEnabled = false;
                            }
                        }

                        settings.OnlyLowCPU.IsEnabled = autoClean;
                        settings.RunIfInactive.IsEnabled = autoClean;
                        settings.SkipBatterry.IsEnabled = autoClean;
                    });

                    await Task.Delay(1000);
                }
            }
       
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SettingsManager settingsManager = new SettingsManager(this);
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
                .Where(l => l.Contains(":"))
                .Select(l => l.Split(new[] { ':' }, 2))
                .ToDictionary(parts => parts[0].Trim().ToLower(), parts => parts[1].Trim());

            string Get(string key) => settings.TryGetValue(key.ToLower(), out string val) ? val : null;
            bool GetBool(string key) => Get(key) == "1";

          
            for (int i = 1; i <= 7; i++)
            {
                string key = $"day{i}";
                if (this.FindName($"Day{i}") is CheckBox cb)
                {
                    cb.IsChecked = GetBool(key);
                }
            }

           
            chkAutoClean.IsChecked = GetBool("autoclean");
            chkTrayIcon.IsChecked = GetBool("trayicon");
            OnlyLowCPU.IsChecked = GetBool("onlylowcpu");
            RunIfInactive.IsChecked = GetBool("runifactive");
            SkipBatterry.IsChecked = GetBool("batterylow");
 
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
            
            if (settings.TryGetValue("turboboost", out string value5))
            {
                if(value5 == "1")
                {
                    Utilities.Turbo.Content = "Deactivate Turbo Boost";
                    Utilities.turboBoostActive = true;
                } else
                {
                    Utilities.Turbo.Content = "Activate Turbo Boost";
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
            Upsert("starttime", txtStartTime.Text.Trim());
            Upsert("endtime", txtEndTime.Text.Trim());
            Upsert("minutes", txtCleaningInterval.Text.Trim());
            Upsert("autoclean", chkAutoClean.IsChecked == true ? "1" : "0");
            Upsert("trayicon", chkTrayIcon.IsChecked == true ? "1" : "0");
            Upsert("onlylowcpu", OnlyLowCPU.IsChecked == true ? "1" : "0");
            Upsert("runifactive", RunIfInactive.IsChecked == true ? "1" : "0");
            Upsert("batterylow", SkipBatterry.IsChecked == true ? "1" : "0");
             
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
                using (StreamWriter writer = new StreamWriter(excludedfilesdir))
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

        private void ApplyUserSelection_Click(object sender, RoutedEventArgs e)
        {
             
                            mainWindow.database.Clear();
                            mainWindow.wrapPanel1.Children.Clear();
                            Load load = new Load(mainWindow);
                            Thread t2 = new Thread(new ThreadStart(load.run));
                            t2.Start();
                         
            
        }

        private void AddToStartup_Click(object sender, RoutedEventArgs e)
        {


            SetStartupWithShortcut(true);




        }

        private void RemoveFromStartup_Click(object sender, RoutedEventArgs e)
        {
            SetStartupWithShortcut(false);
        }
    }
}

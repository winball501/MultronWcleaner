using IWshRuntimeLibrary;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using Multron_Win_Cleaner;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    public partial class Settings : Window
    {
        public HashSet<string> excludedfiles = new HashSet<string>();
        public string logfilepath = "";
        string excludedfilesdir = Environment.CurrentDirectory + "\\excluded.txt";

        MemCleaner memcleaner;
        Utilities Utilities;
        MainWindow mainWindow;
        StartupManager manager;

        public Settings(MemCleaner memcleaner, Utilities utilities, MainWindow mainWindow, StartupManager manager)
        {
            InitializeComponent();
            this.memcleaner = memcleaner;
            this.Utilities = utilities;
            this.mainWindow = mainWindow;
            this.manager = manager;
             
            _ = getusers();
        }

        public void defaultloglocation()
        {
            if (logfilepath == "")
            {
                txtLogPath.Text = Environment.CurrentDirectory + "\\mwc_cleanlog.txt";
            }
        }
         
        public async Task getusers()
        {
            string currentUser = WindowsIdentity.GetCurrent().Name;
            string currentUserShort = currentUser.Contains("\\") ? currentUser.Split('\\')[1] : currentUser;

           
            List<string> foundUsers = new List<string>();
             
            await Task.Run(() =>
            {
                using (PrincipalContext ctx = new PrincipalContext(ContextType.Machine))
                {
                    UserPrincipal userPrincipal = new UserPrincipal(ctx);
                    using (PrincipalSearcher searcher = new PrincipalSearcher(userPrincipal))
                    {
                        foreach (var result in searcher.FindAll())
                        {
                            if (result is UserPrincipal user && !string.IsNullOrEmpty(user.SamAccountName))
                            {
                                foundUsers.Add(user.SamAccountName);
                            }
                        }
                    }
                }
            });
             
            comboBoxUserSelection.Items.Clear();
            foreach (var username in foundUsers)
            {
                comboBoxUserSelection.Items.Add(username);
                if (username.Equals(currentUserShort, StringComparison.OrdinalIgnoreCase))
                {
                    comboBoxUserSelection.SelectedItem = username;
                }
            }
        }

 
        private async Task StatusUpdateLoop()
        {
            while (true)
            {
                DateTime time = DateTime.Now;
                switch (cmbScheduleType.SelectedIndex)
                {
                    case 0: txtStat.Text = "Status: " + time + " Selected = Only Minutes"; break;
                    case 1: txtStat.Text = "Status: " + time + " Selected = Daily Default Interval Min 120 : " + txtStartTime.Text + " " + txtEndTime.Text; break;
                    case 2: txtStat.Text = "Status: " + time + " Selected = Weekly Default Interval Min 120 : " + txtStartTime.Text + " " + txtEndTime.Text; break;
                    case 3: txtStat.Text = "Status: " + time + " Selected = Custom"; break;
                }
                await Task.Delay(1000);
            }
        }

        public void LoadServiceSettings()
        {
            string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
            if (!System.IO.File.Exists(settingsPath)) return;

            var lines = System.IO.File.ReadAllLines(settingsPath);
            var settingsDict = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
             
             
            foreach (var line in lines)
            {
                if (line.StartsWith("selectedservice:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Split(':');
                    if (parts.Length >= 3)
                    {
                        string serviceName = parts[1].Trim();
                        bool isSelected = parts[2].Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
                        settingsDict[serviceName] = isSelected;
                    }
                }
            }
             
            foreach (var svc in Utilities.turboBoostServices)
            {
                if (settingsDict.TryGetValue(svc.ServiceName, out bool isSelected))
                {
                    svc.IsSelected = isSelected;
                }
                else
                {
                    svc.IsSelected = true;  
                }
            }
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadServiceSettings();
            _ = StatusUpdateLoop();

            if (!System.IO.File.Exists(excludedfilesdir))
            {
                System.IO.File.Create(excludedfilesdir).Close();
            }
            else
            {
                var excludeLines = await System.IO.File.ReadAllLinesAsync(excludedfilesdir);
                foreach (string line in excludeLines.Where(l => !string.IsNullOrWhiteSpace(l)))
                {
                    lstExceptions.Items.Add(line);
                    excludedfiles.Add(line);
                }
            }

            string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (!System.IO.File.Exists(settingsPath))
            {
                System.IO.File.Create(settingsPath).Close();
            }
            else
            {
                var lines = await System.IO.File.ReadAllLinesAsync(settingsPath);
                settings = lines
                    .Where(l => l.Contains(":") && !l.StartsWith("selectedservice:", StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.Split(new[] { ':' }, 2))
                    .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(), StringComparer.OrdinalIgnoreCase);
            }

            string GetString(string key, string fallback = "") => settings.TryGetValue(key, out string val) ? val : fallback;
            bool GetBool(string key) => GetString(key) == "1";

            void SetComboBoxSelection(ComboBox comboBox, string value)
            {
                if (string.IsNullOrEmpty(value)) return;
                foreach (ComboBoxItem item in comboBox.Items)
                {
                    if (item.Content.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                    {
                        comboBox.SelectedItem = item;
                        break;
                    }
                }
            }
       

            chkAutoClean.IsChecked = GetBool("autoclean");
            chkTrayIcon.IsChecked = GetBool("trayicon");
            OnlyLowCPU.IsChecked = GetBool("onlylowcpu");
            RunIfInactive.IsChecked = GetBool("runifactive");
            SkipBattery.IsChecked = GetBool("batterylow");
            chkEnableStartupScan.IsChecked = GetBool("startupscan");
            chkEnableStartupClean.IsChecked = GetBool("startupclean");
            chkEnableNotifyScan.IsChecked = GetBool("startupnotifyscan");
            chkEnableNotifyClean.IsChecked = GetBool("startupnotifyclean");

            AccessScan.IsChecked = GetBool("access_scan");
            OnlyBattery.IsChecked = GetBool("pluggedin");
            chkEnableLog.IsChecked = GetBool("enablelog");
            chkShowLastLog.IsChecked = GetBool("showlastlog");
            OldScan.IsChecked = GetBool("oldscan");
             if(memcleaner != null)
            {
                memcleaner.chkEnableAutoClean.IsChecked = GetBool("automemclean");
                memcleaner.chkSmartRAM.IsChecked = GetBool("mon");
                memcleaner.chkShowNotification.IsChecked = GetBool("sendnotify");
                memcleaner.chkSkipOnLowBattery.IsChecked = GetBool("skipiflow");
                memcleaner.chkTopMostMonitor.IsChecked = GetBool("topmost");

                memcleaner.chkClearTemp.IsChecked = GetBool("cleartemp");
                memcleaner.chkClearClipboard.IsChecked = GetBool("clearclip");
                memcleaner.chkStartWithWinCleaner.IsChecked = GetBool("startwithwincleaner");

                memcleaner.rbDeepClean.IsChecked = GetBool("deepclean");
                memcleaner.rbDeepCleanAggresive.IsChecked = GetBool("aggdeepclean");
                if (memcleaner.rbDeepClean.IsChecked == true)
                {
                    memcleaner.rbDeepCleanAggresive.IsChecked = false;
                }
                else if (memcleaner.rbDeepCleanAggresive.IsChecked == true)
                {
                    memcleaner.rbDeepClean.IsChecked = false;
                }
                else if (memcleaner.rbDeepClean.IsChecked == false && memcleaner.rbDeepCleanAggresive.IsChecked == false)
                {
                    memcleaner.rbQuickClean.IsChecked = true;
                }
            }
          


            if(manager != null)
               manager.tglShowNotifications.IsChecked = GetBool("startupwarning");

            txtStartTime.Text = GetString("starttime", "08:00");
            txtEndTime.Text = GetString("endtime", "22:00");
            txtCleaningInterval.Text = GetString("minutes", "0");
            txtCustomAccess.Text = GetString("customaccess");
            txtCustomDay.Text = GetString("customday");


          
            string logPath = GetString("logpath");
            if (!string.IsNullOrEmpty(logPath))
            {
                txtLogPath.Text = logPath;
                logfilepath = logPath;
            }
            else
            {
                defaultloglocation();
            }
 
            SetComboBoxSelection(memcleaner.cbCleanInterval, GetString("mcminutes"));
            SetComboBoxSelection(cmbAccessPreset, GetString("accessscanindex"));
            SetComboBoxSelection(cmbAgePreset, GetString("oldscanindex"));
            SetComboBoxSelection(cmbScheduleType, GetString("scheduletype"));
            SetComboBoxSelection(cmbPostCleanupAction, GetString("postaction"));
        }

        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt";
                var lines = System.IO.File.Exists(path) ? (await System.IO.File.ReadAllLinesAsync(path)).ToList() : new List<string>();

                void Upsert(string key, string value)
                {
                    int index = lines.FindIndex(l => l.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase));
                    if (index != -1)
                        lines[index] = $"{key}:{value}";
                    else
                        lines.Add($"{key}:{value}");
                }

                Upsert("customday", txtCustomDay.Text.Trim());
                Upsert("customaccess", txtCustomAccess.Text.Trim());
                Upsert("endtime", txtEndTime.Text.Trim());
                Upsert("minutes", txtCleaningInterval.Text.Trim());
                Upsert("loglocation", txtLogPath.Text.Trim());
                Upsert("autoclean", chkAutoClean.IsChecked == true ? "1" : "0");
                Upsert("trayicon", chkTrayIcon.IsChecked == true ? "1" : "0");
                Upsert("oldscan", OldScan.IsChecked == true ? "1" : "0");
                Upsert("access_scan", AccessScan.IsChecked == true ? "1" : "0");
                Upsert("onlylowcpu", OnlyLowCPU.IsChecked == true ? "1" : "0");
                Upsert("runifactive", RunIfInactive.IsChecked == true ? "1" : "0");
                Upsert("pluggedin", OnlyBattery.IsChecked == true ? "1" : "0");
                Upsert("batterylow", SkipBattery.IsChecked == true ? "1" : "0");
                Upsert("enablelog", chkEnableLog.IsChecked == true ? "1" : "0");
                Upsert("showlastlog", chkShowLastLog.IsChecked == true ? "1" : "0");
                Upsert("logpath", logfilepath);

                Upsert("startupscan", chkEnableStartupScan.IsChecked == true ? "1" : "0");
                Upsert("startupclean", chkEnableStartupClean.IsChecked == true ? "1" : "0");
                Upsert("startupnotifyscan", chkEnableNotifyScan.IsChecked == true ? "1" : "0");
                Upsert("startupnotifyclean", chkEnableNotifyClean.IsChecked == true ? "1" : "0");

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

                await System.IO.File.WriteAllLinesAsync(path, lines);
                MessageBox.Show("Settings Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
          
        }



        private void txtCleaningInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsNumeric(e.Text);
        }

        private bool IsNumeric(string text)
        {
            return int.TryParse(text, out _);
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

        public async void addexception(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && !lstExceptions.Items.Contains(path))
            {
                lstExceptions.Items.Add(path);
                excludedfiles.Add(path);
                txtExceptionPath.Clear();

                await System.IO.File.AppendAllTextAsync(excludedfilesdir, path + Environment.NewLine);
            }
        }

        public async void removeexception(string path)
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
                var lines = await System.IO.File.ReadAllLinesAsync(excludedfilesdir);
                var updatedLines = lines
                    .Where(line => !line.Equals(path, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                await System.IO.File.WriteAllLinesAsync(excludedfilesdir, updatedLines);
            }
        }

        private void AddException_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(txtExceptionPath.Text))
            {
                addexception(txtExceptionPath.Text.Trim());
            }
            else
            {
                MessageBox.Show("Path is not correct or file not exists anymore");
            }
        }

        private void RemoveException_Click(object sender, RoutedEventArgs e)
        {
            if (lstExceptions.SelectedItem != null)
                removeexception(lstExceptions.SelectedItem.ToString());
        }

        private void RemoveExceptionAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (string selected in lstExceptions.Items.Cast<string>().ToList())
            {
                removeexception(selected);
            }
        }

        private void allusersChecked(object sender, RoutedEventArgs e)
        {
            comboBoxUserSelection.IsEnabled = false;
        }

        private void allusersUnchecked(object sender, RoutedEventArgs e)
        {
            comboBoxUserSelection.IsEnabled = true;
        }

        private async void ApplyUserSelection_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.database.Clear();
            mainWindow.wrapPanel1.Children.Clear();
            var load = new MultronWinCleaner.Processes.Load(mainWindow);
            await Task.Run(() => load.RunAsync());
        }


        public async Task<bool> IsInStartup_ActAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/query /tn \"MultronWCleaner\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                  
                };

                using var proc = Process.Start(startInfo);
                if (proc == null) return false;

                
                await Task.Run(() => proc.WaitForExit());
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        public async Task<bool> RemoveFromStartup_ActAsync()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = "/delete /tn \"MultronWCleaner\" /f",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(startInfo);
                if (proc == null) return false;

                await Task.Run(() => proc.WaitForExit());
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        public async Task<bool> CreateStartupTaskAsync()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                var startInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                   
                    Arguments = $"/create /tn \"MultronWCleaner\" /tr \"\\\"{exePath}\\\" -startup\" /sc onlogon /rl highest /f",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(startInfo);
                if (proc == null) return false;

                await Task.Run(() => proc.WaitForExit());
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        private async void AddToStartup_Click(object sender, RoutedEventArgs e)
        {
            if (!await IsInStartup_ActAsync())
            {
                bool success = await CreateStartupTaskAsync();

                if (success)
                {
                    MessageBox.Show("Successfully added to startup!", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to add to startup. Please make sure to run the program as Administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("It has already been added to startup.", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void RemoveFromStartup_Click(object sender, RoutedEventArgs e)
        {
            if (await IsInStartup_ActAsync())
            {
                bool success = await RemoveFromStartup_ActAsync();

                if (success)
                {
                    MessageBox.Show("Successfully removed from startup!", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Failed to remove from startup. Please make sure to run the program as Administrator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The program is not currently in the startup list.", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show("These settings are intended for high-end systems. If you are using an older system, there is no need to modify these settings. You don't need to use in old systems because cache files cause slowdowns and fill up storage space on older systems. However, high-end systems can reduce power consumption and allow applications to open faster by using cache files and similar optimizations. The recommended setting is 30 days, but you can adjust it according to your own knowledge or situation.", "Scanner Help", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AccessScan_Checked(object sender, RoutedEventArgs e)
        {
            OldScan.IsChecked = false;
        }

        private void ResetExtensions_Click(object sender, RoutedEventArgs e)
        {
            txtNewExtension.Text = ".log.etl.dmp.trace.tmp.temp.bak.swp";
        }

        private void AddExtension_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.extensions == txtNewExtension.Text)
            {
                System.Windows.Forms.MessageBox.Show("No changed detected. " + txtNewExtension.Text + " > " + mainWindow.extensions);
                return;
            }

            string input = txtNewExtension.Text.Trim();

            if (input.Contains(" "))
            {
                MessageBox.Show("Extensions cannot contain spaces.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(input, @"^(\.[a-zA-Z0-9]+)+$"))
            {
                MessageBox.Show("Invalid format. Example: .log.etl.dmp.tmp", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show("Are you sure to add " + txtNewExtension.Text + "?", "Confirm", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                mainWindow.extensions = txtNewExtension.Text;
                System.Windows.MessageBox.Show("Changes applied. " + mainWindow.extensions + " > " + txtNewExtension.Text, "Information", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void OpenLogFile_Click(object sender, RoutedEventArgs e)
        {
            if (System.IO.File.Exists(logfilepath))
                Process.Start("explorer.exe", logfilepath);
        }

        private void OpenAppFolder_Click(object sender, RoutedEventArgs e)
        {
            string folderPath;

            if (!string.IsNullOrEmpty(logfilepath) && System.IO.File.Exists(logfilepath))
            {
                folderPath = System.IO.Path.GetDirectoryName(logfilepath);
            }
            else
            {
                folderPath = Environment.CurrentDirectory;
            }

            Process.Start("explorer.exe", folderPath);
        }

        private async void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await System.IO.File.WriteAllTextAsync(logfilepath, "");
                MessageBox.Show("Log file " + logfilepath + " all logs cleaned!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error clearing log: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        private void BrowseLogPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = "Select Folder For Log File.";

            dialog.DefaultDirectory = Environment.CurrentDirectory;
            dialog.InitialDirectory = Environment.CurrentDirectory;
            if (dialog.ShowDialog() == true)
            {
                txtLogPath.Text = dialog.FolderName;
            }
        }

        private async void ApplyLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logEntry = DateTime.Now + " Log path applied successfully!" + Environment.NewLine;

                if (!txtLogPath.Text.EndsWith(".txt"))
                {
                    string path = txtLogPath.Text + "\\mwc_cleanlog.txt";
                    await System.IO.File.AppendAllTextAsync(path, logEntry);
                    logfilepath = path;
                    txtLogPath.Text = path;
                }
                else
                {
                    await System.IO.File.AppendAllTextAsync(txtLogPath.Text, logEntry);
                    logfilepath = txtLogPath.Text;
                }

                MessageBox.Show("Log path applied successfully!", "Multron Win Cleaner", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error applying log path: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
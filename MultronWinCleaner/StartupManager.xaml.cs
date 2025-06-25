using IWshRuntimeLibrary;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MultronWinCleaner
{
    public partial class StartupManager : Window, INotifyPropertyChanged
    {
        public enum OperationType
        {
            Delete,
            Move,
            Rename
        }

        public class BootOperation
        {
            public OperationType Type { get; set; }
            public string SourcePath { get; set; } = "";
            public string? DestinationPath { get; set; } 
        }
      

      
        public ObservableCollection<StartupApp> StartupApps { get; set; } = new();

        private StartupApp? selectedApp;
        public StartupApp? SelectedApp
        {
            get => selectedApp;
            set
            {
                selectedApp = value;
                OnPropertyChanged(nameof(SelectedApp));
            
                btnRemove.IsEnabled = selectedApp != null;
            }
        }

        private readonly string[] StartupApprovedSubKeys = new string[]
        {
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32",
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
        };

        public StartupManager()
        {
            InitializeComponent();
            DataContext = this;
            LoadStartupApps();
          
        }

        private void LoadStartupApps()
        {
            StartupApps.Clear();

          
            ReadRegistryStartupApps(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run");
            ReadRegistryStartupApps(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run");
            ReadRegistryStartupApps(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run");
            AddBackupStartupApps(Registry.CurrentUser);
            AddBackupStartupApps(Registry.LocalMachine);
            AddBackupStartupApps(Registry.LocalMachine, isWow64: true);
        
            StartupAppsDataGrid.ItemsSource = StartupApps;
            CollectionViewSource.GetDefaultView(StartupApps).Refresh();
        }
        private List<BootOperation> _bootOperations = new();

        private void SaveBootOperations()
        {
            
            string file = Path.Combine(Environment.CurrentDirectory, "BootOperations.json");
            var json = System.Text.Json.JsonSerializer.Serialize(_bootOperations);
            System.IO.File.WriteAllText(file, json);
        }

        private void LoadBootOperations()
        {
            string file = Path.Combine(Environment.CurrentDirectory, "BootOperations.json");
            if (System.IO.File.Exists(file))
            {
                var json = System.IO.File.ReadAllText(file);
                _bootOperations = System.Text.Json.JsonSerializer.Deserialize<List<BootOperation>>(json) ?? new List<BootOperation>();
            }
        }
        private string GetHiveName(RegistryKey key)
        {
            if (key == Registry.CurrentUser) return "CurrentUser";
            if (key == Registry.LocalMachine) return "LocalMachine";
            return key.Name ?? "";
        }
        private void AddBackupStartupApps(RegistryKey rootKey, bool isWow64 = false)
        {
            string backupRegPath = @"Software\MyApp\StartupBackups";
            string runRegPath = isWow64
                ? @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
                : @"Software\Microsoft\Windows\CurrentVersion\Run";

            using var backupKey = rootKey.OpenSubKey(backupRegPath, writable: false);
            if (backupKey == null) return;

            foreach (string name in backupKey.GetValueNames())
            {
                bool alreadyExists = StartupApps.Any(x =>
                    x.ValueName == name &&
                    GetHiveName(x.RegistryRoot) == GetHiveName(rootKey) &&
                    x.RegistryPath == runRegPath);

                if (alreadyExists) continue;

                string? backupValue = backupKey.GetValue(name)?.ToString();
                if (string.IsNullOrEmpty(backupValue)) continue;

                var icon = GetIcon(backupValue);

                StartupApps.Add(new StartupApp
                {
                    RegistryRoot = rootKey,
                    RegistryPath = runRegPath,
                    ValueName = name,
                    Name = name,
                    Path = backupValue,
                    BackupData = backupValue,
                    IsEnabled = false,
                    Status = "Disabled (Backup)",
                    StartupType = rootKey == Registry.CurrentUser ? "Registry (User)" : "Registry (Machine)",
                    Icon = icon,
                    IsVisible = true
                });
            }
        }

        private void ReadRegistryStartupApps(RegistryKey rootKey, string subKey)
        {
            using RegistryKey? key = rootKey.OpenSubKey(subKey, writable: true);
            if (key == null) return;

            foreach (string name in key.GetValueNames())
            {
                string path = key.GetValue(name)?.ToString() ?? "";
                bool enabled = IsStartupApprovedEnabled(name, rootKey);
                var icon = GetIcon(path);

                StartupApps.Add(new StartupApp
                {
                    RegistryRoot = rootKey,
                    RegistryPath = subKey,
                    ValueName = name,
                    Name = name,
                    Path = path,
                    IsEnabled = enabled,
                    Status = enabled ? "Enabled" : "Disabled",
                    StartupType = rootKey == Registry.CurrentUser ? "Registry (User)" : "Registry (Machine)",
                    Icon = icon,
                     IsVisible = true
                });
            }
        }

        private bool IsStartupApprovedEnabled(string name, RegistryKey root)
        {
            foreach (var path in StartupApprovedSubKeys)
            {
                using var key = root.OpenSubKey(path, false);
                if (key == null) continue;

                if (key.GetValue(name) is byte[] data && data.Length > 0)
                {
                    if (data[0] == 0x03 || data[0] == 0x08)
                        return true;
                    else if (data[0] == 0x02)
                        return false;
                }
            }
            return true;
        }

        

        private void ReadStartupFolderShortcuts()
        {
            string[] startupFolders = new string[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };

            foreach (var folder in startupFolders)
            {
                if (!Directory.Exists(folder)) continue;
                var lnkFiles = Directory.GetFiles(folder, "*.lnk");
                foreach (var lnk in lnkFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(lnk);
                    var icon = GetIconFromShortcut(lnk);
                    bool enabled = IsStartupFolderShortcutEnabled(lnk);
                    StartupApps.Add(new StartupApp
                    {
                        RegistryRoot = null!,
                        RegistryPath = "",
                        ValueName = name,
                        Name = name,
                        Path = lnk,
                        IsEnabled = enabled,
                        Status = enabled ? "Enabled" : "Disabled",
                        StartupType = "Startup Folder",
                        Icon = icon,
                         IsVisible = true
                    });
                }
            }
        }

        private BitmapImage? GetIconFromShortcut(string shortcutPath)
        {
            try
            {
                var shell = new WshShell();
                var lnk = (IWshShortcut)shell.CreateShortcut(shortcutPath);
                string targetPath = lnk.TargetPath;
                return GetIcon(targetPath);
            }
            catch
            {
                return null;
            }
        }

        private bool IsStartupFolderShortcutEnabled(string shortcutPath)
        {
            try
            {
                var attr = System.IO.File.GetAttributes(shortcutPath);
                return (attr & (FileAttributes.Hidden | FileAttributes.System)) == 0;
            }
            catch { return true; }
        }

        private BitmapImage? GetIcon(string path)
        {
            try
            {
                string exe = path.Trim('"').Split(' ')[0];
                if (!System.IO.File.Exists(exe)) return null;
                var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                using var ms = new MemoryStream();
                sysIcon.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                return bmp;
            }
            catch { return null; }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadStartupApps();
 

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddStartup startup = new AddStartup();
            startup.ShowDialog();
            LoadStartupApps();
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (StartupAppsDataGrid.SelectedItem != null)
            {
                StartupAppsDataGrid.BeginEdit();
            }
        }

        private void StartupAppsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedApp = StartupAppsDataGrid.SelectedItem as StartupApp;
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
        public void savesettings(string setting)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "Settings.txt");

                string key = setting.Split(':')[0];
                List<string> lines;

                if (System.IO.File.Exists(filePath))
                {
                    lines = System.IO.File.ReadAllLines(filePath).ToList();
                }
                else
                {

                    lines = new List<string>();
                }

                int index = lines.FindIndex(line => line.StartsWith(key + ":"));
                if (index >= 0)
                {
                    lines[index] = setting;
                }
                else
                {
                    lines.Add(setting);
                }

                System.IO.File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {

            }
        }
        private void tglShowNotifications_Checked(object sender, RoutedEventArgs e)
        {
            savesettings("startupwarning:1");
            _notificationsEnabled = true;


            _knownStartupItems = new HashSet<string>(
                StartupApps.Select(app => $"{app.StartupType}|{app.Name}|{app.Path}"));

            _startupMonitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _startupMonitorTimer.Tick += StartupMonitorTimer_Tick;
            _startupMonitorTimer.Start();
        }

        private DispatcherTimer? _startupMonitorTimer;
        private HashSet<string> _knownStartupItems = new();
        private bool _notificationsEnabled = false;
        private void tglShowNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            _notificationsEnabled = false;
            _startupMonitorTimer?.Stop();
            _startupMonitorTimer = null;
            savesettings("startupwarning:0");
        }
        private void StartupAppsDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.DisplayIndex == 0)  
            {
                if (e.Row.Item is StartupApp editedApp)
                {
                    var textBox = e.EditingElement as TextBox;
                    if (textBox == null) return;

                    string newName = textBox.Text.Trim();

                    if (string.IsNullOrEmpty(newName) || newName == editedApp.ValueName)
                        return;  

                    try
                    {
                        if (editedApp.StartupType.StartsWith("Registry") && editedApp.RegistryRoot != null)
                        {
                            using var key = editedApp.RegistryRoot.OpenSubKey(editedApp.RegistryPath, writable: true);
                            if (key == null) return;
 
                            if (key.GetValue(newName) != null)
                            {
                                MessageBox.Show($"A startup item with the name '{newName}' already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                e.Cancel = true; 
                                return;
                            }

                          
                            var value = key.GetValue(editedApp.ValueName);
                            var valueKind = key.GetValueKind(editedApp.ValueName);
                            key.SetValue(newName, value, valueKind);
                             
                            key.DeleteValue(editedApp.ValueName);
                             
                            using var backupKey = editedApp.RegistryRoot.OpenSubKey(@"Software\MyApp\StartupBackups", writable: true);
                            if (backupKey != null)
                            {
                                var backupVal = backupKey.GetValue(editedApp.ValueName);
                                if (backupVal != null)
                                {
                                    backupKey.SetValue(newName, backupVal);
                                    backupKey.DeleteValue(editedApp.ValueName);
                                }
                            }
                             
                            editedApp.ValueName = newName;
                            editedApp.Name = newName;

                            MessageBox.Show($"Startup item renamed to '{newName}' successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
 
                        }
                        else
                        {
                            MessageBox.Show("Only registry startup items can be renamed.", "Not Supported", MessageBoxButton.OK, MessageBoxImage.Warning);
                            e.Cancel = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming startup item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        e.Cancel = true;
                    }
                }
            }
        }

        private List<NewStartupItemDetected> _openNotifications = new();

        private void StartupMonitorTimer_Tick(object? sender, EventArgs e)
        {
            StartupApps.Clear();

          
            ReadRegistryStartupApps(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run");
            ReadRegistryStartupApps(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run");
            ReadRegistryStartupApps(Registry.LocalMachine, @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run");

            AddBackupStartupApps(Registry.CurrentUser);
            AddBackupStartupApps(Registry.LocalMachine);
            AddBackupStartupApps(Registry.LocalMachine, isWow64: true);

           
            ReadStartupFolderShortcuts();
         

            var currentApps = StartupApps.ToList();

            foreach (var app in currentApps)
            {
                string key = $"{app.StartupType}|{app.Name}|{app.Path}";
                if (!_knownStartupItems.Contains(key))
                {
                    _knownStartupItems.Add(key);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var detect = new NewStartupItemDetected(app.Name, app.Path, this);
                        _openNotifications.Add(detect);
                        detect.Closed += (s, args) => _openNotifications.Remove(detect);
                        detect.Show();
                    });
                }
            }
        }
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private bool isMaximized = false;
        private double previousWidth, previousHeight, previousLeft, previousTop;
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

            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));


         

        public class StartupApp : INotifyPropertyChanged
        {
            public BitmapImage Icon { get; set; } = null!;
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public string StartupType { get; set; } = "";
            public RegistryKey RegistryRoot { get; set; } = null!;
            public string RegistryPath { get; set; } = "";
            public string ValueName { get; set; } = "";
            public string? BackupData { get; set; }
            public FileAttributes? OriginalAttributes { get; set; }

            private bool _isEnabled;
            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    if (_isEnabled != value)
                    {
                        _isEnabled = value;
                        Status = _isEnabled ? "Enabled" : "Disabled";
                        OnPropertyChanged();
                        try
                        {
                            ToggleInternally(_isEnabled);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Error while toggling startup item: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }

            private bool _isVisible;
            public bool IsVisible
            {
                get => _isVisible;
                set
                {
                    if (_isVisible != value)
                    {
                        _isVisible = value;
                        OnPropertyChanged();
                    }
                }
            }

            public void CheckEnabled()
            {
                if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                {
                    using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: false);
                    bool existsInRun = key?.GetValue(ValueName) != null;

                    bool enabledInApproved = true;

                    foreach (var approvedPath in new[]
                    {
                        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32",
                        @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
                    })
                    {
                        using var approvedKey = RegistryRoot.OpenSubKey(approvedPath, writable: false);
                        if (approvedKey == null) continue;

                        if (approvedKey.GetValue(ValueName) is byte[] data && data.Length > 0)
                        {
                            if (data[0] == 0x02)
                            {
                                enabledInApproved = false;
                                break;
                            }
                        }
                    }

                    IsEnabled = existsInRun && enabledInApproved;
                    IsVisible = existsInRun || !string.IsNullOrEmpty(BackupData);
                }
                else
                {
                    IsEnabled = false;
                    IsVisible = false;
                }

                Status = IsEnabled ? "Enabled" : "Disabled";
            }

            private void ToggleInternally(bool enable)
            {
                if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                {
                    using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: true);
                    if (enable)
                    {
                        if (!string.IsNullOrEmpty(BackupData))
                        {
                            key?.SetValue(ValueName, BackupData);
                            BackupData = null;
                            RemoveBackupData(RegistryRoot, ValueName);
                        }
                        UpdateStartupApproved(ValueName, RegistryRoot, true);
                    }
                    else
                    {
                        if (key?.GetValue(ValueName) is string currentValue)
                        {
                            BackupData = currentValue;
                            SaveBackupData(RegistryRoot, ValueName, currentValue);

                            key.DeleteValue(ValueName, false);
                        }
                        UpdateStartupApproved(ValueName, RegistryRoot, false);

                   
                    }
                }
            }

            private void SaveBackupData(RegistryKey rootKey, string valueName, string data)
            {
                using var backupKey = rootKey.CreateSubKey(@"Software\MyApp\StartupBackups");
                backupKey?.SetValue(valueName, data, RegistryValueKind.String);
            }

            private void RemoveBackupData(RegistryKey rootKey, string valueName)
            {
                using var backupKey = rootKey.OpenSubKey(@"Software\MyApp\StartupBackups", writable: true);
                backupKey?.DeleteValue(valueName, false);
            }

            private void UpdateStartupApproved(string valueName, RegistryKey rootKey, bool enabled)
            {
                foreach (var approvedPath in new[]
                {
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32",
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder"
                })
                {
                    using var approvedKey = rootKey.OpenSubKey(approvedPath, writable: true);
                    if (approvedKey == null) continue;

                    byte[] value = enabled ? new byte[] { 0x03, 0, 0, 0, 0 } : new byte[] { 0x02, 0, 0, 0, 0 };
                    approvedKey.SetValue(valueName, value, RegistryValueKind.Binary);
                }
            }

            private string _status = "";
            public string Status
            {
                get => _status;
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        OnPropertyChanged();
                    }
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string p = "") =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedApp == null)
            {
                MessageBox.Show("Please select a startup item to remove.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to remove '{SelectedApp.Name}' from startup?", "Confirm Removal", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (SelectedApp.StartupType.StartsWith("Registry") && SelectedApp.RegistryRoot != null)
                {
                    using var key = SelectedApp.RegistryRoot.OpenSubKey(SelectedApp.RegistryPath, writable: true);
                    key?.DeleteValue(SelectedApp.ValueName, false);
                     
                    using var backupKey = SelectedApp.RegistryRoot.OpenSubKey(@"Software\MyApp\StartupBackups", writable: true);
                    backupKey?.DeleteValue(SelectedApp.ValueName, false);
                }
                else if (SelectedApp.StartupType == "Scheduled Task")
                {
                    using TaskService ts = new();
                    var task = ts.FindTask(SelectedApp.ValueName);
                    if (task != null)
                    {
                        task.Enabled = false;
                        ts.RootFolder.DeleteTask(task.Name, false);
                    }
                }
                else if (SelectedApp.StartupType == "Startup Folder")
                {
                    if (System.IO.File.Exists(SelectedApp.Path))
                        System.IO.File.Delete(SelectedApp.Path);
                }

                MessageBox.Show($"'{SelectedApp.Name}' removed from startup.", "Removed", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadStartupApps();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing startup item: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddOperationButton_Click(object sender, RoutedEventArgs e)
        {
            var addOpWindow = new AddBootOperationWindow();
            if (addOpWindow.ShowDialog() == true && addOpWindow.ResultOperation != null)
            {
                _bootOperations.Add(addOpWindow.ResultOperation);
                SaveBootOperations();
                MessageBox.Show("Boot operation added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void tglEnableFileOps_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}

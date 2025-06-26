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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
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
        private async System.Threading.Tasks.Task LoadStartupFromTaskScheduler()
        {
            var appsToAdd = new List<StartupApp>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                using (TaskService ts = new TaskService())
                {
                    try
                    {
                        
                        SearchTasksRecursively(ts.RootFolder, appsToAdd);
                    }
                    catch (Exception ex)
                    {
                       
                        SearchTasksInFolder(ts.RootFolder, appsToAdd);
                    }
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var app in appsToAdd)
                {
                    StartupApps.Add(app);
                }
            });
        }
        private void SearchTasksRecursively(TaskFolder folder, List<StartupApp> appsToAdd)
        {
            try
            {
                
                SearchTasksInFolder(folder, appsToAdd);
               
                foreach (TaskFolder subFolder in folder.SubFolders)
                {
                    SearchTasksRecursively(subFolder, appsToAdd);
                }
            }
            catch (Exception ex)
            {
               
            }
        }
        private void SearchTasksInFolder(TaskFolder folder, List<StartupApp> appsToAdd)
        {
            try
            {
                foreach (var task in folder.Tasks)
                {
                    if (IsStartupTask(task))
                    {
                        var action = task.Definition.Actions.FirstOrDefault();
                        string actionPath = GetTaskActionPath(action);
                        string taskPath = folder.Path == "\\" ? task.Name : $"{folder.Path}\\{task.Name}";

                        var app = new StartupApp
                        {
                            Name = task.Name,
                            Path = actionPath,
                            Location = $"Task Scheduler ({folder.Path})",
                            StartupType = "Scheduled Task",
                            ValueName = taskPath,  
                            IsEnabled = task.Enabled, 
                            Status = task.Enabled ? "Enabled" : "Disabled"
                        };

                        app.CheckPresence();
                        appsToAdd.Add(app);
                    }
                }
            }
            catch (Exception ex)
            {
               
            }
        }
        private bool IsStartupTask(Microsoft.Win32.TaskScheduler.Task task)
        {
            try
            {
                
                bool hasLogonTrigger = task.Definition.Triggers.Any(t =>
                    t.TriggerType == TaskTriggerType.Logon);
                 
                bool hasBootTrigger = task.Definition.Triggers.Any(t =>
                    t.TriggerType == TaskTriggerType.Boot);
                 
                bool hasStartupTrigger = task.Definition.Triggers.Any(t =>
                    t.TriggerType == TaskTriggerType.Registration);

                return hasLogonTrigger || hasBootTrigger || hasStartupTrigger;
            }
            catch
            {
                return false;
            }
        }
        private string GetTaskActionPath(Microsoft.Win32.TaskScheduler.Action action)
        {
            try
            {
                if (action is ExecAction execAction)
                {
                    string path = execAction.Path;
                    if (!string.IsNullOrEmpty(execAction.Arguments))
                    {
                        path += $" {execAction.Arguments}";
                    }
                    return path;
                }
                else if (action is ComHandlerAction comAction)
                {
                    return $"COM Handler: {comAction.ClassId}";
                }
                else if (action is EmailAction emailAction)
                {
                    return $"Email: {emailAction.Subject}";
                }
                else if (action is ShowMessageAction msgAction)
                {
                    return $"Message: {msgAction.Title}";
                }
                else
                {
                    return action?.ToString() ?? "Unknown Action";
                }
            }
            catch
            {
                return "Unknown";
            }
        }
        private async void LoadStartupApps()
        {
            StartupApps.Clear();
            StartupAppsDataGrid.ItemsSource = null;

            string[] subKeys = new[]
            {
        @"Software\Microsoft\Windows\CurrentVersion\Run",
        @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
        @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run",
        @"Software\Microsoft\Windows\CurrentVersion\RunServices",
        @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run",
        @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce",
        @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunServices"
    };

            foreach (var subKey in subKeys)
            {
                await ReadRegistryStartupApps(Registry.CurrentUser, subKey);
                await ReadRegistryStartupApps(Registry.LocalMachine, subKey);
            }

            await AddBackupStartupApps(Registry.CurrentUser);
            await AddBackupStartupApps(Registry.LocalMachine);
            await AddBackupStartupApps(Registry.LocalMachine, isWow64: true);

            await LoadStartupFromTaskScheduler();
            await ReadStartupFolderShortcuts();

            await Dispatcher.InvokeAsync(() =>
            {
                StartupAppsDataGrid.ItemsSource = StartupApps;
                CollectionViewSource.GetDefaultView(StartupApps).Refresh();
            });
        }

        private async System.Threading.Tasks.Task ReadStartupFolderApps()
        {
            string currentUserPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string allUsersPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);

            await AddStartupFolderItems(currentUserPath);
            await AddStartupFolderItems(allUsersPath);
        }
        private async System.Threading.Tasks.Task AddStartupFolderItems(string folderPath)
        {
            var appsToAdd = new List<StartupApp>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                if (Directory.Exists(folderPath))
                {
                    var files = Directory.GetFiles(folderPath, "*.lnk");

                    foreach (var file in files)
                    {
                        appsToAdd.Add(new StartupApp
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            Path = file,
                            Location = "Startup Folder"
                        });
                    }
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var app in appsToAdd)
                {
                    StartupApps.Add(app);
                }
            });
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
        private async System.Threading.Tasks.Task AddBackupStartupApps(RegistryKey rootKey, bool isWow64 = false)
        {
          
            var appsToAdd = new List<StartupApp>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                string backupRegPath = @"Software\MyApp\StartupBackups";
                string runRegPath = isWow64
                    ? @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
                    : @"Software\Microsoft\Windows\CurrentVersion\Run";

                using var backupKey = rootKey.OpenSubKey(backupRegPath, writable: false);
                if (backupKey == null) return;

                foreach (string name in backupKey.GetValueNames())
                {
                    string? backupValue = backupKey.GetValue(name)?.ToString();
                    if (string.IsNullOrEmpty(backupValue)) continue;

                  
                    var app = new StartupApp
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
                        Icon = null!  
                    };

                    appsToAdd.Add(app);
                }
            });

        
            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var app in appsToAdd)
                {
                 
                    app.Icon = GetIcon(app.Path);

                    bool alreadyExists = StartupApps.Any(x =>
                        x != null &&
                        x.ValueName == app.ValueName &&
                        GetHiveName(x.RegistryRoot) == GetHiveName(app.RegistryRoot) &&
                        x.RegistryPath == app.RegistryPath);

                    if (!alreadyExists)
                    {
                        StartupApps.Add(app);
                    }
                }
            });
        }

        private async System.Threading.Tasks.Task ReadRegistryStartupApps(RegistryKey rootKey, string subKey)
        {
            var appsToAdd = new List<StartupApp>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                using var key = rootKey.OpenSubKey(subKey, writable: false);
                if (key == null) return;

                foreach (string name in key.GetValueNames())
                {
                    string path = key.GetValue(name)?.ToString() ?? "";
                    bool enabled = IsStartupApprovedEnabled(name, rootKey, subKey);
                    var icon = GetIcon(path);  

                    appsToAdd.Add(new StartupApp
                    {
                        RegistryRoot = rootKey,
                        RegistryPath = subKey,
                        ValueName = name,
                        Name = name,
                        Path = path,
                        IsEnabled = enabled,
                        Status = enabled ? "Enabled" : "Disabled",
                        StartupType = rootKey == Registry.CurrentUser ? "Registry (User)" : "Registry (Machine)",
                        Icon = icon
                    });
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var app in appsToAdd)
                {
                    StartupApps.Add(app);
                }
            });
        }

        private static bool IsStartupApprovedEnabled(string name, RegistryKey root, string runSubKey)
        {
            string approvedSubKey = runSubKey.Replace(@"Run", @"Explorer\StartupApproved\Run");

            using var key = root.OpenSubKey(approvedSubKey, false);
            if (key == null) return true; 

            if (key.GetValue(name) is byte[] data && data.Length > 0)
            {

                return (data[0] & 0x03) == 0x02 || (data[0] & 0x03) == 0x03;
            }

            return true;
        }


        private async System.Threading.Tasks.Task ReadStartupFolderShortcuts()
        {
            var appsToAdd = new List<StartupApp>();

            await System.Threading.Tasks.Task.Run(() =>
            {
                string[] startupFolders = new[]
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
                        bool enabled = IsStartupFolderShortcutEnabled(lnk);

                        var icon = GetIconFromShortcut(lnk);

                        appsToAdd.Add(new StartupApp
                        {
                            RegistryRoot = null!,
                            RegistryPath = "",
                            ValueName = name,
                            Name = name,
                            Path = lnk,
                            IsEnabled = enabled,
                            Status = enabled ? "Enabled" : "Disabled",
                            StartupType = "Startup Folder",
                            Icon = icon
                        });
                    }
                }
            });

            await Dispatcher.InvokeAsync(() =>
            {
                foreach (var app in appsToAdd)
                {
                    StartupApps.Add(app);
                }
            });
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

        private static bool IsStartupFolderShortcutEnabled(string shortcutPath)
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
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();  

                return bmp;
            }
            catch
            {
                return null;
            }
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
            if (this.Visibility == Visibility.Hidden)
            {
                StartupApps.Clear();


                LoadStartupApps();

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
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();

        public event PropertyChangedEventHandler? PropertyChanged;

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

        private bool _isEnabled;
        private bool _isPresent;
        private string _status = "";

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                    UpdateStatus();
                }
            }
        }

      
        public void SetEnabledByUser(bool enabled)
        {
            if (_isEnabled != enabled)
            {
                _isEnabled = enabled;
                OnPropertyChanged(nameof(IsEnabled));

                UpdateTaskSchedulerEnabled(enabled);
                UpdateStatus();
            }
        }

        private void UpdateTaskSchedulerEnabled(bool enabled)
        {
            if (StartupType == "Scheduled Task")
            {
                try
                {
                    using var ts = new TaskService();
                    string taskName = ValueName.Contains("\\") ? ValueName : Name;

                    var task = ts.FindTask(taskName);
                    if (task != null)
                    {
                        try
                        {
                            task.Enabled = enabled;
                            Application.Current.Dispatcher.Invoke(UpdateStatus);
                        }
                        catch (UnauthorizedAccessException) { }
                        catch (Exception) { }
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() => IsPresent = false);
                    }
                }
                catch (Exception) { }
            }
        }

        public bool IsPresent
        {
            get => _isPresent;
            set
            {
                if (_isPresent != value)
                {
                    _isPresent = value;
                    OnPropertyChanged();
                    if (!_isPresent)
                    {
                        _isEnabled = false;  
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                    UpdateStatus();
                }
            }
        }

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

        public string? BackupData { get; set; }
        public string Location { get; set; } = "";

        public bool IsCheckboxEnabled => Status != "Removed";
        public bool IsCheckboxChecked => Status == "Enabled";

        private ICommand? _toggleEnabledCommand;
        public ICommand ToggleEnabledCommand => _toggleEnabledCommand ??= new RelayCommand(param =>
        {
            if (param is bool isChecked)
            {
                SetEnabledByUser(isChecked);
            }
        });

            public void CheckPresence()
            {
                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: false);
                        IsPresent = key?.GetValue(ValueName) != null;

                        if (IsPresent)
                        {
                            IsEnabled = IsStartupApprovedEnabled(ValueName, RegistryRoot, RegistryPath);
                        }
                        else
                        {
                            IsEnabled = false;
                        }
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        using var ts = new TaskService();
                        var task = ts.FindTask(Name);

                        IsPresent = task != null;
                        IsEnabled = task?.Enabled ?? false;
                    }
                    else if (StartupType == "Startup Folder")
                    {
                        IsPresent = System.IO.File.Exists(Path);
                        IsEnabled = IsPresent;
                    }
                    else
                    {
                        IsPresent = false;
                        IsEnabled = false;
                    }
                }
                catch
                {
                    IsPresent = false;
                    IsEnabled = false;
                }
                UpdateStatus();
            }

            private void UpdateStatus()
            {
                Status = !IsPresent ? "Removed" : (IsEnabled ? "Enabled" : "Disabled");

                OnPropertyChanged(nameof(IsCheckboxEnabled));
                OnPropertyChanged(nameof(IsCheckboxChecked));
            }

            public void Remove()
        {
            if (!IsPresent) return;

            try
            {
                if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                {
                    using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: true);
                    if (key?.GetValue(ValueName) is string currentValue)
                    {
                        BackupData = currentValue;
                        SaveBackupData(RegistryRoot, ValueName, currentValue);
                        key.DeleteValue(ValueName, false);
                        IsPresent = false;
                        IsEnabled = false;
                    }
                }
                else if (StartupType == "Scheduled Task")
                {
                    using var ts = new TaskService();
                    string taskName = ValueName.Contains("\\") ? ValueName : Name;
                    var task = ts.FindTask(taskName);
                    if (task != null)
                    {
                        BackupData = task.Xml;
                        try
                        {
                            string folderPath = System.IO.Path.GetDirectoryName(taskName) ?? "\\";
                            string taskNameOnly = System.IO.Path.GetFileName(taskName);
                            var folder = ts.GetFolder(folderPath);
                            folder.DeleteTask(taskNameOnly);
                            IsPresent = false;
                            IsEnabled = false;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            task.Enabled = false;
                            IsEnabled = false;
                        }
                        catch (Exception) { }
                    }
                }
                else if (StartupType == "Startup Folder" && System.IO.File.Exists(Path))
                {
                    BackupData = Path;
                    System.IO.File.Delete(Path);
                    IsPresent = false;
                    IsEnabled = false;
                }
            }
            catch (Exception) { }
        }

        public void Restore()
        {
            if (IsPresent) return;

            try
            {
                if (StartupType.StartsWith("Registry") && RegistryRoot != null && !string.IsNullOrEmpty(BackupData))
                {
                    using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: true);
                    key?.SetValue(ValueName, BackupData);
                    RemoveBackupData(RegistryRoot, ValueName);
                    BackupData = null;
                    IsPresent = true;
                    IsEnabled = true;
                }
                else if (StartupType == "Scheduled Task" && !string.IsNullOrEmpty(BackupData))
                {
                    using var ts = new TaskService();
                    var taskDefinition = ts.NewTask();
                    taskDefinition.XmlText = BackupData;

                    string taskName = ValueName.Contains("\\") ? ValueName : Name;
                    string folderPath = System.IO.Path.GetDirectoryName(taskName) ?? "\\";
                    string taskNameOnly = System.IO.Path.GetFileName(taskName);
                    var folder = ts.GetFolder(folderPath);

                    folder.RegisterTaskDefinition(taskNameOnly, taskDefinition,
                        TaskCreation.CreateOrUpdate, null, null, TaskLogonType.InteractiveToken);

                    BackupData = null;
                    IsPresent = true;
                    IsEnabled = true;
                }
                else if (StartupType == "Startup Folder" && !string.IsNullOrEmpty(BackupData))
                {
                    if (System.IO.File.Exists(BackupData))
                    {
                        System.IO.File.Copy(BackupData, Path, true);
                    }
                    BackupData = null;
                    IsPresent = true;
                    IsEnabled = true;
                }
            }
            catch (Exception) { }
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

        
        private bool IsStartupFolderShortcutEnabled(string path)
        {
           
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

         
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
}

        private void tglEnableFileOps_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}

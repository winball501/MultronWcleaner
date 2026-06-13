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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
           
          
          
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
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
                            Status = task.Enabled ? "Enabled" : "Disabled",
                            IsPresent = true
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
        public static bool RemoveWinlogonEntry(string pathToRemove)
        {
            try
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", writable: true);

                if (key == null) return false;

                string raw = key.GetValue("Userinit") as string ?? "";

                var parts = raw.Split(',')
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                               .Where(p => !p.Equals(pathToRemove, StringComparison.OrdinalIgnoreCase))
                               .ToList();
                 
                string userinit = @"C:\Windows\system32\userinit.exe";
                if (!parts.Any(p => p.EndsWith("userinit.exe", StringComparison.OrdinalIgnoreCase)))
                    parts.Insert(0, userinit);
                 
                string newValue = string.Join(",", parts) + ",";
                key.SetValue("Userinit", newValue);

                return true;
            }
            catch { return false; }
        }
     
        public void GetWinlogonEntries()
        {
            var entries = new List<StartupApp>();

            try
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                 string raw = key.GetValue("Userinit") as string ?? "";

               
                var parts = raw.Split(',') .Select(p => p.Trim())  .Where(p => !string.IsNullOrEmpty(p));

                foreach (var part in parts)
                {
                  
                    if (part.EndsWith("userinit.exe", StringComparison.OrdinalIgnoreCase))
                        continue;
                    bool isenabled = true;
                    if(System.IO.File.Exists(part))
                    {
                         isenabled = true;
                    } else
                    {
                         isenabled = false;
                    }
                    var icon = GetIcon(part);
                    
                    entries.Add(new StartupApp
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(part),
                        Path = part,
                        Location = $"Location ({key.Name})",
                        StartupType = "Userinit (WinLogon)",
                        IsEnabled = isenabled,
                        Icon = icon,
                        BackupData = "NO_BACKUP",
                        Status = isenabled ? "Enabled" : "Disabled",
                        IsPresent = true

                    });
                }
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var app in entries)
                    {
                        StartupApps.Add(app);
                    }
                });
            }
            catch (Exception ex) {

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                  
                    MessageBox.Show(
                        ex.Message,
                        "Operation Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
         

        }
        private Storyboard _spinnerStoryboard;



         
        private bool _isLoadingStartupApps = false;

        private async void LoadStartupApps()
        {
      
            if (!this.IsVisible || _isLoadingStartupApps)
            {
                return;
            }

            try
            { 
                _isLoadingStartupApps = true;

                LoadingOverlay.Visibility = Visibility.Visible;
                StartupAppsDataGrid.ItemsSource = null;
                StartupApps.Clear();
 
                await System.Threading.Tasks.Task.Run(async() =>
                {
                    GetWinlogonEntries();

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

                  
                });
                await LoadStartupFromTaskScheduler();
                await ReadStartupFolderShortcuts();
                if (!this.IsVisible)
                {
                    return; 
                }

                
                StartupAppsDataGrid.ItemsSource = StartupApps;
                CollectionViewSource.GetDefaultView(StartupApps).Refresh();
                SubscribeToPendingChanges(StartupApps);
            }
            finally
            {
                 await System.Threading.Tasks.Task.Delay(50);
                 LoadingOverlay.Visibility = Visibility.Collapsed;
                _isLoadingStartupApps = false;
            }
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
                            Name = System.IO.Path.GetFileNameWithoutExtension(file),
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
            
            string file = System.IO.Path.Combine(Environment.CurrentDirectory, "BootOperations.json");
            var json = System.Text.Json.JsonSerializer.Serialize(_bootOperations);
            System.IO.File.WriteAllText(file, json);
        }

        private void LoadBootOperations()
        {
            string file = System.IO.Path.Combine(Environment.CurrentDirectory, "BootOperations.json");
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
                    bool enabled = IsStartupEnabled(name, rootKey, subKey);
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
                        Icon = icon,
                        IsPresent = true
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
        
        private static bool IsStartupEnabled(string name, RegistryKey root, string runSubKey)
        {
            
            string approvedSubKey = runSubKey.Replace(
                @"Run",
                @"Explorer\StartupApproved\Run"
            );

            using var key = root.OpenSubKey(approvedSubKey, false);
            if (key == null)
                return false;  

            if (key.GetValue(name) is byte[] data && data.Length > 0)
            {
              
                return (data[0] & 1) == 0;
            }

            return false;
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
                        string name = System.IO.Path.GetFileNameWithoutExtension(lnk);
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

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
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
                } else if (SelectedApp.StartupType == "Userinit (WinLogon)")
                {
                     RemoveWinlogonEntry(SelectedApp.Path);
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
                var addOp = addOpWindow.ResultOperation;

                var op = new StartupManager.BootOperation
                {
                    Type = (StartupManager.OperationType)Enum.Parse(typeof(StartupManager.OperationType), addOp.Type.ToString()),
                    SourcePath = addOp.SourcePath,
                    DestinationPath = addOp.DestinationPath,
                   
                };

                _bootOperations.Add(op);
                SaveBootOperations();
                MessageBox.Show("Boot operation added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SubscribeToPendingChanges(IEnumerable<StartupApp> items)
        {
            foreach (var item in items)
            {
                item.PropertyChanged -= StartupApp_PropertyChanged;  
                item.PropertyChanged += StartupApp_PropertyChanged;
            }
        }

        private void StartupApp_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StartupApp.HasPendingChange))
            {
                btnApply.IsEnabled = StartupAppsDataGrid.ItemsSource
                    .Cast<StartupApp>()
                    .Any(a => a.HasPendingChange);
            }
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            btnApply.IsEnabled = false;
            btnApply.Content = "Applying...";

            var changedItems = StartupAppsDataGrid.ItemsSource
                .Cast<StartupApp>()
                .Where(a => a.HasPendingChange)
                .ToList();

            if (!changedItems.Any())
            {
                btnApply.IsEnabled = true;
                btnApply.Content = "Apply";
                MessageBox.Show("No Changes Found!", "Multron Startup Manager", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var itemsChanged = new List<string>();

            foreach (var item in changedItems)
            {
                string before = item.Status;
                item.ApplyPendingChange();
                string after = item.Status;
                itemsChanged.Add($"{item.Name}:  {before}  →  {after}");
            }

            StartupAppsDataGrid.Items.Refresh();
            btnApply.IsEnabled = true;
            btnApply.Content = "Apply";

            MessageBox.Show(string.Join(Environment.NewLine, itemsChanged),   "Multron Startup Manager",  MessageBoxButton.OK,   MessageBoxImage.Information);
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
            private bool _pendingEnabled;
            private bool _hasPendingChange;
            public bool HasPendingChange
            {
                get => _hasPendingChange;
            }

            public bool PendingEnabled
            {
                get => _pendingEnabled;
                set
                {
                    if (_pendingEnabled != value)
                    {
                        _pendingEnabled = value;
                        _hasPendingChange = (value != IsEnabled);
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(HasPendingChange));
                    }
                }
            }
            public bool IsEnabled
            {
                get => _isEnabled;
                set
                {
                    if (_isEnabled != value)
                    {
                        _isEnabled = value;
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(HasPendingChange));
                        UpdateStatus();

                       
                        if (!_hasPendingChange)
                        {
                            _pendingEnabled = value;
                            OnPropertyChanged(nameof(PendingEnabled));
                        }
                    }
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
                     
                        OnPropertyChanged(nameof(IsCheckboxEnabled));
                    }
                }
            }

            public string? BackupData { get; set; }
            public string Location { get; set; } = "";

            public bool IsCheckboxEnabled => Status != "Removed";

            public void ApplyPendingChange()
            {
                if (!_hasPendingChange) return;

                SetEnabledByUser(_pendingEnabled);
 
                _pendingEnabled = IsEnabled;
                _hasPendingChange = false;
                OnPropertyChanged(nameof(PendingEnabled));
                OnPropertyChanged(nameof(HasPendingChange));
            }

            public void SetEnabledByUser(bool enabled)
            {
                System.Diagnostics.Debug.WriteLine($"SetEnabledByUser called: {Name}, enabled={enabled}, CurrentPresent={IsPresent}, CurrentEnabled={IsEnabled}");

                try
                {
                    if (enabled)
                    {
                        System.Diagnostics.Debug.WriteLine($"Enable requested for {Name}");

                         
                        if (!IsPresent && !string.IsNullOrEmpty(BackupData) && BackupData != "NO_BACKUP")
                        {
                            System.Diagnostics.Debug.WriteLine($"Restoring from backup: {Name}");
                            Restore();
                        }
                        else if (IsPresent && !IsEnabled)
                        {
                            System.Diagnostics.Debug.WriteLine($"Enabling existing item: {Name}, Type: {StartupType}");

                           
                            if (StartupType == "Scheduled Task")
                            {
                                UpdateTaskSchedulerEnabled(true);
                            }
                            else if (StartupType.StartsWith("Registry"))
                            {
                                SetRegistryStartupApproved(true);
                            }
                            else if (StartupType == "Startup Folder")
                            {
                                IsEnabled = true;
                            }
                        }
                        else if (!IsPresent && (string.IsNullOrEmpty(BackupData) || BackupData == "NO_BACKUP"))
                        {
                            System.Diagnostics.Debug.WriteLine($"Cannot enable {Name}: No backup data available");
                            
                            IsEnabled = false;
                        }
                        else if (IsPresent && IsEnabled)
                        {
                            System.Diagnostics.Debug.WriteLine($"Item already enabled: {Name}");
                        }
                    }
                    else
                    {
                     

                        if (StartupType.StartsWith("Registry"))
                        {
                            using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: false);
                            var value = key?.GetValue(ValueName);

                            if (value != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Registry disable: backing up and removing");

                                BackupRegistryValue();    
                                RemoveRegistryValue();    
                                IsEnabled = false;       
                                IsPresent = false;    
                            }
                            else
                            { 
                                IsEnabled = false;
                                IsPresent = false;
                            }
                        }
                        else if (StartupType == "Scheduled Task")
                        {
                            UpdateTaskSchedulerEnabled(false);
                        }
                        else if (StartupType == "Startup Folder")
                        {
                            if (System.IO.File.Exists(Path))
                            {
                                BackupStartupFolderFile();
                                RemoveStartupFolderFile();
                                IsEnabled = true;
                                IsPresent = false;
                            }
                            else
                            {
                                IsEnabled = false;
                                IsPresent = false;
                            }
                        }
                    }
                     

                    UpdateStatus();
                     

                    OnPropertyChanged(nameof(IsEnabled));
                    OnPropertyChanged(nameof(IsPresent));
                  
                    OnPropertyChanged(nameof(IsCheckboxEnabled));
 
                }
                catch (Exception ex)
                { 
               
                    CheckPresence();
                }
            }

            private void BackupRegistryValue()
            {
                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null && IsPresent)
                    {
                        System.Diagnostics.Debug.WriteLine($"BackupRegistryValue: {Name}, Path: {RegistryPath}, ValueName: {ValueName}");

                        using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: false);
                        if (key?.GetValue(ValueName) is string currentValue)
                        {
                            BackupData = currentValue;
                            SaveBackupData(RegistryRoot, ValueName, currentValue);
                            System.Diagnostics.Debug.WriteLine($"Backup saved: {currentValue}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"No value found to backup");
                            BackupData = "NO_BACKUP";
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BackupRegistryValue exception: {ex.Message}");
                    BackupData = "NO_BACKUP";
                }
            }

            private void RemoveRegistryValue()
            {
                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"RemoveRegistryValue: {Name}, Path: {RegistryPath}, ValueName: {ValueName}");

                        using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: true);
                        if (key != null)
                        {
                            var valueBefore = key.GetValue(ValueName);
                            System.Diagnostics.Debug.WriteLine($"Value before delete: {valueBefore}");

                            key.DeleteValue(ValueName, false);

                            var valueAfter = key.GetValue(ValueName);
                            System.Diagnostics.Debug.WriteLine($"Value after delete: {valueAfter}");

                            IsPresent = false;
                            IsEnabled = false;

                            System.Diagnostics.Debug.WriteLine($"Registry value removed, IsPresent={IsPresent}, IsEnabled={IsEnabled}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Could not open registry key for writing");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveRegistryValue exception: {ex.Message}");
                }
            }

            private void BackupStartupFolderFile()
            {
                try
                {
                    if (!string.IsNullOrEmpty(Path) && System.IO.File.Exists(Path))
                    {
                        string backupPath = Path + ".bak";
                        System.IO.File.Copy(Path, backupPath, overwrite: true);
                        BackupData = backupPath;
                        System.Diagnostics.Debug.WriteLine($"Backup created: {backupPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BackupStartupFolderFile exception: {ex.Message}");
                    BackupData = "NO_BACKUP";
                }
            }

            private void RemoveStartupFolderFile()
            {
                try
                {
                    if (StartupType == "Startup Folder" && System.IO.File.Exists(Path))
                    {
                        System.IO.File.Delete(Path);
                        IsPresent = false;
                        System.Diagnostics.Debug.WriteLine($"Startup folder file removed: {Path}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveStartupFolderFile exception: {ex.Message}");
                }
            }

            private void SetRegistryStartupApproved(bool enabled)
            {
                try
                {
                    string approvedSubKey = RegistryPath.Replace(@"Run", @"Explorer\StartupApproved\Run");
                    using var key = RegistryRoot.CreateSubKey(approvedSubKey);
                    if (key != null)
                    {
                        byte[] data = new byte[12];
                        data[0] = enabled ? (byte)0x02 : (byte)0x03;
                        key.SetValue(ValueName, data, RegistryValueKind.Binary);
                        IsEnabled = enabled;
                        System.Diagnostics.Debug.WriteLine($"Registry StartupApproved set to {enabled} for {Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SetRegistryStartupApproved exception: {ex.Message}");
                    IsEnabled = enabled;
                }
            }

            private void UpdateTaskSchedulerEnabled(bool enabled)
            {
                if (StartupType != "Scheduled Task") return;

                try
                {
                    using var ts = new TaskService();
                    string taskName = !string.IsNullOrEmpty(ValueName) ? ValueName : Name;

                    var task = ts.FindTask(taskName);
                    if (task != null)
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"Setting task {taskName} enabled = {enabled}");
                            task.Enabled = enabled;

                        
                            IsEnabled = task.Enabled;
                            IsPresent = true;

                           

                            Application.Current.Dispatcher.Invoke(UpdateStatus);
                        }
                        catch (UnauthorizedAccessException ex)
                        {
                            
                          
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                IsEnabled = task.Enabled;
                                IsPresent = true;
                                UpdateStatus();
                            });
                        }
                        catch (Exception ex)
                        {
                         
                  
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                CheckTaskPresence();
                                UpdateStatus();
                            });
                        }
                    }
                    else
                    { 
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsPresent = false;
                            IsEnabled = false;
                            UpdateStatus();
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"UpdateTaskSchedulerEnabled exception: {ex.Message}");
                    Application.Current.Dispatcher.Invoke(CheckPresence);
                }
            }

            private void CheckTaskPresence()
            {
                try
                {
                    using var ts = new TaskService();
                    string taskName = !string.IsNullOrEmpty(ValueName) ? ValueName : Name;
                    var task = ts.FindTask(taskName);

                    IsPresent = task != null;
                    IsEnabled = task?.Enabled ?? false;

                    System.Diagnostics.Debug.WriteLine($"CheckTaskPresence for {taskName}: Present={IsPresent}, Enabled={IsEnabled}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"CheckTaskPresence exception: {ex.Message}");
                    IsPresent = false;
                    IsEnabled = false;
                }
            }

            public void CheckPresence()
            {
                if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.CheckAccess())
                {
                    Application.Current.Dispatcher.Invoke(CheckPresence);
                    return;
                }

         

                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        using var key = RegistryRoot.OpenSubKey(RegistryPath, writable: false);
                        var regValue = key?.GetValue(ValueName) as string;

                        IsPresent = !string.IsNullOrEmpty(regValue);
               

                        if (IsPresent)
                        {
                            IsEnabled = IsStartupEnabled(ValueName, RegistryRoot, RegistryPath);
                       

                            if (!string.IsNullOrEmpty(BackupData) && BackupData != "NO_BACKUP")
                            {
                                RemoveBackupData(RegistryRoot, ValueName);
                                BackupData = null;
                          
                            }
                        }
                        else
                        {
                            IsEnabled = false;

                            if (string.IsNullOrEmpty(BackupData) || BackupData == "NO_BACKUP")
                            {
                                using var backupKey = RegistryRoot.OpenSubKey(@"Software\MyApp\StartupBackups", writable: false);
                                var backupValue = backupKey?.GetValue(ValueName) as string;

                                if (!string.IsNullOrEmpty(backupValue))
                                {
                                    BackupData = backupValue;
                                    
                                }
                                else
                                {
                                    BackupData = "NO_BACKUP";
                                 
                                }
                            }
                        }
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        using var ts = new TaskService();
                        var task = ts.FindTask(Name);  
                        IsPresent = task != null;
                        IsEnabled = task?.Enabled ?? false;
                        BackupData = null; 
                         
                    }
                    else if (StartupType == "Startup Folder")
                    {
                        IsPresent = System.IO.File.Exists(Path);
                        IsEnabled = IsPresent;

                        if (!IsPresent)
                        {
                            string backupPath = Path + ".bak";
                            BackupData = System.IO.File.Exists(backupPath) ? backupPath : "NO_BACKUP";
                        
                        }
                        else
                        {
                            BackupData = null;
                        }
 
                    }
                    else if (StartupType == "Userinit (WinLogon)")
                    {
                        IsPresent = CheckİfEntryExists(Path);
                        IsEnabled = IsPresent;
                        BackupData = "NO_BACKUP";
                     

                    } else
                    {
                        IsPresent = false;
                        IsEnabled = false;
                        BackupData = "NO_BACKUP";
                    }
                }
                catch (Exception ex)
                {
               
                    IsPresent = false;
                    IsEnabled = false;
                    BackupData = "NO_BACKUP";
                }

                UpdateStatus();

                _pendingEnabled = IsEnabled;
                _hasPendingChange = false;
                OnPropertyChanged(nameof(PendingEnabled));
                OnPropertyChanged(nameof(HasPendingChange));
                OnPropertyChanged(nameof(IsCheckboxEnabled));
        
            }
            private bool CheckİfEntryExists(string pathtocheck)
            {
                var entries = new List<StartupApp>();

                try
                {
                    using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                    string raw = key.GetValue("Userinit") as string ?? "";


                    var parts = raw.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p));

                    foreach (var part in parts)
                    {

                        if (part.EndsWith("userinit.exe", StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (part.Contains(pathtocheck))
                        {
                            return true;
                        }

                    }

                }
                catch (Exception ex)
                {
                    return false;

                }
                return false;

            }
            private void UpdateStatus()
            {
             

                if (IsPresent && IsEnabled)
                    Status = "Enabled";
                else if (IsPresent && !IsEnabled)
                    Status = "Disabled";
                else if (!IsPresent && !string.IsNullOrEmpty(BackupData) && BackupData != "NO_BACKUP")
                    Status = "Disabled Backup";
                else
                    Status = "Removed";
                   
           
 
    
            }

            public void Remove()
            {
                if (!IsPresent) return;

                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        BackupRegistryValue();
                        RemoveRegistryValue();
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        using var ts = new TaskService();
                        string taskName = !string.IsNullOrEmpty(ValueName) ? ValueName : Name;
                        var task = ts.FindTask(taskName);
                        if (task != null)
                        {
                            try
                            {
                                string folderPath = System.IO.Path.GetDirectoryName(taskName) ?? "\\";
                                string taskNameOnly = System.IO.Path.GetFileName(taskName);
                                var folder = ts.GetFolder(folderPath);
                                folder.DeleteTask(taskNameOnly);

                                IsPresent = false;
                                IsEnabled = false;
                                BackupData = null;

                                System.Diagnostics.Debug.WriteLine($"Task {taskName} removed successfully");
                            }
                            catch (UnauthorizedAccessException)
                            {
                                System.Diagnostics.Debug.WriteLine($"Cannot delete task {taskName}, disabling instead");
                                task.Enabled = false;
                                IsEnabled = false;
                                IsPresent = true;
                            }
                        }
                    }
                    else if (StartupType == "Startup Folder" && System.IO.File.Exists(Path))
                    {
                        string backupPath = Path + ".bak";
                        System.IO.File.Copy(Path, backupPath, true);
                        BackupData = backupPath;
                        System.IO.File.Delete(Path);
                        IsPresent = false;
                        IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Remove exception: {ex.Message}");
                }

                UpdateStatus();
            }

            public void Restore()
            {
                if (IsPresent || string.IsNullOrEmpty(BackupData) || BackupData == "NO_BACKUP") return;

                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        using var key = RegistryRoot.CreateSubKey(RegistryPath);
                        key?.SetValue(ValueName, BackupData);
                        RemoveBackupData(RegistryRoot, ValueName);
                        BackupData = null;
                        IsPresent = true;
                        IsEnabled = true;

                        System.Diagnostics.Debug.WriteLine($"Registry value restored for {Name}");
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        RestoreScheduledTaskFromBackup();
                    }
                    else if (StartupType == "Startup Folder" && System.IO.File.Exists(BackupData))
                    {
                        System.IO.File.Copy(BackupData, Path, true);
                        System.IO.File.Delete(BackupData);
                        BackupData = null;
                        IsPresent = true;
                        IsEnabled = true;

                        System.Diagnostics.Debug.WriteLine($"Startup folder file restored for {Name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Restore exception: {ex.Message}");
                }

                UpdateStatus();
            }

            private void RestoreScheduledTaskFromBackup()
            {
                try
                {
                    if (string.IsNullOrEmpty(BackupData) || BackupData == "NO_BACKUP")
                    {
                        System.Diagnostics.Debug.WriteLine($"No backup data for Scheduled Task {Name}");
                        return;
                    }

                    using (TaskService ts = new TaskService())
                    {

                        var existingTask = ts.FindTask(Name);
                        if (existingTask != null)
                        {
                            ts.RootFolder.DeleteTask(Name);
                            System.Diagnostics.Debug.WriteLine($"Deleted existing task before restore: {Name}");

                            using (var sr = new System.IO.StringReader(BackupData))
                            {
                                var taskDefinition = ts.NewTask();
                                taskDefinition.XmlText = BackupData;

                                ts.RootFolder.RegisterTaskDefinition(Name, taskDefinition);
                                System.Diagnostics.Debug.WriteLine($"Scheduled Task restored from backup: {Name}");
                            }
                        }

                        IsPresent = true;
                        IsEnabled = true;
                        BackupData = null;

                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception restoring Scheduled Task {Name}: {ex.Message}");
                }
            }

            private void SaveBackupData(RegistryKey rootKey, string valueName, string valueData)
            {
                try
                {
                    using var backupKey = rootKey.CreateSubKey(@"Software\MyApp\StartupBackups", writable: true);
                    if (backupKey != null)
                    {
                        backupKey.SetValue(valueName, valueData, RegistryValueKind.String);
                        System.Diagnostics.Debug.WriteLine($"Backup saved for {valueName} with data: {valueData}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to open/create backup registry key");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Exception saving backup data: {ex.Message}");
                }
            }

            private void RemoveBackupData(RegistryKey rootKey, string valueName)
            {
                try
                {
                    using var backupKey = rootKey.OpenSubKey(@"Software\MyApp\StartupBackups", writable: true);
                    backupKey?.DeleteValue(valueName, false);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"RemoveBackupData exception: {ex.Message}");
                }
            }

            private static bool IsStartupEnabled(string name, RegistryKey root, string runSubKey)
            {

                string approvedSubKey = runSubKey.Replace(
                    @"Run",
                    @"Explorer\StartupApproved\Run"
                );

                using var key = root.OpenSubKey(approvedSubKey, false);
                if (key == null)
                    return false; 

                if (key.GetValue(name) is byte[] data && data.Length > 0)
                { 
                    return (data[0] & 1) == 0;
                }

                return false;
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string propertyName = "")
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public class RelayCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Func<object?, bool>? _canExecute;

            public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

            public void Execute(object? parameter) => _execute(parameter);

            public event EventHandler? CanExecuteChanged
            {
                add { CommandManager.RequerySuggested += value; }
                remove { CommandManager.RequerySuggested -= value; }
            }
        }
      
    }
}

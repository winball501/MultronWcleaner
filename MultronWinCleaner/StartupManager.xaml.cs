using IWshRuntimeLibrary;
using Microsoft.Win32;
using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        public ObservableCollection<StartupApp> StartupApps { get; set; } = new();
        Utilities utilities;
        private static readonly ConcurrentDictionary<string, BitmapImage> IconCache = new();

        private StartupApp? selectedApp;
        public StartupApp? SelectedApp
        {
            get => selectedApp;
            set { selectedApp = value; OnPropertyChanged(); btnRemove.IsEnabled = value != null; }
        }

        private bool _isLoading = false;
        private DispatcherTimer? _realTimeWatcher;
        private HashSet<string> _knownItems = new();
        private bool _notificationsEnabled = false;

        public StartupManager(Utilities utilities)
        {
            InitializeComponent();
            DataContext = this;

            this.utilities = utilities;
            tglShowNotifications.IsChecked = _notificationsEnabled;

            Loaded += async (s, e) =>
            {
                await LoadStartupAppsFastAsync(isSilentRefresh: false);

                _realTimeWatcher = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                _realTimeWatcher.Tick += async (s, ev) => await LoadStartupAppsFastAsync(isSilentRefresh: true);
                _realTimeWatcher.Start();
            };
            StartRealTimeMonitoring();
        }

        private CancellationTokenSource _monitoringCts;
        protected override void OnClosed(EventArgs e)
        {
            _monitoringCts?.Cancel();
            base.OnClosed(e);
        }

        private void StartRealTimeMonitoring()
        {
            _monitoringCts = new CancellationTokenSource();
            var token = _monitoringCts.Token;

            System.Threading.Tasks.Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await LoadStartupAppsFastAsync(true);
                        await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(5), token);
                    }
                    catch (TaskCanceledException) { break; }
                    catch (Exception ex) { Debug.WriteLine($"Real-time monitoring error: {ex.Message}"); }
                }
            }, token);
        }

        private async System.Threading.Tasks.Task LoadStartupAppsFastAsync(bool isSilentRefresh)
        {
            if (_isLoading) return;
            try
            {
                if (!isSilentRefresh) LoadingOverlay.Visibility = Visibility.Visible;
                _isLoading = true;

                await System.Threading.Tasks.Task.Delay(50);

                var allApps = new ConcurrentBag<StartupApp>();

                await System.Threading.Tasks.Task.Run(() =>
                {
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();
                    var staThread = new Thread(() =>
                    {
                        try
                        {
                            GetWinlogonEntries(allApps);
                            GetRegistryStartupApps(allApps);
                            GetTaskSchedulerApps(allApps);
                            GetStartupFolderApps(allApps);
                            tcs.SetResult(true);
                        }
                        catch (Exception ex) { tcs.SetException(ex); }
                    });

                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.IsBackground = true;
                    staThread.Start();

                    return tcs.Task;
                });

                var sortedApps = allApps.OrderBy(a => a.Name).ToList();
                var currentHashes = new HashSet<string>(sortedApps.Select(a => a.UniqueId));

                if (_knownItems == null) _knownItems = new HashSet<string>();

                if (isSilentRefresh && _knownItems.SetEquals(currentHashes)) return;

                var newItems = currentHashes.Except(_knownItems).ToList();
                bool isInitialScan = _knownItems.Count == 0;

                if (isSilentRefresh && !isInitialScan && newItems.Any() && _notificationsEnabled)
                {
                    foreach (var hash in newItems)
                    {
                        var newApp = sortedApps.First(a => a.UniqueId == hash);
                        Application.Current.Dispatcher.Invoke(() => {
                            try
                            {
                                var type = Type.GetType("MultronWinCleaner.NewStartupItemDetected");
                                if (type != null)
                                {
                                    var detect = Activator.CreateInstance(type, newApp.Name, newApp.Path, this) as Window;
                                    detect?.Show();
                                }
                            }
                            catch (Exception ex) { Debug.WriteLine($"Notification Error: {ex.Message}"); }
                        });
                    }
                }

                _knownItems = currentHashes;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var itemsToRemove = StartupApps.Where(a => !currentHashes.Contains(a.UniqueId)).ToList();
                    foreach (var item in itemsToRemove) StartupApps.Remove(item);

                    foreach (var app in sortedApps)
                    {
                        if (!StartupApps.Any(a => a.UniqueId == app.UniqueId))
                        {
                            app.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(StartupApp.HasPendingChange)) CheckPendingChanges(); };
                            app.HighImpactWarningAction = (msg) => ShowInAppWarning(msg);

                            StartupApps.Add(app);
                        }
                    }
                     
                    if (!isSilentRefresh)
                    {
                        var highImpactApps = StartupApps.Where(a => a.IsEnabled && a.Impact == "High").Select(a => a.Name).ToList();
                        if (highImpactApps.Any())
                        {
                            string appNames = string.Join(", ", highImpactApps.Take(3));
                            if (highImpactApps.Count > 3) appNames += " and others";
                            ShowInAppWarning($"Warning: You have high-impact apps ({appNames}) enabled. This may slow down your boot time.");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"A critical error occurred while loading the list:\n\n{ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isLoading = false;
                if (!isSilentRefresh) LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(StartupApps);
            if (view == null) return;

            string filter = SearchBox.Text.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(filter))
                view.Filter = null;
            else
                view.Filter = item => (item as StartupApp)?.Name.ToLowerInvariant().Contains(filter) == true;
        }

        private void tglShowNotifications_Checked(object sender, RoutedEventArgs e)
        {
            _notificationsEnabled = true;
            utilities.savesettings("startupwarning:1");
        }

        private void tglShowNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            _notificationsEnabled = false;
            utilities.savesettings("startupwarning:0");
        }

        private void GetRegistryStartupApps(ConcurrentBag<StartupApp> apps)
        {
            string[] keys = {
                @"Software\Microsoft\Windows\CurrentVersion\Run",
                @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
            };

            foreach (var keyPath in keys)
            {
                ReadRegKey(Registry.CurrentUser, keyPath, apps);
                ReadRegKey(Registry.LocalMachine, keyPath, apps);
            }
        }

        private void ReadRegKey(RegistryKey root, string subKey, ConcurrentBag<StartupApp> apps)
        {
            try
            {
                using var key = root.OpenSubKey(subKey, false);
                if (key == null) return;

                foreach (var name in key.GetValueNames())
                {
                    try
                    {
                        string path = key.GetValue(name)?.ToString() ?? "";
                        bool enabled = !IsRegStartupDisabled(name, subKey, root);

                        apps.Add(new StartupApp
                        {
                            Name = name,
                            Path = path,
                            ValueName = name,
                            RegistryRoot = root,
                            RegistryPath = subKey,
                            StartupType = root == Registry.CurrentUser ? "Registry (User)" : "Registry (Machine)",
                            IsEnabled = enabled,
                            IsPresent = true,
                            Icon = GetIcon(path),
                            Impact = CalculateImpactAutomatically(path, name)
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void GetTaskSchedulerApps(ConcurrentBag<StartupApp> apps)
        {
            try
            {
                using var ts = new TaskService();
                foreach (var task in ts.AllTasks)
                {
                    try
                    {
                        if (task.Definition.Triggers.Any(tr => tr.TriggerType is TaskTriggerType.Logon or TaskTriggerType.Boot or TaskTriggerType.Registration))
                        {
                            var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();
                            string actionPath = action != null ? $"{action.Path} {action.Arguments}".Trim() : "Task Action";

                            apps.Add(new StartupApp
                            {
                                Name = task.Name,
                                Path = actionPath,
                                ValueName = task.Path,
                                StartupType = "Scheduled Task",
                                IsEnabled = task.Enabled,
                                IsPresent = true,
                                Impact = CalculateImpactAutomatically(actionPath, task.Name),
                                Icon = action != null ? GetIcon(action.Path) : null
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Error]: {task.Name} -> {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TaskService Error]: {ex.Message}");
            }
        }

        private void GetStartupFolderApps(ConcurrentBag<StartupApp> apps)
        {
            string[] folders = { Environment.GetFolderPath(Environment.SpecialFolder.Startup), Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) };

            foreach (var folder in folders)
            {
                try
                {
                    if (!Directory.Exists(folder)) continue;

                    foreach (var file in Directory.GetFiles(folder, "*.lnk"))
                    {
                        try
                        {
                            apps.Add(new StartupApp
                            {
                                Name = Path.GetFileNameWithoutExtension(file),
                                Path = file,
                                ValueName = Path.GetFileName(file),
                                StartupType = "Startup Folder",
                                IsEnabled = true,
                                IsPresent = true,
                                Icon = GetIconFromShortcut(file),
                                Impact = CalculateImpactAutomatically(file, Path.GetFileNameWithoutExtension(file))
                            });
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        private void GetWinlogonEntries(ConcurrentBag<StartupApp> apps)
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon");
                string raw = key?.GetValue("Userinit") as string ?? "";

                foreach (var part in raw.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p) && !p.EndsWith("userinit.exe", StringComparison.OrdinalIgnoreCase)))
                {
                    try
                    {
                        apps.Add(new StartupApp
                        {
                            Name = Path.GetFileNameWithoutExtension(part),
                            Path = part,
                            StartupType = "Userinit (WinLogon)",
                            IsEnabled = System.IO.File.Exists(part),
                            IsPresent = true,
                            Icon = GetIcon(part),
                            Impact = CalculateImpactAutomatically(part, Path.GetFileNameWithoutExtension(part))
                        });
                    }
                    catch { }
                }
            }
            catch { }
        }

        private const long HighThresholdBytes = 40L * 1024 * 1024;
        private const long MediumThresholdBytes = 10L * 1024 * 1024;
        private const long ManagedRuntimeOverheadBytes = 8L * 1024 * 1024;
        private const int StabilityRetries = 3;
        private const int StabilityDelayMs = 150;

        private string CalculateImpactAutomatically(string path, string appName)
        {
            if (string.IsNullOrWhiteSpace(path)) return "Low";

            try
            {
                if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
                {
                    string? target = ResolveShortcutTargetSta(path);
                    if (!string.IsNullOrEmpty(target))
                        path = target;
                }

                string? exePath = ExtractExecutablePath(path);
                if (string.IsNullOrEmpty(exePath)) return "Low";

            
                exePath = Environment.ExpandEnvironmentVariables(exePath);

                if (!System.IO.File.Exists(exePath)) return "Low";

                string fullPathNormalized = Path.GetFullPath(exePath);
                string winDirNormalized = Path.GetFullPath(
                        Environment.GetFolderPath(Environment.SpecialFolder.Windows))
                    .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

                if (fullPathNormalized.StartsWith(winDirNormalized, StringComparison.OrdinalIgnoreCase))
                    return "Low";

                long weight = GetStableImportWeight(fullPathNormalized);

                if (weight > HighThresholdBytes) return "High";
                if (weight > MediumThresholdBytes) return "Medium";
            }
            catch { }

            return "Low";
        }

        private static long GetStableImportWeight(string exePath)
        {
            long previous = -1;
            for (int attempt = 0; attempt < StabilityRetries; attempt++)
            {
                long current = MeasureImportWeightOnce(exePath);
                if (current == previous) return current;
                previous = current;
                if (attempt < StabilityRetries - 1) Thread.Sleep(StabilityDelayMs);
            }
            return previous;
        }

        private static long MeasureImportWeightOnce(string exePath)
        {
            long ownSize = 0;
            try { ownSize = new FileInfo(exePath).Length; } catch { }

            var info = PeImportAnalyzer.Analyze(exePath);

            if (!info.IsValidPe)
                return ownSize;
             
            long weight = ownSize + ResolvePrivateImportWeight(exePath, info);
            if (info.IsManaged) weight += ManagedRuntimeOverheadBytes;
            return weight;
        }
         
        private static long ResolvePrivateImportWeight(string exePath, PeImportAnalyzer.PeInfo rootInfo)
        {
            string? exeDir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(exeDir)) return 0;

            long totalWeight = 0;
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Path.GetFileName(exePath)
    };
            var queue = new Queue<(string DllName, int Depth)>();
            foreach (var dll in rootInfo.ImportedDllNames)
                queue.Enqueue((dll, 0));

            const int maxDepth = 2;
            const int maxScanned = 200;
            int scanned = 0;

            while (queue.Count > 0 && scanned < maxScanned)
            {
                var (dllName, depth) = queue.Dequeue();
                scanned++;

                if (!visited.Add(dllName)) continue;

                string candidatePath = Path.Combine(exeDir, dllName);
                if (!System.IO.File.Exists(candidatePath)) continue; 

                try { totalWeight += new FileInfo(candidatePath).Length; }
                catch { continue; }

                if (depth >= maxDepth) continue;

                var childInfo = PeImportAnalyzer.Analyze(candidatePath);
                if (!childInfo.IsValidPe) continue;

                foreach (var childDll in childInfo.ImportedDllNames)
                    if (!visited.Contains(childDll))
                        queue.Enqueue((childDll, depth + 1));
            }

            return totalWeight;
        }

        private static string? ResolveShortcutTargetSta(string lnkPath)
        {
            string? target = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var shell = new IWshRuntimeLibrary.WshShell();
                    var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(lnkPath);
                    if (!string.IsNullOrEmpty(shortcut.TargetPath))
                        target = shortcut.TargetPath;
                }
                catch { }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return target;
        }

        private static string? ExtractExecutablePath(string commandLine)
        {
            IntPtr argv = NativeMethods.CommandLineToArgvW(commandLine, out int argc);
            if (argv == IntPtr.Zero || argc == 0) return null;
            try
            {
                IntPtr firstArgPtr = Marshal.ReadIntPtr(argv, 0);
                return Marshal.PtrToStringUni(firstArgPtr);
            }
            finally { NativeMethods.LocalFree(argv); }
        }

 
        private static class PeImportAnalyzer
        {
            private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D;
            private const uint IMAGE_NT_SIGNATURE = 0x00004550;
            private const int IMAGE_DIRECTORY_ENTRY_IMPORT = 1;
            private const int IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR = 14; 

            public sealed class PeInfo
            {
                public bool IsValidPe;
                public bool IsManaged;
                public List<string> ImportedDllNames = new();
            }

            public static PeInfo Analyze(string filePath)
            {
                var info = new PeInfo();
                try
                {
                    using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var br = new BinaryReader(fs);

                    if (fs.Length < 0x40) return info;
                    if (br.ReadUInt16() != IMAGE_DOS_SIGNATURE) return info;

                    fs.Seek(0x3C, SeekOrigin.Begin);
                    int peHeaderOffset = br.ReadInt32();
                    if (peHeaderOffset <= 0 || peHeaderOffset >= fs.Length - 4) return info;

                    fs.Seek(peHeaderOffset, SeekOrigin.Begin);
                    if (br.ReadUInt32() != IMAGE_NT_SIGNATURE) return info;

                    br.ReadUInt16();                     
                    ushort numberOfSections = br.ReadUInt16();
                    br.ReadUInt32(); br.ReadUInt32(); br.ReadUInt32(); 
                    ushort sizeOfOptionalHeader = br.ReadUInt16();
                    br.ReadUInt16();                  

                    long optionalHeaderStart = fs.Position;
                    if (sizeOfOptionalHeader == 0) return info;

                    ushort magic = br.ReadUInt16();
                    bool isPe32Plus = magic == 0x20B;
                    if (!isPe32Plus && magic != 0x10B) return info;

                    info.IsValidPe = true;

                    long dataDirectoryOffset = optionalHeaderStart + (isPe32Plus ? 112 : 96);
                    fs.Seek(dataDirectoryOffset, SeekOrigin.Begin);

                    var dataDirectories = new (uint Rva, uint Size)[16];
                    for (int i = 0; i < 16; i++)
                        dataDirectories[i] = (br.ReadUInt32(), br.ReadUInt32());

                    long sectionHeadersOffset = optionalHeaderStart + sizeOfOptionalHeader;
                    fs.Seek(sectionHeadersOffset, SeekOrigin.Begin);

                    var sections = new List<(uint Va, uint Vs, uint RawSize, uint RawPtr)>();
                    for (int i = 0; i < numberOfSections; i++)
                    {
                        fs.Seek(8, SeekOrigin.Current); 
                        uint vs = br.ReadUInt32();
                        uint va = br.ReadUInt32();
                        uint rawSize = br.ReadUInt32();
                        uint rawPtr = br.ReadUInt32();
                        fs.Seek(16, SeekOrigin.Current);
                        sections.Add((va, vs, rawSize, rawPtr));
                    }

                    long RvaToOffset(uint rva)
                    {
                        foreach (var s in sections)
                        {
                            uint size = s.Vs != 0 ? s.Vs : s.RawSize;
                            if (rva >= s.Va && rva < s.Va + size)
                                return s.RawPtr + (rva - s.Va);
                        }
                        return -1;
                    }

                    info.IsManaged = dataDirectories[IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR].Rva != 0;

                    var (importRva, importSize) = dataDirectories[IMAGE_DIRECTORY_ENTRY_IMPORT];
                    if (importRva == 0 || importSize == 0) return info;

                    long importTableOffset = RvaToOffset(importRva);
                    if (importTableOffset < 0) return info;

                    fs.Seek(importTableOffset, SeekOrigin.Begin);
                    while (true)
                    {
                        uint originalFirstThunk = br.ReadUInt32();
                        uint timeDateStamp = br.ReadUInt32();
                        uint forwarderChain = br.ReadUInt32();
                        uint nameRva = br.ReadUInt32();
                        uint firstThunk = br.ReadUInt32();

                        if (originalFirstThunk == 0 && timeDateStamp == 0 && forwarderChain == 0
                            && nameRva == 0 && firstThunk == 0)
                            break;

                        if (nameRva != 0)
                        {
                            long nameOffset = RvaToOffset(nameRva);
                            if (nameOffset >= 0)
                            {
                                long returnPos = fs.Position;
                                fs.Seek(nameOffset, SeekOrigin.Begin);
                                string dllName = ReadAsciiZ(br);
                                if (!string.IsNullOrEmpty(dllName))
                                    info.ImportedDllNames.Add(dllName);
                                fs.Seek(returnPos, SeekOrigin.Begin);
                            }
                        }

                        if (info.ImportedDllNames.Count > 256) break;
                    }
                }
                catch {  }

                return info;
            }

            private static string ReadAsciiZ(BinaryReader br)
            {
                var bytes = new List<byte>(64);
                while (true)
                {
                    byte b = br.ReadByte();
                    if (b == 0) break;
                    bytes.Add(b);
                    if (bytes.Count > 260) break;
                }
                return Encoding.ASCII.GetString(bytes.ToArray());
            }
        }

        private static class NativeMethods
        {
            [DllImport("shell32.dll", SetLastError = true)]
            public static extern IntPtr CommandLineToArgvW(
                [MarshalAs(UnmanagedType.LPWStr)] string cmdLine, out int numArgs);

            [DllImport("kernel32.dll")]
            public static extern IntPtr LocalFree(IntPtr hMem);
        }

        private BitmapImage? GetIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            string exe = path.Trim('"').Split(' ')[0];

            if (IconCache.TryGetValue(exe, out var cachedIcon)) return cachedIcon;
            if (!System.IO.File.Exists(exe)) return null;

            try
            {
                var sysIcon = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                using var ms = new MemoryStream();
                sysIcon!.ToBitmap().Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.StreamSource = ms;
                bmp.EndInit();
                bmp.Freeze();

                IconCache[exe] = bmp;
                return bmp;
            }
            catch { return null; }
        }

        private BitmapImage? GetIconFromShortcut(string shortcut)
        {
            try { return GetIcon(((IWshShortcut)new WshShell().CreateShortcut(shortcut)).TargetPath); } catch { return null; }
        }

        private bool IsRegStartupDisabled(string name, string runSubKey, RegistryKey root)
        {
            try
            {
                string approvedKey = runSubKey.Replace(@"Run", @"Explorer\StartupApproved\Run");
                using var key = root.OpenSubKey(approvedKey, false);
                return key?.GetValue(name) is byte[] data && data.Length > 0 && (data[0] & 1) != 0;
            }
            catch { return false; }
        }

        private void CheckPendingChanges() => btnApply.IsEnabled = StartupApps.Any(a => a.HasPendingChange);

        private bool isMaximized = false;
        private double previousWidth, previousHeight, previousLeft, previousTop;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isMaximized)
            {
                WindowState = WindowState.Normal;
                Width = previousWidth;
                Height = previousHeight;
                Left = previousLeft;
                Top = previousTop;
                isMaximized = false;
            }
            else
            {
                previousWidth = Width;
                previousHeight = Height;
                previousLeft = Left;
                previousTop = Top;

                WindowState = WindowState.Normal;
                Left = SystemParameters.WorkArea.Left;
                Top = SystemParameters.WorkArea.Top;
                Width = SystemParameters.WorkArea.Width;
                Height = SystemParameters.WorkArea.Height;
                isMaximized = true;
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in StartupApps.Where(a => a.HasPendingChange))
                item.ApplyPendingChange();

            MessageBox.Show("Changes applied successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            CheckPendingChanges();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedApp == null) return;
            if (MessageBox.Show($"Are you sure you want to remove '{SelectedApp.Name}'?", "Confirm Removal", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                SelectedApp.Remove();
                StartupApps.Remove(SelectedApp);
            }
        }

        private void StartupAppsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => SelectedApp = StartupAppsDataGrid.SelectedItem as StartupApp;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Visibility = Visibility.Collapsed;
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
        private void AddButton_Click(object sender, RoutedEventArgs e) { }
        private void AddOperationButton_Click(object sender, RoutedEventArgs e) { }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string p = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));

        private Visibility _warningVisibility = Visibility.Collapsed;
        public Visibility WarningVisibility
        {
            get => _warningVisibility;
            set { _warningVisibility = value; OnPropertyChanged(); }
        }

        private string _warningMessage = "";
        public string WarningMessage
        {
            get => _warningMessage;
            set { _warningMessage = value; OnPropertyChanged(); }
        }

        private DispatcherTimer? _warningTimer;
         
        private void ShowInAppWarning(string message)
        {
            Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                WarningMessage = message;
                WarningVisibility = Visibility.Visible;

                if (_warningTimer == null)
                {
                    _warningTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
                    _warningTimer.Tick += (s, e) =>
                    {
                        WarningVisibility = Visibility.Collapsed;
                        _warningTimer.Stop();
                    };
                }

                _warningTimer.Stop();
                _warningTimer.Start();
            }), DispatcherPriority.Normal);
        }

        private void CloseWarning_Click(object sender, RoutedEventArgs e)
        {
            WarningVisibility = Visibility.Collapsed;
            _warningTimer?.Stop();
        }

        public class StartupApp : INotifyPropertyChanged
        {
            public string UniqueId => $"{StartupType}|{Name}|{Path}";

            public BitmapImage? Icon { get; set; }
            public string Name { get; set; } = "";
            public string Path { get; set; } = "";
            public string ValueName { get; set; } = "";
            public string StartupType { get; set; } = "";
            public RegistryKey? RegistryRoot { get; set; }
            public string RegistryPath { get; set; } = "";

            private bool _isEnabled, _isPresent, _pendingEnabled;
            public bool IsPresent { get => _isPresent; set { _isPresent = value; OnPropertyChanged(); OnPropertyChanged(nameof(Status)); OnPropertyChanged(nameof(IsCheckboxEnabled)); } }
            public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; _pendingEnabled = value; OnPropertyChanged(); OnPropertyChanged(nameof(Status)); } }
            public bool IsCheckboxEnabled => IsPresent;
            public string Impact { get; set; } = "Low";
            public int ImpactScore => Impact == "High" ? 3 : (Impact == "Medium" ? 2 : 1);
          
            public Action<string>? HighImpactWarningAction { get; set; }

            public bool PendingEnabled
            {
                get => _pendingEnabled;
                set
                {
                    _pendingEnabled = value; 
                    if (value == true && Impact == "High")
                    {
                        HighImpactWarningAction?.Invoke($"Warning: '{Name}' may consume significant system resources and slow down your startup time.");
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasPendingChange));
                }
            }

            public bool HasPendingChange => _isEnabled != _pendingEnabled;

            public string Status
            {
                get => !IsPresent ? "Removed" : (IsEnabled ? "Enabled" : "Disabled");
                set
                {
                    if (value == Status) return;

                    switch (value)
                    {
                        case "Enabled":
                            IsPresent = true;
                            IsEnabled = true;
                            break;
                        case "Disabled":
                            IsPresent = true;
                            IsEnabled = false;
                            break;
                        case "Removed":
                            IsPresent = false;
                            break;
                        default:
                            if (bool.TryParse(value, out var b))
                            {
                                IsPresent = true;
                                IsEnabled = b;
                            }
                            break;
                    }
                    OnPropertyChanged(nameof(Status));
                }
            }

            public void ApplyPendingChange()
            {
                if (!HasPendingChange) return;
                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        string appKey = RegistryPath.Replace(@"Run", @"Explorer\StartupApproved\Run");
                        using var key = RegistryRoot.CreateSubKey(appKey, true);
                        key?.SetValue(ValueName, new byte[] { (byte)(_pendingEnabled ? 0x02 : 0x03), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, RegistryValueKind.Binary);
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        using var ts = new TaskService();
                        var task = ts.GetTask(ValueName);
                        if (task != null) task.Enabled = _pendingEnabled;
                    }
                    IsEnabled = _pendingEnabled;
                }
                catch (Exception ex) { MessageBox.Show($"Error applying changes: {ex.Message}"); }
            }

            public void Remove()
            {
                try
                {
                    if (StartupType.StartsWith("Registry") && RegistryRoot != null)
                    {
                        using var key = RegistryRoot.OpenSubKey(RegistryPath, true);
                        key?.DeleteValue(ValueName, false);
                    }
                    else if (StartupType == "Scheduled Task")
                    {
                        using var ts = new TaskService();
                        var task = ts.GetTask(ValueName);
                        if (task != null)
                        {
                            task.Folder.DeleteTask(task.Name);
                        }
                    }
                    else if (StartupType == "Startup Folder" && System.IO.File.Exists(Path))
                    {
                        System.IO.File.Delete(Path);
                    }
                    IsPresent = false;
                }
                catch { }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string p = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }
    }
}
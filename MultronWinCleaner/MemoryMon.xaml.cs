using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for MemoryMon.xaml
    /// </summary>
    public partial class MemoryMon : Window
    {
        private readonly MemCleaner _memcleaner;
        private bool _processesVisible = false;
        private readonly ObservableCollection<ProcessItem> _processes = new ObservableCollection<ProcessItem>();
        private readonly DispatcherTimer _updateTimer;
        private bool _isUpdatingProcesses = false;

      
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private readonly Dictionary<int, TimeSpan> _lastCpuTimes = new Dictionary<int, TimeSpan>();
        private DateTime _lastCpuCheckTime = DateTime.Now;
        private readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public MemoryMon(MemCleaner memcleaner)
        {
            InitializeComponent();
            _memcleaner = memcleaner;

            Loaded += MemoryMon_Loaded;
            Closed += MemoryMon_Closed;

            ProcessesListView.ItemsSource = _processes;
             
            var statusMonitor = new SystemStatusMonitor(this, _cts.Token);
            _ = Task.Run(() => statusMonitor.RunAsync());

            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _updateTimer.Tick += UpdateTimer_Tick;

            Topmost = !_memcleaner.chkTopMostMonitor.IsEnabled;
        }

        public class ProcessItem
        {
            public ImageSource Icon { get; set; }
            public string Name { get; set; }
            public string Cpu { get; set; }
            public string Ram { get; set; }
        }

        public class SystemStatusMonitor
        {
            private readonly MemoryMon _window;
            private readonly CancellationToken _cancellationToken;
            private double _lastRamPercent = -1;
            private int _lastCpuValue = -1;

            private ulong _prevIdleTime = 0;
            private ulong _prevKernelTime = 0;
            private ulong _prevUserTime = 0;

            [StructLayout(LayoutKind.Sequential)]
            private struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

            public SystemStatusMonitor(MemoryMon window, CancellationToken cancellationToken)
            {
                _window = window;
                _cancellationToken = cancellationToken;
                UpdateCpuTimes(out _prevIdleTime, out _prevKernelTime, out _prevUserTime);
            }

            private void UpdateCpuTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime)
            {
                GetSystemTimes(out FILETIME idle, out FILETIME kernel, out FILETIME user);
                idleTime = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime;
                kernelTime = ((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime;
                userTime = ((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime;
            }

            private int CalculateCpuUsage()
            {
                UpdateCpuTimes(out ulong idleTime, out ulong kernelTime, out ulong userTime);

                ulong sysIdle = idleTime - _prevIdleTime;
                ulong sysKernel = kernelTime - _prevKernelTime;
                ulong sysUser = userTime - _prevUserTime;
                ulong sysTotal = sysKernel + sysUser;

                _prevIdleTime = idleTime;
                _prevKernelTime = kernelTime;
                _prevUserTime = userTime;

                if (sysTotal == 0) return 0;

                ulong sysUsed = sysTotal - sysIdle;
                double cpuUsage = (sysUsed * 100.0) / sysTotal;
                return (int)Math.Round(Math.Max(0, Math.Min(100, cpuUsage)));
            }

            public async Task RunAsync()
            {
                var computerInfo = new ComputerInfo();

                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        if (_window.Dispatcher.HasShutdownStarted || _window.Dispatcher.HasShutdownFinished)
                            break;

                        ulong totalMemory = computerInfo.TotalPhysicalMemory;
                        ulong availableMemory = computerInfo.AvailablePhysicalMemory;
                        ulong usedMemory = totalMemory - availableMemory;
                        double ramPercent = Math.Round((usedMemory * 100.0) / totalMemory, 1);

                        int cpuValue = CalculateCpuUsage();

                        if (_window.Dispatcher.HasShutdownStarted || _window.Dispatcher.HasShutdownFinished)
                            break;

                        await _window.Dispatcher.InvokeAsync(() =>
                        {
                            if (Math.Abs(ramPercent - _lastRamPercent) > 0.1)
                            {
                                _window.RamBar.Minimum = 0;
                                _window.RamBar.Maximum = 100;
                                _window.RamBar.Value = ramPercent;
                                _window.RamPercentText.Text = $"{ramPercent:F1}%";
                                _lastRamPercent = ramPercent;
                            }

                            if (cpuValue != _lastCpuValue)
                            {
                                _window.CpuBar.Minimum = 0;
                                _window.CpuBar.Maximum = 100;
                                _window.CpuBar.Value = cpuValue;
                                _window.CpuPercentText.Text = $"{cpuValue}%";
                                _lastCpuValue = cpuValue;
                            }
                        });

                        await Task.Delay(200, _cancellationToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SystemStatusMonitor] Hata: {ex.Message}");
                        await Task.Delay(1000, _cancellationToken);
                    }
                }
            }
        }

        private void MemoryMon_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSavedLocation();
        }

        private void LoadSavedLocation()
        {
            bool locationLoaded = false;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\MultronWinCleaner"))
                {
                    if (key != null)
                    {
                        var leftValue = key.GetValue("MemMonLeft");
                        var topValue = key.GetValue("MemMonTop");

                        if (leftValue != null && topValue != null)
                        { 
                            if (double.TryParse(leftValue.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double left) &&
                                double.TryParse(topValue.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double top))
                            { 
                                var workArea = SystemParameters.WorkArea;
                                if (left + 50 < workArea.Right && top + 50 < workArea.Bottom)
                                {
                                    Left = left;
                                    Top = top;
                                    locationLoaded = true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            if (!locationLoaded)
            {
                var workingArea = SystemParameters.WorkArea;
                 
                double currentWidth = double.IsNaN(Width) ? ActualWidth : Width;
                double currentHeight = double.IsNaN(Height) ? ActualHeight : Height;

                Left = (workingArea.Width - currentWidth) / 2 + workingArea.Left;
                Top = (workingArea.Height - currentHeight) / 2 + workingArea.Top;
            }
        }

        private void SaveCurrentLocation()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\MultronWinCleaner"))
                { 
                    key.SetValue("MemMonLeft", Left.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    key.SetValue("MemMonTop", Top.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
            }
            catch { }
        }

        private async void CleanMemory_Click(object sender, RoutedEventArgs e)
        {
            if (_memcleaner != null)
            {
                await _memcleaner.cleanmemory();
            }
        }

        private void Window_DragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
                SaveCurrentLocation();
            }
        }

        private void SlowProcesses_Click(object sender, RoutedEventArgs e)
        {
            if (!_processesVisible)
            {
                ProcessesListView.Visibility = Visibility.Collapsed;
                LoadingBorder.Visibility = Visibility.Visible;

                _processesVisible = true;
                _updateTimer.Start();
                UpdateTimer_Tick(null, null);
            }
            else
            {
                _updateTimer.Stop();
                ProcessesListView.Visibility = Visibility.Collapsed;
                LoadingBorder.Visibility = Visibility.Collapsed;
                _processesVisible = false;
            }
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingProcesses || Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
            _isUpdatingProcesses = true;

            try
            {
                await Task.Run(() =>
                {
                    var now = DateTime.Now;
                    var timeDifference = now - _lastCpuCheckTime;
                    if (timeDifference.TotalMilliseconds <= 0) return;

                    var topProcs = Process.GetProcesses()
                        .OrderByDescending(p => p.WorkingSet64)
                        .Take(15)
                        .ToList();

                    var processList = new List<ProcessItem>();

                    foreach (var p in topProcs)
                    {
                        double cpuUsage = 0;
                        string processName = p.ProcessName;
                        long ramUsage = 0;

                        try { ramUsage = p.WorkingSet64; } catch { }

                        try
                        {
                            var cpuTime = p.TotalProcessorTime;
                            if (_lastCpuTimes.TryGetValue(p.Id, out var prevCpuTime))
                            {
                                var diff = cpuTime - prevCpuTime;
                                cpuUsage = (diff.TotalMilliseconds / timeDifference.TotalMilliseconds) / Environment.ProcessorCount * 100;
                            }
                            _lastCpuTimes[p.Id] = cpuTime;
                        }
                        catch { }

                        ImageSource icon = GetProcessIcon(p, processName);

                        processList.Add(new ProcessItem
                        {
                            Icon = icon,
                            Name = processName,
                            Cpu = $"{Math.Max(0, Math.Round(cpuUsage, 1))}%",
                            Ram = $"{ramUsage / 1024 / 1024} MB"
                        });
                    }

                    _lastCpuCheckTime = now;

                    var currentIds = topProcs.Select(x => x.Id).ToHashSet();
                    var keysToRemove = _lastCpuTimes.Keys.Where(k => !currentIds.Contains(k)).ToList();
                    foreach (var k in keysToRemove) _lastCpuTimes.Remove(k);

                    if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;

                    Dispatcher.Invoke(() =>
                    {
                        _processes.Clear();
                        foreach (var item in processList)
                        {
                            _processes.Add(item);
                        }

                        LoadingBorder.Visibility = Visibility.Collapsed;
                        ProcessesListView.Visibility = Visibility.Visible;
                    });
                });
            }
            catch { }
            finally
            {
                _isUpdatingProcesses = false;
            }
        }

        private ImageSource GetProcessIcon(Process p, string processName)
        {
            if (_iconCache.TryGetValue(processName, out var cachedIcon))
            {
                return cachedIcon;
            }

            string path = null;
            try { path = p.MainModule?.FileName; } catch { }

            ImageSource iconSource = null;

            try
            {
                System.Drawing.Icon ico = null;

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    try { ico = System.Drawing.Icon.ExtractAssociatedIcon(path); } catch { }
                }

                if (ico == null)
                {
                    string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                    try { ico = System.Drawing.Icon.ExtractAssociatedIcon(defaultPath); } catch { }
                }

                if (ico != null)
                {
                    IntPtr hIcon = ico.Handle;
                    var bmpSource = Imaging.CreateBitmapSourceFromHIcon(
                        hIcon,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());

                    bmpSource.Freeze();
                    DestroyIcon(hIcon);
                    ico.Dispose();

                    iconSource = bmpSource;
                }
            }
            catch { }

            if (iconSource != null)
            {
                _iconCache[processName] = iconSource;
            }

            return iconSource;
        }

        
        private void MemoryMon_Closed(object sender, EventArgs e)
        {
            _cts?.Cancel();

            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Tick -= UpdateTimer_Tick; 
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _updateTimer?.Stop();
            Close();

            if (_memcleaner?.chkSmartRAM != null)
            {
                _memcleaner.chkSmartRAM.IsChecked = false;
            }
        }
    }
}
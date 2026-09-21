using Microsoft.Win32;
using Multron_Win_Cleaner;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
 

namespace MultronWinCleaner
{
    public partial class MemCleaner : Window
    {
     
        private const uint PROCESS_SET_QUOTA = 0x0100;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        #region Native Methods (Windows API)
        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("psapi.dll")]
        static extern bool EmptyWorkingSet(IntPtr hProcess);
 

        [DllImport("kernel32.dll", SetLastError = true)]
   
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetPerformanceInfo(out PERFORMANCE_INFORMATION pPerformanceInformation, int size);
        [DllImport("kernel32.dll", SetLastError = true)]

        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);
        [StructLayout(LayoutKind.Sequential)]
        struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PERFORMANCE_INFORMATION
        {
            public uint cb;
            public ulong CommitTotal;
            public ulong CommitLimit;
            public ulong CommitPeak;
            public ulong PhysicalTotal;
            public ulong PhysicalAvailable;
            public ulong SystemCache;
            public ulong KernelTotal;
            public ulong KernelPaged;
            public ulong KernelNonpaged;
            public ulong PageSize;
            public uint HandleCount;
            public uint ProcessCount;
            public uint ThreadCount;
        }

        public static ulong GetAvailablePhysicalMemory()
        {
            PERFORMANCE_INFORMATION pi = new PERFORMANCE_INFORMATION();
            pi.cb = (uint)Marshal.SizeOf(typeof(PERFORMANCE_INFORMATION));

            if (GetPerformanceInfo(out pi, Marshal.SizeOf(typeof(PERFORMANCE_INFORMATION))))
            {
                return pi.PhysicalAvailable * pi.PageSize;
            }
            return 0;
        }
        #endregion

        public MemoryMon memorymon;
        MainWindow window;
        AutoCleanMem autocleanmem;

        int sendnotify = 0;
     
        private PerformanceCounter cpuCounter;

        public MemCleaner(MainWindow window)
        {
            InitializeComponent();

            this.window = window;
     



        }
        private void btnResetWidgetLocation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                        using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\MultronWinCleaner", true))
                        {
                            if (key != null)
                            {
                                key.DeleteValue("MemMonLeft", false);
                                key.DeleteValue("MemMonTop", false);
                            }
                        }
 
             
                    
                        memorymon.Close();
                        memorymon = new MemoryMon(this);
                        memorymon.Show();
                        memorymon.Activate();
                         
                     
              
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error resetting location: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            memorymon = new MemoryMon(this);
            if(this.chkSmartRAM.IsChecked == true)
            {
                memorymon.Show();
                if(chkTopMostMonitor.IsChecked == true)
                {
                    memorymon.Topmost = true;
                }
            }
            autocleanmem = new AutoCleanMem(this);
            InitializeSystemInfo();
            UpdateSystemStats();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
         

        }
        private void InitializeSystemInfo()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

                using RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                CpuNameText.Text = key?.GetValue("ProcessorNameString")?.ToString() ?? "Unknown CPU";
            }
            catch (Exception)
            {
                cpuCounter = null;
                CpuNameText.Text = "CPU Information Unavailable";
            }
        }

    
        public void UpdateSystemStats()
        {
            try
            {
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                ulong totalMemory = computerInfo.TotalPhysicalMemory;
                ulong availableMemory = computerInfo.AvailablePhysicalMemory;
                ulong usedMemory = totalMemory - availableMemory;

                double usedPercent = (double)usedMemory * 100 / totalMemory;
                RamUsagePercentText.Text = $"{usedPercent:F0}%";

                double totalGb = totalMemory / (1024.0 * 1024.0 * 1024.0);
                double usedGb = usedMemory / (1024.0 * 1024.0 * 1024.0);
                RamInfoText.Text = $"RAM: {usedGb:F1} GB / {totalGb:F1} GB";

                if (cpuCounter != null)
                {
                    int currentCpuUsage = (int)cpuCounter.NextValue();
                    CpuUsageText.Text = $"CPU Load: %{currentCpuUsage}";
                }

                UpdateArcGraphic(usedPercent);
            }
            catch
            {
                
            }
        }

        private void UpdateArcGraphic(double usedPercent)
        {
            double stroke = 10;
            double size = 150;
            double radius = (size / 2.0) - (stroke / 2.0);
            double center = size / 2.0;

            double startAngle = -90;
            double sweepAngle = usedPercent * 360.0 / 100.0;

            sweepAngle = Math.Clamp(sweepAngle, 0.01, 359.99);
            double endAngle = startAngle + sweepAngle;

            double startRadians = Math.PI * startAngle / 180.0;
            double endRadians = Math.PI * endAngle / 180.0;

            Point startPoint = new Point(center + radius * Math.Cos(startRadians), center + radius * Math.Sin(startRadians));
            Point endPoint = new Point(center + radius * Math.Cos(endRadians), center + radius * Math.Sin(endRadians));

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false,
                Segments = new PathSegmentCollection
                {
                    new ArcSegment
                    {
                        Point = endPoint,
                        Size = new Size(radius, radius),
                        IsLargeArc = sweepAngle > 180,
                        SweepDirection = SweepDirection.Clockwise
                    }
                }
            };

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            ArcPath.Stretch = Stretch.None;
            ArcPath.RenderTransform = new TranslateTransform(0, 0);
            ArcPath.Data = geometry;
        }
 
        public async Task cleanmemory()
        {
            CleanButton.IsEnabled = false;
            CleanButton.Content = "Cleaning...";
            CleanedMemoryLabel.Text = "";

            bool isDeepClean = rbDeepClean.IsChecked == true;
            bool isAggressiveClean = rbDeepCleanAggresive.IsChecked == true;
            bool clearTempFiles = chkClearTemp.IsChecked == true;
            bool clearClipboardData = chkClearClipboard.IsChecked == true;

            long beforeFree = (long)GetAvailablePhysicalMemory();

            if (clearClipboardData)
            {
                try { Clipboard.Clear(); } catch {   }
            }

            await Task.Run(() =>
            {
                if (isAggressiveClean)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        foreach (Process proc in Process.GetProcesses())
                        {
                            try
                            {
                                if (!proc.HasExited && proc.ProcessName != "System" && proc.ProcessName != "Idle")
                                {
                                    IntPtr handle = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, proc.Id);

                                    if (handle != IntPtr.Zero)
                                    {
                                        try
                                        {
                                            EmptyWorkingSet(handle);
                                            SetProcessWorkingSetSize(handle, (IntPtr)(-1), (IntPtr)(-1));
                                        }
                                        finally
                                        {
                                            CloseHandle(handle);
                                        }
                                    }
                                }
                            }
                            catch
                            {

                            }
                            finally
                            {
                                proc.Dispose();
                            }
                        }
                    }
                }
                else if(isDeepClean)
                {
                    foreach (Process proc in Process.GetProcesses())
                    {
                        try
                        {
                            if (!proc.HasExited && proc.ProcessName != "System" && proc.ProcessName != "Idle")
                            {
                                IntPtr handle = OpenProcess(PROCESS_SET_QUOTA | PROCESS_QUERY_INFORMATION, false, proc.Id);

                                if (handle != IntPtr.Zero)
                                {
                                    try
                                    {
                                        EmptyWorkingSet(handle);
                                        SetProcessWorkingSetSize(handle, (IntPtr)(-1), (IntPtr)(-1));
                                    }
                                    finally
                                    {
                                        CloseHandle(handle);
                                    }
                                }
                            }
                        }
                        catch
                        {

                        }
                        finally
                        {
                            proc.Dispose();
                        }
                    }
                }
                {
                    try
                    {
                        using Process currentProc = Process.GetCurrentProcess();
                        EmptyWorkingSet(currentProc.Handle);
                    }
                    catch { }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (clearTempFiles)
                {
                    CleanWindowsTemp();
                }
            });

            long afterFree = (long)GetAvailablePhysicalMemory();
            long freed = Math.Max(0, afterFree - beforeFree);
            double mbFreed = freed / (1024.0 * 1024.0);

            string freedMessage = $"Freed {mbFreed:F2} MB {DateTime.Now:T}";
            CleanedMemoryLabel.Text = freedMessage;

            if (memorymon != null)
                memorymon.FreedLabel.Text = freedMessage;

            CleanButton.Content = "Clean Memory Now";
            CleanButton.IsEnabled = true;
             
            UpdateSystemStats();

            if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
            {
                Notify notify = new Notify("Memory Cleaned", "Your system memory has been optimized successfully.\r\n", $"Freed {mbFreed:F2} MB");
                notify.Show();
            }
        }

        private void CleanWindowsTemp()
        {
            string tempPath = Path.GetTempPath();
            DirectoryInfo di = new DirectoryInfo(tempPath);

            if (!di.Exists) return;

            foreach (FileInfo file in di.GetFiles())
            {
                try { file.Delete(); } catch {   }
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                try { dir.Delete(true); } catch {   }
            }
        }

        #region Window & UI Events

        private async void CleanMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            await cleanmemory();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Visibility = Visibility.Collapsed;
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void WindowDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void cbCleanInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (autocleanmem == null) return;
            autocleanmem.StopAutoClean();
            if (chkEnableAutoClean.IsChecked == true)
            {
                autocleanmem.StartAutoClean();
            }
        }

        private void chkTopMostMonitor_Checked(object sender, RoutedEventArgs e) { window.utilities.savesettings("topmost:1"); if (memorymon != null) memorymon.Topmost = true; }
        private void chkTopMostMonitor_Unchecked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("topmost:0"); if (memorymon != null) memorymon.Topmost = false; }
        private void chkSmartRAM_Checked(object sender, RoutedEventArgs e) {  window.utilities.savesettings("mon:1"); memorymon = new MemoryMon(this); memorymon.Show(); }
        private void chkSmartRAM_Unchecked(object sender, RoutedEventArgs e) { memorymon?.Close(); window.utilities.savesettings("mon:0"); }
        private void chkEnableAutoClean_Checked(object sender, RoutedEventArgs e) { window.utilities.savesettings("automemclean:1"); autocleanmem?.StartAutoClean(); }
        private void chkEnableAutoClean_UnChecked(object sender, RoutedEventArgs e) { window.utilities.savesettings("automemclean:0"); autocleanmem?.StopAutoClean(); }
        private void chkShowNotification_Checked(object sender, RoutedEventArgs e) { window.utilities.savesettings("sendnotify:1"); sendnotify = 1; }
        private void chkShowNotification_UnChecked(object sender, RoutedEventArgs e) { window.utilities.savesettings("sendnotify:0"); sendnotify = 0; }
        private void chkSkipOnLowBattery_Checked(object sender, RoutedEventArgs e) { window.utilities.savesettings("skipiflow:1"); }
        private void chkSkipOnLowBattery_UnChecked(object sender, RoutedEventArgs e) { window.utilities.savesettings("skipiflow:0"); }
        private void chkClearTemp_Checked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("cleartemp:1"); }
        private void chkClearTemp_Unchecked(object sender, RoutedEventArgs e)  { if (window != null) window.utilities.savesettings("cleartemp:0"); }
        private void chkClearClipboard_Checked(object sender, RoutedEventArgs e)  { if (window != null) window.utilities.savesettings("clearclip:1");   }
        private void chkClearClipboard_Unchecked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("clearclip:0"); }
        private void rbDeepClean_Checked(object sender, RoutedEventArgs e)   { if (window != null) window.utilities.savesettings("deepclean:1"); }
        private void rbDeepClean_UnChecked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("deepclean:0"); }
        private void rbDeepCleanAgressive_Checked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("aggdeepclean:1"); }
        private void rbDeepCleanAgressive_UnChecked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("aggdeepclean:0"); }
 
        private void chkStartWithWinCleaner_Checked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("startwithwincleaner:1"); }
        private void chkStartWithWinCleaner_UnChecked(object sender, RoutedEventArgs e) { if (window != null) window.utilities.savesettings("startwithwincleaner:0"); }

        #endregion

        #region AutoClean Class
        public class AutoCleanMem
        {
            private readonly MemCleaner memcleaner;
            private CancellationTokenSource _autoCleanCts;

            public AutoCleanMem(MemCleaner memcleaner)
            {
                this.memcleaner = memcleaner;
            }

            public async void StartAutoClean()
            {
                _autoCleanCts?.Cancel();
                _autoCleanCts = new CancellationTokenSource();
                var token = _autoCleanCts.Token;

                int intervalMinutes = 5;

                await memcleaner.Dispatcher.InvokeAsync(() =>
                {
                    if (memcleaner.cbCleanInterval.SelectedItem is ComboBoxItem selectedItem)
                    {
                        string content = selectedItem.Content?.ToString()?.ToLower() ?? "";
                        intervalMinutes = content switch
                        {
                            "1 minute" => 1,
                            "10 minutes" => 10,
                            "15 minutes" => 15,
                            "30 minutes" => 30,
                            "1 hour" => 60,
                            "2 hours" => 120,
                            _ => 5
                        };
                    }
                });

                memcleaner.window.utilities.savesettings($"mcminutes:{intervalMinutes} minutes");

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        for (int i = intervalMinutes * 60; i > 0; i--)
                        {
                            token.ThrowIfCancellationRequested();

                            int mins = i / 60;
                            int secs = i % 60;

                            memcleaner.Dispatcher.Invoke(() =>
                            {
                                memcleaner.Status.Text = $"Next clean: {mins:D2}:{secs:D2}";
                            });

                            await Task.Delay(1000, token);
                        }

                        bool isSkipChecked = await memcleaner.Dispatcher.InvokeAsync(() => memcleaner.chkSkipOnLowBattery.IsChecked == true);

                        if (isSkipChecked)
                        {
                            int battery = GetBatteryPercent();
                            if (battery == -1)
                            {
                                memcleaner.Dispatcher.Invoke(() => memcleaner.Status.Text = "Battery unknown, skipping...");
                                await Task.Delay(5000, token);
                            }
                            else if (battery <= 30)
                            {
                                memcleaner.Dispatcher.Invoke(() => memcleaner.Status.Text = "Skipping clean (Low battery).");
                                await Task.Delay(5000, token);
                                continue;
                            }
                        }

                        await memcleaner.Dispatcher.InvokeAsync(() => memcleaner.cleanmemory());
                    }
                }
                catch (OperationCanceledException)
                {
                  
                }
            }

            public void StopAutoClean()
            {
                if (_autoCleanCts != null)
                {
                    _autoCleanCts.Cancel();
                    _autoCleanCts.Dispose();
                    _autoCleanCts = null;
                }

                memcleaner.Dispatcher.Invoke(() =>
                {
                    memcleaner.Status.Text = "Auto clean stopped.";
                });
            }

            public int GetBatteryPercent()
            {
                GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);
                return status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
            }
        }
        #endregion
    }
}
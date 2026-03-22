using Multron_Win_Cleaner;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MultronWinCleaner
{
  
    public partial class MemCleaner : Window
    {

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        [DllImport("kernel32.dll")]
        static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

        [DllImport("kernel32.dll")]
        static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);
        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

   
        static extern bool SetProcessWorkingSetSize(IntPtr procHandle, int minSize, int maxSize);
 

        [StructLayout(LayoutKind.Sequential)]
        struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
 

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
        MemoryMon memorymon;
        MainWindow window;
        int sendnotify = 0;
        public MemCleaner(MainWindow window)
        {
            InitializeComponent();
            memorymon = new MemoryMon(this);
            MemoryUsage usage = new MemoryUsage(this);
            Thread t = new Thread(usage.run);
            t.Start();
            this.window = window;
            originalHeight = this.Height;

        }
       
        private void chkTopMostMonitor_Checked(object sender, RoutedEventArgs e)
        {

            window.utilities.savesettings("topmost:1");
            memorymon.Topmost = true;
        }

        private void chkTopMostMonitor_Unchecked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("topmost:0");
            memorymon.Topmost = false;
        }
        public static class NativeMethods
{
    [DllImport("psapi.dll", SetLastError = true)]
    public static extern bool GetPerformanceInfo(out PERFORMANCE_INFORMATION pPerformanceInformation, int size);

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
}
        public class AutoCleanMem
        {
            private MemCleaner memcleaner;
            private System.Threading.Timer? timer;

            public AutoCleanMem(MemCleaner memcleaner)
            {
                this.memcleaner = memcleaner;
            }

            public void StartAutoClean()
            {
                memcleaner.Dispatcher.Invoke(new Action(() =>
                {
                    var selectedItem = memcleaner.cbCleanInterval.SelectedItem as ComboBoxItem;
                    if (selectedItem == null) return;

                    string content = selectedItem.Content.ToString()?.ToLower() ?? "";

                    int intervalMinutes = content switch
                    {
                        "1 minutes" => 1,
                        "5 minutes" => 5,
                        "10 minutes" => 10,
                        "15 minutes" => 15,
                        "30 minutes" => 30,
                        "1 hour" => 60,
                        "2 hour" => 120,
                        _ => 5
                    };

                    timer = new System.Threading.Timer(_ =>
                    {
                        memcleaner.Dispatcher.Invoke(() =>
                        {
                            if(memcleaner.chkSkipOnLowBattery.IsChecked == true)
                            {
                                if (GetBatteryPercent() > 30)
                                {
                                    memcleaner.cleanmemory();
                                }
                            }
                            

                         
                        });
                    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
                }));
            }
            public int GetBatteryPercent()
            {
                GetSystemPowerStatus(out SYSTEM_POWER_STATUS status);

                if (status.BatteryLifePercent == 255) return -1;
                return status.BatteryLifePercent;
            }
            public void StopAutoClean()
            {
                timer?.Dispose();
                timer = null;
            }
        }
    
        public class MemoryUsage
        {
            MemCleaner memcleaner;
            public MemoryUsage(MemCleaner memcleaner)
            {
                 this.memcleaner = memcleaner;
            }
            public void run()
            {
                while(true)
                {
                    UpdateRamUsage();
                    Thread.Sleep(100);
                }
            }
            private void UpdateRamUsage()
            {
                memcleaner.Dispatcher.Invoke(() =>
                {
                    
                    var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                    ulong totalMemory = computerInfo.TotalPhysicalMemory;
                    ulong availableMemory = computerInfo.AvailablePhysicalMemory;

                    ulong usedMemory = totalMemory - availableMemory;
                    double usedPercent = usedMemory * 100.0 / totalMemory;
                     
                    memcleaner.RamUsagePercentText.Text = $"{usedPercent:F1}%";
                     
                    double stroke = 16;
                    double size = 210;
                    double radius = (size / 2.0) - (stroke / 2.0);
                    double center = size / 2.0;
                     
                    double startAngle = -90;
                    double sweepAngle = usedPercent * 360.0 / 100.0;
                    double endAngle = startAngle + sweepAngle;
                     
                    double startRadians = Math.PI * startAngle / 180.0;
                    double endRadians = Math.PI * endAngle / 180.0;

                    Point startPoint = new Point(
                        center + radius * Math.Cos(startRadians),
                        center + radius * Math.Sin(startRadians));

                    Point endPoint = new Point(
                        center + radius * Math.Cos(endRadians),
                        center + radius * Math.Sin(endRadians));

                    bool isLargeArc = sweepAngle > 180;

                   
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
                    IsLargeArc = isLargeArc,
                    SweepDirection = SweepDirection.Clockwise
                }
            }
                    };

                    var geometry = new PathGeometry();
                    geometry.Figures.Add(figure);

                  
                    memcleaner.ArcPath.Stretch = Stretch.None;
                    memcleaner.ArcPath.RenderTransform = new TranslateTransform(0, 0);
                    memcleaner.ArcPath.Data = geometry;
                });
            }


        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void WindowDragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("psapi.dll")]
        static extern bool EmptyWorkingSet(IntPtr hProcess);
        public async Task cleanmemory()
        {
            CleanButton.IsEnabled = false;
            CleanButton.Content = "Cleaning...";
            CleanedMemoryLabel.Text = "";

            long beforeFree = (long)NativeMethods.GetAvailablePhysicalMemory();

            await Task.Run(() =>
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    try
                    {
                        if (!proc.HasExited && proc.ProcessName != "System" && proc.ProcessName != "Idle")
                        {
                            EmptyWorkingSet(proc.Handle);
                            SetProcessWorkingSetSize(proc.Handle, (IntPtr)(-1), (IntPtr)(-1));
                        }
                    }
                    catch { }
                }

         
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            });

            long afterFree = (long)NativeMethods.GetAvailablePhysicalMemory();

            long freed = afterFree - beforeFree;
            if (freed < 0) freed = 0;

            double mbFreed = freed / (1024.0 * 1024.0);

            await Dispatcher.InvokeAsync(() =>
            {
                CleanedMemoryLabel.Text = $"Freed {mbFreed:F2} MB system memory at {DateTime.Now:T}";

                if (memorymon != null)
                    memorymon.FreedLabel.Text = $"Freed {mbFreed:F2} MB system memory at {DateTime.Now:T}";

                CleanButton.Content = "Clean Memory Now";
                CleanButton.IsEnabled = true;

                if (chkShowNotification.IsChecked == true)
                {
                    if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
                    {
                        Notify notify = new Notify($"Freed {mbFreed:F2} MB");
                        notify.Show();
                    }
                }
            });
        }

        private async void CleanMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            await cleanmemory();
        }
       

      
      
        private void chkSmartRAM_Checked(object sender, RoutedEventArgs e)
        {

            window.utilities.savesettings("mon:1");
            memorymon.Show();
               
           
            
           
        }
        private void chkSmartRAM_Unchecked(object sender, RoutedEventArgs e)
        {
          
           
            memorymon.Hide();
            window.utilities.savesettings("mon:0");

        }
        AutoCleanMem autocleanmem;
        private void chkEnableAutoClean_Checked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("automemclean:1");
            autocleanmem = new AutoCleanMem(this);
            Thread t = new Thread(autocleanmem.StartAutoClean);
            t.Start();
        }
        private void chkEnableAutoClean_UnChecked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("automemclean:0");
            autocleanmem.StopAutoClean();
        }

        private void chkShowNotification_Checked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("sendnotify:1");
            sendnotify = 1;
        }
        private void chkShowNotification_UnChecked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("sendnotify:0");
            sendnotify = 0;
        }

        private void chkSkipOnLowBattery_Checked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("skipiflow:1");
        }
        private void chkSkipOnLowBattery_UnChecked(object sender, RoutedEventArgs e)
        {
            window.utilities.savesettings("skipiflow:0");
        }

        private double originalHeight;
        private void BtnSettings_Checked(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Visible;
            this.Height = originalHeight + 200;
        }
        private void BtnSettings_Unchecked(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Collapsed;
            this.Height = originalHeight;
        }
    }
}

using Microsoft.VisualBasic.Devices;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using static MultronWinCleaner.MemCleaner.NativeMethods;

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for MemoryMon.xaml
    /// </summary>
    public partial class MemoryMon : Window
    {
        MemCleaner memcleaner;
        private bool processesVisible = false;
        private ObservableCollection<ProcessItem> processes = new ObservableCollection<ProcessItem>();
        private DispatcherTimer updateTimer;
      

        public MemoryMon(MemCleaner memcleaner)
        {
            InitializeComponent();
            this.Loaded += MemoryMon_Loaded;
            this.memcleaner = memcleaner;
            var ramupdater = new ramstatus(this);
            _ = Task.Run(() => ramupdater.Run());
            var cpuupdater = new cpustatus(this);
            _ = Task.Run(() => cpuupdater.Run());
            ProcessesListView.ItemsSource = processes;

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(2);
            updateTimer.Tick += UpdateTimer_Tick;

            if (memcleaner.chkTopMostMonitor.IsEnabled == true)
            {
                this.Topmost = false;
            }
            else
            {
                this.Topmost = true;
            }

         
        }
        public class ramstatus
        {
            MemoryMon memorymon;
            private double lastRamPercent = -1;
            public ramstatus(dynamic memorymon)
            {
                this.memorymon = memorymon;
            }
            public async Task Run()
            {
                var computerInfo = new ComputerInfo();
                while (true)
                {
                    ulong totalMemory = computerInfo.TotalPhysicalMemory;
                    ulong availableMemory = computerInfo.AvailablePhysicalMemory;
                    ulong usedMemory = totalMemory - availableMemory;
                    double ramPercent = Math.Round((usedMemory * 100.0) / totalMemory, 1);


 
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {

                        if (Math.Abs(ramPercent - lastRamPercent) > 0.1)
                        {
                            memorymon.RamBar.Minimum = 0;
                            memorymon.RamBar.Maximum = 100;
                            memorymon.RamBar.Value = ramPercent;
                            memorymon.RamPercentText.Text = $"{ramPercent:F1}%";
                            lastRamPercent = ramPercent;
                        }


                      
                    });
                    await Task.Delay(100);
                }
            }
        }
        public class cpustatus
        {
            private readonly dynamic memorymon;

            private double lastRamPercent = -1;
            private int lastCpuValue = -1;

            private ulong prevIdleTime = 0;
            private ulong prevKernelTime = 0;
            private ulong prevUserTime = 0;

            [StructLayout(LayoutKind.Sequential)]
            struct FILETIME
            {
                public uint dwLowDateTime;
                public uint dwHighDateTime;
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);

            public cpustatus(dynamic memorymon)
            {
                this.memorymon = memorymon;

             
                UpdateCpuTimes(out prevIdleTime, out prevKernelTime, out prevUserTime);
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

                ulong sysIdle = idleTime - prevIdleTime;
                ulong sysKernel = kernelTime - prevKernelTime;
                ulong sysUser = userTime - prevUserTime;

                ulong sysTotal = sysKernel + sysUser;

                prevIdleTime = idleTime;
                prevKernelTime = kernelTime;
                prevUserTime = userTime;

                if (sysTotal == 0)
                    return 0;

                ulong sysUsed = sysTotal - sysIdle;

                double cpuUsage = (sysUsed * 100.0) / sysTotal;

                return (int)Math.Round(cpuUsage);
            }

            public async Task Run()
            {
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();

                while (true)
                {
                    try
                    {
                        ulong totalMemory = computerInfo.TotalPhysicalMemory;
                        ulong availableMemory = computerInfo.AvailablePhysicalMemory;
                        ulong usedMemory = totalMemory - availableMemory;
                        double ramPercent = Math.Round((usedMemory * 100.0) / totalMemory, 1);

                        int cpuValue = CalculateCpuUsage();

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (Math.Abs(ramPercent - lastRamPercent) > 0.1)
                            {
                                memorymon.RamBar.Minimum = 0;
                                memorymon.RamBar.Maximum = 100;
                                memorymon.RamBar.Value = ramPercent;
                                memorymon.RamPercentText.Text = $"{ramPercent:F1}%";
                                lastRamPercent = ramPercent;
                            }

                            if (cpuValue != lastCpuValue)
                            {
                                memorymon.CpuBar.Minimum = 0;
                                memorymon.CpuBar.Maximum = 100;
                                memorymon.CpuBar.Value = cpuValue;
                                memorymon.CpuPercentText.Text = $"{cpuValue}%";
                                lastCpuValue = cpuValue;
                            }
                        });

                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[cpustatus] error: {ex.Message}");
                    }
                }
            }
        }


        private void MemoryMon_Loaded(object sender, RoutedEventArgs e)
        {
      
            var workingArea = System.Windows.SystemParameters.WorkArea;
             
            this.Left = workingArea.Right - this.Width - 10;
            this.Top = workingArea.Bottom - this.Height - 10;
        }

        private async void CleanMemory_Click(object sender, RoutedEventArgs e)
        {
           await memcleaner.cleanmemory();
        }
        private void Window_DragMove(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void SlowProcesses_Click(object sender, RoutedEventArgs e)
        {
            if (!processesVisible)
            {
                ProcessesListView.Visibility = Visibility.Visible;
                this.Height = 220;

                processesVisible = true;
                updateTimer.Start();
            }
            else
            {
                updateTimer.Stop();
                ProcessesListView.Visibility = Visibility.Collapsed;
                this.Height = 120;

                processesVisible = false;
            }
        }
        public class ProcessItem
        {
            public string Name { get; set; }
            public string Ram { get; set; }
        }
        
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var currentProcesses = Process.GetProcesses()
                    .OrderByDescending(p => p.WorkingSet64)
                    .Take(15)
                    .Select(p => new ProcessItem
                    {
                        Name = p.ProcessName,
                        Ram = $"{p.WorkingSet64 / 1024 / 1024} MB"
                    }).ToList();

                processes.Clear();
                foreach (var proc in currentProcesses)
                    processes.Add(proc);
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            memcleaner.chkSmartRAM.IsChecked = false;
        }
    }
}

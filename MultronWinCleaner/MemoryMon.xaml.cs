using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Management;
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

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for MemoryMon.xaml
    /// </summary>
    public partial class MemoryMon : Window
    {
        MemCleaner memcleaner;
        private bool processesVisible = false;
        private ObservableCollection<string> processes = new ObservableCollection<string>();
        private DispatcherTimer updateTimer;
        private PerformanceCounter ramCounter;

        public MemoryMon(MemCleaner memcleaner)
        {
            InitializeComponent();
            this.Loaded += MemoryMon_Loaded;
            this.memcleaner = memcleaner;
            ProcessesListView.ItemsSource = processes;
            ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
            if(memcleaner.chkTopMostMonitor.IsEnabled == true)
            {
                this.Topmost = false;
            } else
            {
                this.Topmost = true;
            }
            waitforit waitforit = new waitforit(this);
            Thread t = new Thread(waitforit.run);
            t.Start();
          
        }




        public class waitforit
        {
            public MemoryMon memorymon;

            public waitforit(MemoryMon memorymon)
            {
                this.memorymon = memorymon;
            }

            public void run()
            {
                while (true)
                {
                    if (memorymon.Visibility == Visibility.Visible)
                    {

                        MemoryMon.getstatus status = new MemoryMon.getstatus(memorymon);
                        Task.Run(() => status.run());
                    }
                    Thread.Sleep(2000);
                }
            }
        }

        public class getstatus
        {
            private readonly dynamic memorymon;

            private string lastRamText = "";
            private int lastRamValue = -1;
            private string lastCpuText = "";
            private int lastCpuValue = -1;

            public getstatus(dynamic memorymon)
            {
                this.memorymon = memorymon;
            }

            private int GetSystemCpuLoad()
            {
                
                using (var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor"))
                {
                    int sum = 0, count = 0;
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        sum += Convert.ToInt32(obj["LoadPercentage"]);
                        count++;
                    }
                    return (count > 0) ? sum / count : 0;
                }
            }

            public async Task run()
            {
                while (true)
                {
                    try
                    {
                         int cpuLoad = GetSystemCpuLoad();

                       
                        await ((Dispatcher)memorymon.Dispatcher).InvokeAsync(() =>
                        {
                            
                            string ramText = memorymon.memcleaner.RamUsagePercentText.Text
                                                 ?.Replace("%", "")
                                                 .Trim() ?? "";

                            if (ramText != lastRamText
                                && float.TryParse(ramText.Replace(',', '.'),
                                                  System.Globalization.NumberStyles.Float,
                                                  System.Globalization.CultureInfo.InvariantCulture,
                                                  out float ramPercent))
                            {
                                int ramValue = Math.Clamp((int)Math.Round(ramPercent), 0, 100);
                                if (ramValue != lastRamValue)
                                {
                                    memorymon.RamBar.Minimum = 0;
                                    memorymon.RamBar.Maximum = 100;
                                    memorymon.RamBar.Value = ramValue;
                                    memorymon.RamPercentText.Text = $"{ramValue}%";
                                    lastRamValue = ramValue;
                                }
                                lastRamText = ramText;
                            }
 
                            string cpuText = $"{cpuLoad}%";
                            if (cpuText != lastCpuText)
                            {
                                memorymon.CpuPercentText.Text = cpuText;
                                lastCpuText = cpuText;
                            }
                            if (cpuLoad != lastCpuValue)
                            {
                                memorymon.CpuBar.Minimum = 0;
                                memorymon.CpuBar.Maximum = 100;
                                memorymon.CpuBar.Value = cpuLoad;
                                lastCpuValue = cpuLoad;
                            }
                        });

                      
                        await Task.Delay(2000);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    { 
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

        private void CleanMemory_Click(object sender, RoutedEventArgs e)
        {
            memcleaner.cleanmemory();
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
                this.Height = 200; 
               
                processesVisible = true;
                updateTimer.Start();
            }
            else
            {
                updateTimer.Stop();
                ProcessesListView.Visibility = Visibility.Collapsed;
                this.Height = 75;  
              
                processesVisible = false;
            }
        }
      
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                var currentProcesses = Process.GetProcesses()
                 
                    .OrderByDescending(p => p.WorkingSet64)
                    .Select(p => $"{p.ProcessName} ({p.Id}) - {p.WorkingSet64 / 1024 / 1024} MB")
                    .ToList();

                processes.Clear();
                foreach (var proc in currentProcesses)
                    processes.Add(proc);
            }
            catch
            {
             
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            memcleaner.chkSmartRAM.IsChecked = false;
        }
    }
}

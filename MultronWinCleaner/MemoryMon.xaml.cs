using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
                        Thread t = new Thread(status.run);
                        t.Start();
                    }
                    Thread.Sleep(2000);
                }
            }
        }

        public class getstatus
        {
            MemoryMon memorymon;
            PerformanceCounter cpuCounter;

            public getstatus(MemoryMon memorymon)
            {
                this.memorymon = memorymon;
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
 
                _ = cpuCounter.NextValue();
            }

            public async void run()
            {
                await Task.Delay(1000);  

                while (true)
                {
                    float cpuUsage = cpuCounter.NextValue();  

                    memorymon.Dispatcher.Invoke(() =>
                    {
                        memorymon.RamPercentText.Text = memorymon.memcleaner.RamUsagePercentText.Text;

                        string ramText = memorymon.RamPercentText.Text.Replace("%", "").Trim();

                        if (float.TryParse(ramText.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float ramPercent))
                        {
                            int value = (int)Math.Round(ramPercent);
                            value = Math.Clamp(value, 0, 100);

                            memorymon.RamBar.Minimum = 0;
                            memorymon.RamBar.Maximum = 100;
                            memorymon.RamBar.Value = value;
                        }
                        else
                        {
                            memorymon.RamBar.Value = 0;
                        }

                        string cpuUsageText = $"{cpuUsage:0.0}%";
                        memorymon.CpuPercentText.Text = cpuUsageText;
                        memorymon.CpuBar.Value = Math.Min(100, (int)Math.Round(cpuUsage));
                    });

                    await Task.Delay(1000);  
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
                this.Height = 300; 
               
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
    }
}

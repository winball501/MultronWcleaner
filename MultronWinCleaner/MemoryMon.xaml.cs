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
        public MemoryMon(MemCleaner memcleaner)
        {
            InitializeComponent();
            this.Loaded += MemoryMon_Loaded;
            this.memcleaner = memcleaner;
            ProcessesListView.ItemsSource = processes;

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
        }
        public async Task CpuUsageAsync()
        {
            var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            _ = cpuCounter.NextValue();

            while (true)
            {
                await Task.Run(() =>
                {
                    float cpuUsage = cpuCounter.NextValue();

                    string formattedCpuUsage = cpuUsage.ToString("F1"); 

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CpuBar.Value = cpuUsage;
                        CpuPercentText.Text = formattedCpuUsage + "%";
                    });

                    Thread.Sleep(1000);
                });
            }
        }
        public async Task MemoryStatusAsync()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                
                    this.Dispatcher.Invoke(() =>
                    {
                        string percentText = memcleaner.RamUsagePercentText.Text.TrimEnd('%'); 
                        double ramPercentDouble = double.Parse(percentText, CultureInfo.InvariantCulture);
                        int ramPercent = (int)Math.Round(ramPercentDouble);

                        this.RamPercentText.Text = memcleaner.RamUsagePercentText.Text;
                        this.RamBar.Value = ramPercent;
                        
                    });

                    Thread.Sleep(100);  
                }
            });
        }
        private void MemoryMon_Loaded(object sender, RoutedEventArgs e)
        {
            MemoryStatusAsync();
            CpuUsageAsync();
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

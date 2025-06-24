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
        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr procHandle, int minSize, int maxSize);
        MemoryMon memorymon;
        MainWindow window;
        int sendnotify = 0;
        public MemCleaner(MainWindow window)
        {
            InitializeComponent();
            MemoryUsage usage = new MemoryUsage(this);
            Thread t = new Thread(usage.run);
            t.Start();
            this.window = window;
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
                        "5 minutes" => 5,
                        "10 minutes" => 10,
                        "15 minutes" => 15,
                        "30 minutes" => 30,
                        "1 hour" => 60,
                        _ => 5
                    };

                    timer = new System.Threading.Timer(_ =>
                    {
                        memcleaner.Dispatcher.Invoke(() =>
                        {
                            if(memcleaner.chkSkipOnLowBattery.IsChecked == true)
                            {
                                PowerStatus powerStatus = SystemInformation.PowerStatus;
                                float batteryLifePercent = powerStatus.BatteryLifePercent;  
                                if (batteryLifePercent < 0.20f)
                                {
                                    return;
                                }
                            }
                            

                            memcleaner.cleanmemory();
                        });
                    }, null, TimeSpan.Zero, TimeSpan.FromMinutes(intervalMinutes));
                }));
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

                    double angle = usedPercent * 360 / 100;

                    double controlSize = 240;
                    double stroke = 18;

                    double radius = (controlSize / 2) - (stroke / 2);  
                    double centerX = controlSize / 2;  
                    double centerY = controlSize / 2;

                
                    double startX = centerX;
                    double startY = centerY - radius;

                   
                    double radians = (Math.PI / 180) * (angle - 90);
                    double endX = centerX + radius * Math.Cos(radians);
                    double endY = centerY + radius * Math.Sin(radians);

                    bool isLargeArc = angle > 180.0;

                    var arcSegment = new ArcSegment
                    {
                        Point = new Point(endX, endY),
                        Size = new Size(radius, radius),
                        IsLargeArc = isLargeArc,
                        SweepDirection = SweepDirection.Clockwise
                    };

                    var figure = new PathFigure
                    {
                        StartPoint = new Point(startX, startY),
                        IsClosed = false
                    };
                    figure.Segments.Add(arcSegment);

                    var geometry = new PathGeometry();
                    geometry.Figures.Add(figure);

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
      
        public async Task cleanmemory()
        {
           
            CleanButton.IsEnabled = false;
            CleanButton.Content = "Cleaning...";
            CleanedMemoryLabel.Text = "";

            long totalFreedMemory = 0;
             
            await Task.Run(() =>
            {
                foreach (Process proc in Process.GetProcesses())
                {
                    try
                    {
                        if (!proc.HasExited && !string.IsNullOrEmpty(proc.ProcessName)
                            && proc.ProcessName != "System" && proc.ProcessName != "Idle")
                        {
                            long before = proc.WorkingSet64;

                            if (SetProcessWorkingSetSize(proc.Handle, -1, -1))
                            {
                                proc.Refresh();
                                long after = proc.WorkingSet64;
                                long freed = before - after;

                                if (freed > 0)
                                    totalFreedMemory += freed;
                            }
                        }
                    }
                    catch
                    { 
                    }
                }
            }); 
            double mbFreed = totalFreedMemory / (1024.0 * 1024.0);
            CleanedMemoryLabel.Text = $"🧼 Freed {mbFreed:F2} MB memory at {DateTime.Now:T}";
            if(memorymon != null)
               memorymon.FreedLabel.Text = $"🧼 Freed {mbFreed:F2} MB memory at {DateTime.Now:T}";
            CleanButton.Content = "Clean Memory Now";
            CleanButton.IsEnabled = true;
            if(chkShowNotification.IsChecked == true)
            {
                if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
                {
                    Notify notify = new Notify($"🧼 Freed {mbFreed:F2} MB");
                    notify.Show();
                }
            }
           
        }
        private void CleanMemoryButton_Click(object sender, RoutedEventArgs e)
        {
            cleanmemory();
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
        int doit = 0;
        int vis = 0;
        private void chkSmartRAM_Checked(object sender, RoutedEventArgs e)
        {
            if(vis == 0)
            {
                savesettings("mon:1");
                if (doit == 0)
                {
                    memorymon = new MemoryMon(this);
                    memorymon.Show();
                    doit = 1;
                    vis = 1;
                }
                else
                {
                    memorymon.Show();
                    vis = 1;
                }
            } else
            {
                memorymon.Hide();
                savesettings("mon:0");
            }
            
           
        }
        AutoCleanMem autocleanmem;
        private void chkEnableAutoClean_Checked(object sender, RoutedEventArgs e)
        {
            savesettings("automemclean:1");
            autocleanmem = new AutoCleanMem(this);
            Thread t = new Thread(autocleanmem.StartAutoClean);
            t.Start();
        }
        private void chkEnableAutoClean_UnChecked(object sender, RoutedEventArgs e)
        {
            savesettings("automemclean:0");
            autocleanmem.StopAutoClean();
        }

        private void chkShowNotification_Checked(object sender, RoutedEventArgs e)
        {
            savesettings("sendnotify:1");
            sendnotify = 1;
        }
        private void chkShowNotification_UnChecked(object sender, RoutedEventArgs e)
        {
            savesettings("sendnotify:0");
            sendnotify = 0;
        }

        private void chkSkipOnLowBattery_Checked(object sender, RoutedEventArgs e)
        {
            savesettings("skipiflow:1");
        }
        private void chkSkipOnLowBattery_UnChecked(object sender, RoutedEventArgs e)
        {
            savesettings("skipiflow:0");
        }
    }
}

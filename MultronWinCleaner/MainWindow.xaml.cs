using Hardcodet.Wpf.TaskbarNotification;
using LibreHardwareMonitor.Hardware;
using MFK;
using MultronWinCleaner;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static Multron_Win_Cleaner.MainWindow;
using static System.Net.WebRequestMethods;
using System;
using System.Windows.Threading;

namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {


        public Settings settings;
       
        List<string> logfiles = new List<string>();
        SolidColorBrush brush;
        List<CheckBox> checkboxes2 = new List<CheckBox>();
        List<StackPanel> stackpanels = new List<StackPanel>();
        List<Expander> expanders = new List<Expander>();
        byte autoclean = 0;
        byte onclean = 0;

        public MainWindow()
        {
            InitializeComponent();
          
            utilities = new Utilities(this);
            utilities.Show();
            utilities.Hide();
            progressBar1.ValueChanged += ProgressBar1_ValueChanged;
            settings  = new Settings(utilities.memcleaner, utilities, this);
           
            settings.Show();
            settings.Hide();
          
            if (!System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt")) {
                System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt").Close();
                
            }
            string fileContent = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt");
            
             
          
            if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt"))
            {
            
                if (fileContent.Contains("themes:1"))
                {
                    themeselector.selector(new Uri("Themes/Dark.xaml", UriKind.Relative));
                   ToggleThemeSwitch.IsChecked = true;
                }
                else
                {
                    themeselector.selector(new Uri("Themes/Light.xaml", UriKind.Relative));
                    ToggleThemeSwitch.IsChecked = false;
                }
                string themePath = null;
                if (ToggleThemeSwitch.IsChecked == true)
                {
                    themePath = "Themes/Dark.xaml";
                }
                else
                {
                    themePath = "Themes/Light.xaml";
                }


                var resourceDictionary = new ResourceDictionary
                {
                    Source = new Uri(themePath, UriKind.Relative)
                };
                brush = (SolidColorBrush)resourceDictionary["Text"];
                if (fileContent == "")
                {
                   
                    string defaultSettings = "minutes:0" + Environment.NewLine +
                                             "autoclean:0" + Environment.NewLine +
                                             "trayicon:0" + Environment.NewLine +
                                             "themes:0";
                    System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt", defaultSettings);
                    fileContent = defaultSettings; 
                }
            }
         
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {
         
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
              
                Load load = new Load(this);
                Thread t2 = new Thread(new ThreadStart(load.run));
                t2.Start();

                this.previousWidth = this.Width;
                this.previousHeight = this.Height;
                this.previousLeft = this.Left;
                this.previousTop = this.Top;
                
            }
            else
            {
                MessageBox.Show("Any database file not found, Program closing...", "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            AutoClean autoclean = new AutoClean(this);
            Thread t = new Thread(new ThreadStart(autoclean.run));
            t.Start();
            TrayIconWindow trayiconwindow = new TrayIconWindow(this);
            
            Thread traythread = new Thread(trayiconwindow.run);
            traythread.Start();
          
            TrayIcon.TrayMouseDoubleClick += TrayIcon_MouseDoubleClick;
            CheckBox malscan = new CheckBox
            {
                Content = "Malware Scan=Scans and cleans your system from malicious software=malscan",
                Margin = new Thickness(10),
                IsChecked = false,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = brush,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkboxes2.Add(malscan);

            StackPanel groupBoxContent2 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left

            };
            stackpanels.Add(groupBoxContent2);
            Expander newExpander1 = new Expander
            {
                Header = "Malware Scan (Coming Soon)",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                IsEnabled = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            expanders.Add(newExpander1);
            groupBoxContent2.Children.Add(malscan);
            newExpander1.Content = groupBoxContent2;
            try
            {
                wrapPanel1.Children.Add(newExpander1);
            }
            catch (Exception)
            {

            }
            CheckBox newCheckBox = new CheckBox
            {
                Content = "Deep Log Files Scan " + "=" + "C:\\" + "=warning=Its can take long time." + "=" + "logscan",
                Margin = new Thickness(10),
                IsChecked = false,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = brush,
                FontSize = 12,
                FontFamily = new FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkboxes2.Add(newCheckBox);

            StackPanel groupBoxContent3 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left

            };
            stackpanels.Add(groupBoxContent3);
            Expander newExpander2 = new Expander
            {
                Header = "Deep Log Files Scan",
                Margin = new Thickness(5),
                Background = new SolidColorBrush(Colors.Transparent),
                Foreground = brush,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(2),
                FontSize = 14,
                FontWeight = FontWeights.Regular,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            expanders.Add(newExpander2);
            groupBoxContent3.Children.Add(newCheckBox);
            newExpander2.Content = groupBoxContent3;
            try
            {
                wrapPanel1.Children.Add(newExpander2);
            }
            catch (Exception)
            {

            }
            newCheckBox.Checked += CheckBox_Checked;
            newCheckBox.Unchecked += CheckBox_Unchecked;

        }
        private void ProgressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateArc(progressBar1.Value, progressBar1.Maximum);
        }
        private void UpdateArc(double value, double max)
        {
            double percentage = value / max;
            double angle = 360 * percentage;

            double radius = progressBar1.Width / 2.0 - 7.5;  
            Point center = new Point(progressBar1.Width / 2.0, progressBar1.Height / 2.0);

            Point startPoint = new Point(center.X, center.Y - radius);
            double radians = (Math.PI / 180) * (angle - 90);
            Point endPoint = new Point(
                center.X + radius * Math.Cos(radians),
                center.Y + radius * Math.Sin(radians));

            bool isLargeArc = angle > 180;

            var figure = new PathFigure { StartPoint = startPoint };
            var arcSegment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise,
                RotationAngle = 0
            };

            figure.Segments.Clear();
            figure.Segments.Add(arcSegment);

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);

            if (progressBar1.Template.FindName("PART_Indicator", progressBar1) is System.Windows.Shapes.Path path)
            {
                path.Data = geometry;
            }
        }
        private void TrayIcon_MouseDoubleClick(object sender, RoutedEventArgs e)
        {
           
            if (this.Visibility == Visibility.Hidden)
            {
                this.Show();  
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Normal;  
            }

            this.Activate(); 
        }
        private void HideApp_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            settings.Hide();
        }
        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

  
   
        public string stringtokenizer(string input, string token, int index)
        {

            string[] tokens = input.Split(token);

            if (index >= 0 && index < tokens.Length)
            {
                return (tokens[index]);
            }
            else
            {
                return (null);
            }
        }
        public HashSet<string> database = new HashSet<string>();
        public int scanstatus = 0;
        public int cancelstatus = 0;
        byte created = 0;
        List<CheckBox> checkboxes = new List<CheckBox>();
        StackPanel groupBoxContent = null;
        Expander expander = null;
        public class TrayIconWindow
        {
            MainWindow main;
            public TrayIconWindow(MainWindow main)
            {
                this.main = main;
            }
            public async void run()
            {
                try
                {
                    while (true)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            if (main.settings.chkTrayIcon.IsChecked == true && main.TrayIcon.Visibility != Visibility.Visible)
                            {

                                main.TrayIcon.Visibility = Visibility.Visible;
                            }
                            else if (main.settings.chkTrayIcon.IsChecked == false && main.TrayIcon.Visibility == Visibility.Visible)
                            {
                                main.TrayIcon.Visibility = Visibility.Hidden;
                            }

                        });
                        Thread.Sleep(1000);
                    }
                } catch (Exception e)
                {

                }

            }
        }
        public class AutoClean : IDisposable
        {
            private MainWindow main;
            private int minutes = 0;
            private int seconds = 0;
            private Computer _computer;
            private DateTime? lastRunTime = null;
            private string lastScheduleType = null;
            private bool startTimeTriggered = false;
            private bool endTimeTriggered = false;
            private int lastCheckedDay = -1;
            public AutoClean(MainWindow main)
            {
                this.main = main;
                _computer = new Computer()
                {
                    IsCpuEnabled = true,
                    IsBatteryEnabled = true
                };
                _computer.Open();
            }

            public async void run()
            {
                try
                {
                    while (true)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            var settings = main.settings;

                            int currentDay = DateTime.Now.Day;
                            if (lastCheckedDay != currentDay)
                            {
                                startTimeTriggered = false;
                                endTimeTriggered = false;
                                lastCheckedDay = currentDay;
                            }

                            if (settings.txtCleaningInterval.IsKeyboardFocusWithin ||
                                settings.txtStartTime.IsKeyboardFocusWithin ||
                                settings.txtEndTime.IsKeyboardFocusWithin ||
                                settings.txtExceptionPath.IsKeyboardFocusWithin)
                            {
                                settings.txtClock.Text = $"{DateTime.Now:hh:mm:ss tt} Auto-cleaning is paused while editing settings.";
                                return;
                            }
                            else
                            {
                                settings.txtClock.Text = "Computer Time: " + DateTime.Now.ToString("hh:mm:ss tt");
                            }

                            bool autoCleanEnabled = settings.chkAutoClean.IsChecked == true;
                            bool isCpuUsageLow = settings.OnlyLowCPU.IsChecked != true || CheckCpuUsageBelow(20);
                            bool isBatteryOk = settings.SkipBatterry.IsChecked != true || CheckBatteryLevelAbove(30);
                            bool isInactive = settings.RunIfInactive.IsChecked != true || CheckUserInactivity();

                            if (!autoCleanEnabled || main.onclean == 1 || !isCpuUsageLow || !isBatteryOk || !isInactive)
                            {
                                minutes = 0;
                                seconds = 0;
                                return;
                            }

                            if (!int.TryParse(settings.txtCleaningInterval.Text, out int interval) || interval <= 0)
                            {
                                minutes = 0;
                                seconds = 0;
                                return;
                            }

                            var selectedItem = settings.cmbScheduleType.SelectedItem as ComboBoxItem;
                            string selectedContent = selectedItem?.Content?.ToString() ?? "";

                            if (lastScheduleType != selectedContent)
                            {
                                lastRunTime = null;
                                minutes = 0;
                                seconds = 0;
                                lastScheduleType = selectedContent;
                            }

                            int dayOfWeek = (int)DateTime.Now.DayOfWeek + 1;
                            CheckBox dayCheckBox = (CheckBox)settings.FindName($"day{dayOfWeek}");
                            if (dayCheckBox != null && dayCheckBox.IsChecked != true)
                            {
                                minutes = 0;
                                seconds = 0;
                                lastRunTime = null;
                                return;
                            }

                            bool isInTimeRange = IsWithinAllowedTime(settings.txtStartTime.Text, settings.txtEndTime.Text);
                            if (!isInTimeRange)
                            {
                                minutes = 0;
                                seconds = 0;
                                lastRunTime = null;
                                return;
                            }

                            DateTime now = DateTime.Now;

                            
                            if (!DateTime.TryParse(settings.txtStartTime.Text, out DateTime startTime)) return;
                            if (!DateTime.TryParse(settings.txtEndTime.Text, out DateTime endTime)) return;

                            startTime = startTime.Date + startTime.TimeOfDay;
                            endTime = endTime.Date + endTime.TimeOfDay;

                            
                            startTime = new DateTime(now.Year, now.Month, now.Day, startTime.Hour, startTime.Minute, 0);
                            endTime = new DateTime(now.Year, now.Month, now.Day, endTime.Hour, endTime.Minute, 0);

                             
                            if (!startTimeTriggered && now >= startTime && now < startTime.AddMinutes(1))
                            {
                                TriggerCleaning(now);
                                startTimeTriggered = true;
                                
                                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => startTimeTriggered = false);
                                return;
                            }
                             
                            if (!endTimeTriggered && now >= endTime && now < endTime.AddMinutes(1))
                            {
                                TriggerCleaning(now);
                                endTimeTriggered = true;
                                Task.Delay(TimeSpan.FromMinutes(1)).ContinueWith(_ => endTimeTriggered = false);
                                return;
                            }

                            if (selectedContent != "Instant")
                            {
                                if (lastRunTime.HasValue &&
                                    (now - lastRunTime.Value).TotalMinutes < interval)
                                {
                                    return;
                                }

                                TriggerCleaning(now);
                                return;
                            }

                            if (selectedContent == "Instant" && main.autoclean == 0)
                            {
                                if (minutes >= interval)
                                {
                                    TriggerCleaning(now);
                                    minutes = 0;
                                    seconds = 0;
                                }
                                else
                                {
                                    seconds++;
                                    if (seconds >= 60)
                                    {
                                        minutes++;
                                        seconds = 0;
                                    }
                                }
                            }
                        });

                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                  
                }
            }

            private void TriggerCleaning(DateTime now)
            {
                if (!main.buttonStartScan.IsEnabled)
                    main.buttonStartScan.IsEnabled = true;

                main.buttonStartScan.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                main.autoclean = 1;
                lastRunTime = now;
            }

            bool IsWithinAllowedTime(string startTimeStr, string endTimeStr)
            {
                if (!DateTime.TryParse(startTimeStr, out DateTime startTime)) return false;
                if (!DateTime.TryParse(endTimeStr, out DateTime endTime)) return false;

                TimeSpan now = DateTime.Now.TimeOfDay;
                TimeSpan start = startTime.TimeOfDay;
                TimeSpan end = endTime.TimeOfDay;

                if (start <= end)
                    return now >= start && now <= end;
                else
                    return now >= start || now <= end;
            }

            public bool CheckCpuUsageBelow(int threshold)
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Cpu)
                    {
                        hardware.Update();
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Load && sensor.Name.ToLower().Contains("total"))
                            {
                                return (sensor.Value ?? 0f) < threshold;
                            }
                        }
                    }
                }
                return true;
            }

            public bool CheckBatteryLevelAbove(int minPercentage)
            {
                foreach (IHardware hardware in _computer.Hardware)
                {
                    if (hardware.HardwareType == HardwareType.Battery)
                    {
                        hardware.Update();
                        foreach (ISensor sensor in hardware.Sensors)
                        {
                            if (sensor.SensorType == SensorType.Level)
                            {
                                return (sensor.Value ?? 100f) >= minPercentage;
                            }
                        }
                    }
                }
                return true;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct LASTINPUTINFO
            {
                public uint cbSize;
                public uint dwTime;
            }

            [DllImport("user32.dll")]
            static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

            public bool CheckUserInactivity()
            {
                LASTINPUTINFO info = new LASTINPUTINFO();
                info.cbSize = (uint)Marshal.SizeOf(info);
                if (GetLastInputInfo(ref info))
                {
                    uint idleTime = (uint)Environment.TickCount - info.dwTime;
                    return idleTime >= 900000; 
                }
                return false;
            }

            public void Dispose()
            {
                _computer?.Close();
            }
        }

        public class Scan
        {
            MainWindow main;
            long totalsize = 0;
            public Scan(MainWindow main)
            {
                this.main = main;
            }

            public async void run()
            {
                try
                {
                    long size = main.database.Count();
                    long nowscanning = 0;
                    main.scanstatus = 2;
                    main.onclean = 1;
                    TextBlock directorytextblock = null;

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.wrapPanelDirectories.Visibility = Visibility.Visible;
                        main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                        main.label1_Copy.Foreground = Brushes.Blue;
                    });

                    foreach (string directory in main.database)
                    {

                           
                            if (main.cancelstatus == 1)
                            {
                                 break;
                            }
                             if(main.created == 1)
                             {
                                main.created = 0;
                             }
                            string name = main.stringtokenizer(directory, "=", 0);
                            string path = main.stringtokenizer(directory, "=", 1);

                            if (directory.EndsWith("logscan"))
                            {
                             
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    directorytextblock = new TextBlock
                                    {
                                        Text = $"Process: {name}",
                                        Foreground = Brushes.Goldenrod,
                                        FontSize = 16,
                                        Margin = new Thickness(5),
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                        VerticalAlignment = VerticalAlignment.Top
                                    };
                                });



                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.wrapPanelDirectories.Children.Add(directorytextblock);
                                });

                               
                                await ScanCDirectoryAsync(path, name, directorytextblock);

                            }
                            else
                            {
                                if (System.IO.File.Exists(directory))
                                {

                                    directorytextblock = new TextBlock
                                    {
                                        Text = $"Scanning: {name}",
                                        Foreground = Brushes.Goldenrod,
                                        FontSize = 16,
                                        Margin = new Thickness(5),
                                        HorizontalAlignment = HorizontalAlignment.Stretch,
                                        VerticalAlignment = VerticalAlignment.Top,


                                    };

                                    await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            main.wrapPanelDirectories.Children.Add(directorytextblock);

                                        });

                                    this.totalsize += new FileInfo(directory).Length;


                                }
                                else
                                {

                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        directorytextblock = new TextBlock
                                        {
                                            Text = $"Scanning: {name}",
                                            Foreground = Brushes.Goldenrod,
                                            FontSize = 16,
                                            Margin = new Thickness(5),
                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                            VerticalAlignment = VerticalAlignment.Top
                                        };
                                    
                                          
                                    });

                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.wrapPanelDirectories.Children.Add(directorytextblock);

                                    });


                                    main.created = 0;
                            
                                await ScanDirectoryAsync(path, name, directorytextblock);
                                }
                            }
                            nowscanning++;
                            double percentage = (double)nowscanning / size * 100;
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.progressBar1.Value = percentage;
                                main.UpdateArc(main.progressBar1.Value, main.progressBar1.Maximum);
                            });



                       
                   
                    }
                    if (main.cancelstatus == 1)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Scanning: " + "Canceled";
                       
                            main.ScrollViewerDirectories.Visibility = Visibility.Hidden;
                            main.wrapPanelDirectories.Visibility = Visibility.Hidden;
                            main.wrapPanelDirectories.Children.Clear();
                            main.buttonStartScan.IsEnabled = true;
                        
                            main.wrapPanel1.Visibility = Visibility.Visible;
                            main.cancelstatus = 0;
                            main.progressBar1.Value = 0;
                            main.UpdateArc(main.progressBar1.Value, main.progressBar1.Maximum);
                        });

                    }
                    else
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            if(main.autoclean == 1)
                            {
                                main.label1_Copy.Text = "Auto scan completed! + " + formatsize(totalsize) + "  Useless file found! " + DateTime.Now;
                            } else
                            {
                                main.label1_Copy.Text = "Scan completed! + " + formatsize(totalsize) + "  Useless file found! " + DateTime.Now;
                            }
                                

                            main.label1_Copy.Foreground = Brushes.Goldenrod;
                            main.buttonStartScan.IsEnabled = true;
                            main.buttonStartScan.Content = "Clean";
                            if(main.autoclean == 1)
                            {
                                main.buttonStartScan.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                                Debug.WriteLine("PRESSED");
                            }
                         
                        });

                    }

                    main.scanstatus = 1;
                } catch (Exception ex)
                {

                }
               
            }

            public async Task ScanCDirectoryAsync(string directory, string name, TextBlock directorytextblock)
            {
                try
                {
                    long totalsize = await GetCDirectorySizeAsync(directory);
                    string sizeformatted = formatsize(totalsize);
                    this.totalsize += totalsize;
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        directorytextblock.Text = "Completed: " + name + " " + formatsize(totalsize);

                      
                    });
                }
                catch (Exception ex)
                {
                     Console.WriteLine($"Error scanning directory {directory}: {ex.Message}");
                }
            }

            public async Task<long> GetCDirectorySizeAsync(string path)
            {
                long size = 0;

                try
                {
                    if (Directory.Exists(path))
                    {
                        HashSet<string> files = Directory.GetFiles(path).ToHashSet();
                        HashSet<string> dirs = Directory.GetDirectories(path).ToHashSet();

                     
                        foreach (var dir in dirs)
                        {
                            if (main.cancelstatus == 1)
                                return size;  

                            size += await GetCDirectorySizeAsync(dir);  
                        }

                 
                        foreach (var file in files)
                        {
                            if (main.cancelstatus == 1)
                            {
                                return size;
                              
                            }
                            HashSet<string> logExtensions = new HashSet<string> { ".log", ".etl", ".dmp", ".trace", ".tmp", ".temp", ".bak", ".swp" };
                            if (logExtensions.Any(extension => file.EndsWith(extension)))
                            {
                                if (!main.settings.excludedfiles.Contains(file))
                                {
                                    main.logfiles.Add(file);
                                    long fileSize = new FileInfo(file).Length;
                                    size += fileSize;
                                    this.totalsize += fileSize;


                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.label1_Copy.Text = $"Scanning: {file}";
                                    });

                                    if (main.created == 0)
                                    {
                                        main.created = 1;

                                        await main.Dispatcher.InvokeAsync(async () =>
                                        {

                                            CheckBox newCheckBox = new CheckBox
                                            {
                                                Content = "File to delete=" + file + "=" + formatsize(size),
                                                Margin = new Thickness(10),
                                                IsChecked = false,
                                                BorderThickness = new Thickness(0),
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                                                Foreground = main.brush,
                                                FontSize = 12,
                                                FontFamily = new FontFamily("Segoe UI"),
                                                FontWeight = FontWeights.Regular,
                                                FontStyle = FontStyles.Normal,
                                            };
                                            main.checkboxes2.Add(newCheckBox);
                                            main.groupBoxContent = new StackPanel
                                            {
                                                Orientation = Orientation.Vertical,
                                                VerticalAlignment = VerticalAlignment.Top,
                                                HorizontalAlignment = HorizontalAlignment.Left

                                            };
                                            
                                            main.expander = new Expander
                                            {
                                                Header = "Select Files deep log scan's finded ",
                                                Margin = new Thickness(5),
                                                Background = new SolidColorBrush(Colors.Transparent),
                                                Foreground = main.brush,
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                BorderThickness = new Thickness(2),
                                                FontSize = 14,
                                                FontWeight = FontWeights.Regular,
                                                HorizontalAlignment = HorizontalAlignment.Left,
                                                VerticalAlignment = VerticalAlignment.Top
                                            };
                                            main.expanders.Add(main.expander);
                                            await main.Dispatcher.InvokeAsync(() =>
                                        {
                                                    newCheckBox.IsChecked = true;
                                                    main.checkboxes.Add(newCheckBox);
                                                    main.groupBoxContent.Children.Add(newCheckBox);
                                                    main.expander.Content = main.groupBoxContent;


                                                });
                                            await main.Dispatcher.InvokeAsync(() =>
                                        {
                                                    main.wrapPanelDirectories.Children.Add(main.expander);
                                                });

                                            newCheckBox.Checked += main.CheckBox2_Checked;
                                            newCheckBox.Unchecked += main.CheckBox2_Unchecked;
                                            newCheckBox.Unchecked += main.CheckBox2_Unchecked;



                                        });



                                    }
                                    else
                                    {

                                        await main.Dispatcher.InvokeAsync(async () =>
                                        {
                                            CheckBox newCheckBox = new CheckBox
                                            {
                                                Content = "File to delete=" + file + "=" + formatsize(size),
                                                Margin = new Thickness(10),
                                                IsChecked = false,
                                                BorderThickness = new Thickness(0),
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                                                Foreground = main.brush,
                                                FontSize = 12,
                                                FontFamily = new FontFamily("Segoe UI"),
                                                FontWeight = FontWeights.Regular,
                                                FontStyle = FontStyles.Normal,

                                            };
                                            main.checkboxes2.Add(newCheckBox);
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                newCheckBox.IsChecked = true;
                                                main.checkboxes.Add(newCheckBox);
                                                main.groupBoxContent.Children.Add(newCheckBox);


                                            });
                                            newCheckBox.Checked += main.CheckBox2_Checked;
                                            newCheckBox.Unchecked += main.CheckBox2_Unchecked;
                                        });


                                    }

                                }

                    
                            }
                        }
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Access denied to directory {path}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning directory {path}: {ex.Message}");
                }

                return size;
            }

            public async Task ScanDirectoryAsync(string directory, string name, TextBlock directorytextbox)
            {
                long totalsize = await GetDirectorySizeAsync(directory);
                string sizeformatted = formatsize(totalsize);
                this.totalsize += totalsize;

                await main.Dispatcher.InvokeAsync(() =>
                {
                    directorytextbox.Text = "Completed: " + name + " " + sizeformatted;
                });
            }

          
            private string formatsize(long sizeinbytes)
            {
                double size = sizeinbytes;

                if (size >= 1 << 30) return $"{size / (1 << 30):0.##} GB";
                if (size >= 1 << 20) return $"{size / (1 << 20):0.##} MB";
                if (size >= 1 << 10) return $"{size / (1 << 10):0.##} KB";
                return $"{size} Bytes";
            }

         
            public async Task<long> GetDirectorySizeAsync(string path)
            {
                try
                {
                    long size = 0;

                 
                    if (Directory.Exists(path))
                    {

                        HashSet<string> files = Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToHashSet<string>();

                        await Task.WhenAll(files.Select(async file =>
                        {
                            if (main.cancelstatus == 1)
                            {
                                return;
                            }
                          

                            if(!main.settings.excludedfiles.Contains(file))
                            {
                                files.Remove(file);
                                FileInfo fileInfo = new FileInfo(file);
                                size += fileInfo.Length;


                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Text = $"Scanning: {file}";
                                });
                                if (System.IO.File.Exists(file))
                                {
                                    await main.Dispatcher.InvokeAsync(async () =>
                                    {

                                        if (main.created == 0)
                                        {

                                            main.created = 1;
                                            CheckBox newCheckBox = new CheckBox
                                            {
                                                Content = "File to delete=" + file + "=" + formatsize(size),
                                                Margin = new Thickness(10),
                                                IsChecked = false,
                                                BorderThickness = new Thickness(0),
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                                                Foreground = main.brush,
                                                FontSize = 12,
                                                FontFamily = new FontFamily("Segoe UI"),
                                                FontWeight = FontWeights.Regular,
                                                FontStyle = FontStyles.Normal,
                                            };
                                            main.checkboxes2.Add(newCheckBox);
                                            main.groupBoxContent = new StackPanel
                                            {
                                                Orientation = Orientation.Vertical,
                                                VerticalAlignment = VerticalAlignment.Top,
                                                HorizontalAlignment = HorizontalAlignment.Left

                                            };
                                            main.stackpanels.Add(main.groupBoxContent);
                                            main.expander = new Expander
                                            {
                                                Header = "Select files in " + path,
                                                Margin = new Thickness(5),
                                                Background = new SolidColorBrush(Colors.Transparent),
                                                Foreground = main.brush,
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                BorderThickness = new Thickness(2),
                                                FontSize = 14,
                                                FontWeight = FontWeights.Regular,
                                                HorizontalAlignment = HorizontalAlignment.Left,
                                                VerticalAlignment = VerticalAlignment.Top
                                            };
                                            main.expanders.Add(main.expander);
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                newCheckBox.IsChecked = true;
                                                main.checkboxes.Add(newCheckBox);
                                                main.groupBoxContent.Children.Add(newCheckBox);
                                                main.expander.Content = main.groupBoxContent;


                                            });
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                if (!main.wrapPanelDirectories.Children.Contains(main.expander))
                                                {
                                                    main.wrapPanelDirectories.Children.Add(main.expander);
                                                }
                                            });

                                            newCheckBox.Checked += main.CheckBox2_Checked;
                                            newCheckBox.Unchecked += main.CheckBox2_Unchecked;
                                        }
                                        else
                                        {
                                            CheckBox newCheckBox = new CheckBox
                                            {
                                                Content = "File to delete=" + file + "=" + formatsize(size),
                                                Margin = new Thickness(10),
                                                IsChecked = false,
                                                BorderThickness = new Thickness(0),
                                                BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                                                Foreground = main.brush,
                                                FontSize = 12,
                                                FontFamily = new FontFamily("Segoe UI"),
                                                FontWeight = FontWeights.Regular,
                                                FontStyle = FontStyles.Normal,

                                            };
                                            main.checkboxes2.Add(newCheckBox);
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                newCheckBox.IsChecked = true;
                                                main.checkboxes.Add(newCheckBox);
                                                main.groupBoxContent.Children.Add(newCheckBox);


                                            });
                                            newCheckBox.Checked += main.CheckBox2_Checked;
                                            newCheckBox.Unchecked += main.CheckBox2_Unchecked;
                                        }

                                    });
                                }
                            }
                          
                          
                    
                        }));
                    }

                    return size;
                } catch (Exception ex)
                {
                    return 0;
                }
            
            }
        }
        public class Load
        {
            MainWindow main;
            public Load(MainWindow main)
            {
                this.main = main;
            }
            public async void run()
            {
                try
                {
                    string filePath = Environment.CurrentDirectory + "\\" + "database.txt";
                    if (System.IO.File.Exists(filePath))
                    {
                        int totalLines = System.IO.File.ReadLines(filePath).Count();
                        int currentLine = 0;


                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;


                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = "Loading Database...";
                                main.buttonStartScan.IsEnabled = false;


                            });
                            int groupboxmode = 0;
                            int created = 0;
                            int profileget = 0;
                            string groupboxcontent = "";
                            GroupBox newGroupBox = null;
                            StackPanel groupBoxContent = null;
                            Expander newExpander = null;
                            ComboBox profilelist = null;
                            string profile = "";
                            while ((line = reader.ReadLine()) != null)
                            {

                                int linecontains = 0;
                                string name = main.stringtokenizer(line, "=", 0);
                                string path = main.stringtokenizer(line, "=", 1);

                                bool recommended = false;
                                if (line.StartsWith("{"))
                                {
                                    groupboxmode = 1;
                                    linecontains = 1;
                                    groupboxcontent = main.stringtokenizer(line, "=", 1);

                                }
                                else if (line.StartsWith("}"))
                                {
                                    if (profilelist != null && groupBoxContent != null)
                                    {
                                        main.Dispatcher.Invoke(() =>
                                        {
                                            groupBoxContent.Children.Add(profilelist);
                                        });


                                    }
                                    profile = "";
                                    profilelist = null;
                                    groupBoxContent = null;
                                    linecontains = 1;
                                    groupboxmode = 0;
                                    created = 0;
                                    profile = "";

                                }
                                else
                                {
                                    if (line.Contains("#profileget#"))
                                    {
                                        recommended = bool.Parse(main.stringtokenizer(line, "=", 3));

                                    }
                                    else
                                    {
                                        if (!line.StartsWith("#profile#"))
                                        {
                                            recommended = bool.Parse(main.stringtokenizer(line, "=", 2));
                                        }

                                    }

                                }

                                string haswarning = main.stringtokenizer(line, "=", 3);
                                if (haswarning == "true" || haswarning == "false")
                                {
                                    haswarning = null;
                                }
                                if (line.Contains("{##}"))
                                {
                                    await main.Dispatcher.InvokeAsync(async () =>
                                    {
                                        path = path.Replace("{##}", main.settings.comboBoxUserSelection.SelectedItem.ToString());
                                    });
                                 
                                }
                                if (line.Contains("#profile#="))
                                {
                                    path = main.stringtokenizer(line, "=", 1);
                                    if (line.Contains("{##}"))
                                    {
                                        await main.Dispatcher.InvokeAsync(async () =>
                                        {
                                            path = path.Replace("{##}", main.settings.comboBoxUserSelection.SelectedItem.ToString());
                                        });
                                       
                                    }



                                    if (Directory.Exists(path))
                                    {
                                        await main.Dispatcher.InvokeAsync(async () =>
                                        {
                                            profilelist = new ComboBox
                                            {

                                                Foreground = Brushes.Blue,
                                                FontSize = 16,
                                                Margin = new Thickness(5),
                                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                                VerticalAlignment = VerticalAlignment.Top
                                            };
                                        });

                                        string[] profileFolders = Directory.GetDirectories(path);

                                        foreach (string profileFolder in profileFolders)
                                        {
                                            string folderName = new DirectoryInfo(profileFolder).Name;
                                            if (folderName.StartsWith("Profile") || folderName.Equals("Default", StringComparison.OrdinalIgnoreCase) || folderName.EndsWith(".default-release", StringComparison.OrdinalIgnoreCase))
                                            {
                                                await main.Dispatcher.InvokeAsync(() =>
                                                {
                                                    profilelist.Items.Add(folderName);
                                                    profilelist.SelectedIndex = 0;
                                                });

                                            }
                                        }
                                        if (profilelist != null && profilelist.Items.Count > 0)
                                        {
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                profile = path + profilelist.Items[0];
                                            });

                                        }
                                        profileget = 1;
                                    }




                                }
                                else if (line.Contains("#profileget#") && profileget == 1)
                                {
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        string name = main.stringtokenizer(line, "=", 0);
                                        string dir = main.stringtokenizer(line, "=", 2);

                                        profile = profile.Replace("#profileget#", dir);

                                        path = profile + dir;


                                    });
                                }

                                if (!line.Contains("#profile#"))
                                {

                                    if (Directory.Exists(path) || System.IO.File.Exists(path))
                                    {

                                        if (recommended)
                                        {
                                            main.database.Add(name + "=" + path);
                                        }


                                        currentLine++;
                                        double progress = (double)currentLine / totalLines * 100;


                                        await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            main.progressBar1.Value = progress;

                                        });


                                        if (linecontains == 0)
                                        {
                                            if (groupboxmode == 1)
                                            {

                                                await main.Dispatcher.InvokeAsync(() =>
                                                {
                                                    if (created == 0)
                                                    {
                                                        groupBoxContent = new StackPanel
                                                        {
                                                            Orientation = Orientation.Vertical,
                                                            VerticalAlignment = VerticalAlignment.Top,
                                                            HorizontalAlignment = HorizontalAlignment.Left,
                                                        };

                                                        newExpander = new Expander
                                                        {
                                                            Header = groupboxcontent,
                                                            Margin = new Thickness(5),
                                                            Background = new SolidColorBrush(Colors.Transparent),
                                                            Foreground = main.brush,
                                                            BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                            BorderThickness = new Thickness(2),
                                                            FontSize = 14,
                                                            FontWeight = FontWeights.Regular,

                                                            HorizontalAlignment = HorizontalAlignment.Left,
                                                            VerticalAlignment = VerticalAlignment.Top
                                                        };
                                                        created = 1;
                                                    }



                                                    CheckBox newCheckBox = new CheckBox
                                                    {
                                                        Content = name + "=" + path + "=warning(" + "no warning" + ")",
                                                        Margin = new Thickness(10),
                                                        IsChecked = recommended,
                                                        BorderThickness = new Thickness(0),
                                                        BorderBrush = new SolidColorBrush(Colors.Transparent),


                                                        Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),

                                                        FontSize = 12,
                                                        FontFamily = new FontFamily("Segoe UI"),
                                                        FontWeight = FontWeights.Regular,
                                                        FontStyle = FontStyles.Normal,
                                                    };
                                                    main.checkboxes2.Add(newCheckBox);
                                                    if (haswarning != null)
                                                    {
                                                        newCheckBox.Content = name + "=" + path + "=warning(" + haswarning + ")";
                                                    }
                                                    newCheckBox.Checked += main.CheckBox_Checked;
                                                    newCheckBox.Unchecked += main.CheckBox_Unchecked;
                                                    groupBoxContent.Children.Add(newCheckBox);


                                                    newExpander.Content = groupBoxContent;
                                                    main.expanders.Add(newExpander);
                                                    try
                                                    {
                                                        main.wrapPanel1.Children.Add(newExpander);
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }



                                                });




                                            }
                                            else
                                            {
                                                await main.Dispatcher.InvokeAsync(() =>
                                                {
                                                    CheckBox newCheckBox = new CheckBox
                                                    {
                                                        Content = name + "=" + path,
                                                        Margin = new Thickness(5),
                                                        IsChecked = recommended,
                                                        BorderThickness = new Thickness(0),
                                                        BorderBrush = new SolidColorBrush(Colors.White),


                                                        Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        FontSize = 12,
                                                        FontFamily = new FontFamily("Segoe UI"),
                                                        FontWeight = FontWeights.Bold,
                                                        FontStyle = FontStyles.Normal,
                                                        Padding = new Thickness(10),
                                                        Cursor = Cursors.Hand
                                                    };
                                                    main.checkboxes2.Add(newCheckBox);

                                                    main.wrapPanel1.Children.Add(newCheckBox);
                                                    newCheckBox.Checked += main.CheckBox_Checked;
                                                    newCheckBox.Unchecked += main.CheckBox_Unchecked;
                                                });

                                            }
                                        }
                                    }
                                }



                            }


                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = "Scanning: ";
                                main.buttonStartScan.IsEnabled = true;

                                main.progressBar1.Value = 0;
                            });

                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + " " + ex.StackTrace);
                }



            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (settings.chkTrayIcon.IsChecked == true && TrayIcon.Visibility == Visibility.Visible)
            {
                this.Hide();
                settings.Hide();
            } else
            {
                Environment.Exit(0);
            }
           
        }
        private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
            settings.removeexception(file);
            
        }
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
            settings.addexception(file);
    
        }
      
       
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {

                string content = checkBox.Content.ToString();
                if(content.Contains("logscan"))
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);
                    database.Add(name + "=" + path + "=" + "logscan");
               
                } else
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);

                    database.Add(name + "=" + path);
                    string databasefile = Environment.CurrentDirectory + "\\" + "database.txt";
                  
                }
          
            }
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                string content = checkBox.Content.ToString();
                string name = stringtokenizer(content, "=", 0);
                string path = stringtokenizer(content, "=", 1);
                database.Remove(name + "=" + path);
                string databasefile = Environment.CurrentDirectory + "\\" + "database.txt";
                

            }
        }
   
        private double previousWidth, previousHeight, previousLeft, previousTop;
   
        private bool isMaximized = false;
       
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
                buttonMaximize.Content = "↔"; 
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
                buttonMaximize.Content = "↔";  
            }
        }
        public static byte[] getrandombytes(long size)
        {
            byte[] randombs = new byte[size];
            System.Security.Cryptography.RandomNumberGenerator.Create().GetNonZeroBytes(randombs);
            return randombs;
        }
     
        private string formatsize(long sizeinbytes)
        {
            double size = sizeinbytes;


            if (size >= 1L << 40)
                return $"{size / (1L << 40):0.##} TB";

            else if (size >= 1L << 30)
                return $"{size / (1L << 30):0.##} GB";

            else if (size >= 1L << 20)
                return $"{size / (1L << 20):0.##} MB";

            else if (size >= 1L << 10)
                return $"{size / (1L << 10):0.##} KB";

            else
                return $"{size} Bytes";
        }
        int cancelclean = 0;
        int reset = 0;
        public static class SystemActions
        {
         
            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

            const uint EWX_LOGOFF = 0x00000000;
            const uint EWX_SHUTDOWN = 0x00000001;
            const uint EWX_REBOOT = 0x00000002;
            const uint EWX_FORCE = 0x00000004;
            const uint EWX_FORCEIFHUNG = 0x00000010;

          
            public static bool LogOff()
            {
                return ExitWindowsEx(EWX_LOGOFF | EWX_FORCE, 0);
            }

         
            public static void Restart()
            {
                Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
             
            public static void Shutdown()
            {
                Process.Start(new ProcessStartInfo("shutdown", "/s /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
        }

        public class Clean
        {
            MainWindow main;
            FileInfo fileinfo;
            long totalsize;
            long cleaned;
            public Clean(MainWindow main)
            {
                this.main = main;
            }
            public async void run()
            {
                main.database.RemoveWhere(file => file.EndsWith("=logscan"));
                long size = main.database.Count();
                long nowcleaning = 0;
                TextBlock directorytextblock = null;
                totalsize = new System.IO.DriveInfo("c:\\").AvailableFreeSpace;
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Foreground = Brushes.Blue;
                });
                if(main.logfiles.Count > 0)
                {
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        directorytextblock = new TextBlock
                        {
                            Text = $"Cleaning: {"Deep Log Scan Files"}",
                            Foreground = Brushes.Blue,
                            FontSize = 16,
                           
                            Margin = new Thickness(5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top
                        };
                    });
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.wrapPanelDirectories.Children.Add(directorytextblock);
                    });
                    foreach (string file in main.logfiles)
                    {
                        if (main.cancelclean == 2)
                        {
                            break;
                        }
                        fileinfo = new FileInfo(file);
                 
                        double percentage = (double)nowcleaning / main.logfiles.Count * 100;
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.progressBar1.Value = percentage;
                            main.UpdateArc(main.progressBar1.Value, main.progressBar1.Maximum);
                        });
                        nowcleaning++;
                        if (fileinfo.Exists)
                        {
                            try
                            {
                              
                              
                                if (!main.settings.excludedfiles.Contains(file))
                                {
                                    fileinfo = new FileInfo(file);
                                    long filesize = fileinfo.Length;
                                 
                                    System.IO.File.Delete(file);
                                    
                                    cleaned += filesize;
                           
                                } else
                                {
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.label1_Copy.Text = "Skipping : " + file;
                                    });
                                }
                              
                            } catch (Exception ex)
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Text = "Cannot Delete : " + file + " " + ex.Message;
                                });
                                continue;
                            }
                        }
                        else
                        {
                         
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = "File not found: " + file;
                            });
                            continue;
                        }
                    }
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        directorytextblock.Text = "Cleaned: " + "Deep Log Scan Files" + " " + main.formatsize(cleaned);
                    });
                    cleaned = 0;
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.progressBar1.Value = 0;
                        main.UpdateArc(main.progressBar1.Value, main.progressBar1.Maximum);
                        nowcleaning = 0;
                    });
                }
           
                foreach (string file in main.database)
                {
                    if (main.cancelclean == 2)
                    {
                        return;
                    }
                    string name = main.stringtokenizer(file, "=", 0);
                    string path = main.stringtokenizer(file, "=", 1);

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        directorytextblock = new TextBlock
                        {
                            Text = $"Cleaning: {name}",
                            Foreground = Brushes.Blue,
                            FontSize = 16,
                            Margin = new Thickness(5),
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Top
                        };
                        main.wrapPanelDirectories.Children.Add(directorytextblock);
                    });
             

                    try
                    {
                        cleaned = 0;
                        nowcleaning++;
                        double percentage = (double)nowcleaning / size * 100;
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.progressBar1.Value = percentage;
                            main.UpdateArc(main.progressBar1.Value, main.progressBar1.Maximum);
                        });

                        if (System.IO.File.Exists(path))
                        {
                            if (!main.settings.excludedfiles.Contains(file))
                            {

                                fileinfo = new FileInfo(path);
                                long filesize = fileinfo.Length;
                             
                                System.IO.File.Delete(path);

                                cleaned += filesize;
                            }
                            else
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Text = "Skipping : " + file;
                                });
                            }
                        }
                        else if (Directory.Exists(path))
                        {
                            await cleandir(path, directorytextblock, name);
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                directorytextblock.Text = "Cleaned: " + name + " " + main.formatsize(cleaned);

                            });
                        }
                        else
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = "File not found: " + path;
                            });

                        }

                    }
                    catch (Exception ex)
                    {

                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Cannot delete: " + path + " " + ex.Message;
                        });
                    }
                }

                await main.Dispatcher.InvokeAsync(() =>
                {
                    if(main.autoclean == 1)
                    {
                        long tltlsize = new System.IO.DriveInfo("c:\\").AvailableFreeSpace - totalsize;
                        main.label1_Copy.Text = "Auto Clean done! " + main.formatsize(tltlsize) + " Cleaned. " + DateTime.Now;
                     

                    } else
                    {
                        long tltlsize = new System.IO.DriveInfo("c:\\").AvailableFreeSpace - totalsize;
                        main.label1_Copy.Text = "Cleaning done! " + main.formatsize(tltlsize) + " Cleaned. " + DateTime.Now;
                    }
                 
                    main.label1_Copy.Foreground = Brushes.Green;
                   
                    main.reset = 1;
                    main.cancelclean = 0;
                    main.cancelstatus = 0;
                    main.autoclean = 0;
                    main.onclean = 0;
                    var settings = main.settings;
                    var selectedActionItem = settings.cmbPostCleanupAction.SelectedItem as ComboBoxItem;
                    string action = selectedActionItem?.Content?.ToString();

                    if (string.IsNullOrEmpty(action)) return;

                    switch (action)
                    {
                        case "Restart":
                            SystemActions.Restart();
                            break;
                        case "LogOff":
                            SystemActions.LogOff();
                            break;
                        case "Shutdown":
                            SystemActions.Shutdown();
                            break;
                    }
                });
            }

            public async Task cleandir(string path, TextBlock directorytextblock, string name)
            {
                HashSet<string> files = Directory.GetFiles(path).ToHashSet<string>();
                HashSet<string> dirs = Directory.GetDirectories(path).ToHashSet<string>();
                await Task.WhenAll(files.Select(async file =>
                {
                    if (main.cancelclean == 2)
                    {
                        return;
                    }
                    try
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Deleting file: " + file;
                        });
                        if (!main.settings.excludedfiles.Contains(file))
                        {
                            fileinfo = new FileInfo(file);
                            long filesize = fileinfo.Length;


                            System.IO.File.Delete(file);

                            cleaned += filesize;
                          
                        }
                        else
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = "Skipping : " + file;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Cannot delete file: " + file + " " + ex.Message;
                        });
                    }
                }));

                await Task.WhenAll(dirs.Select(async dir =>
                {
                    if (main.cancelclean == 2)
                    {
                        return;
                    }
                    try
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Deleting directory: " + dir;
                        });

                        await cleandir(dir, directorytextblock, name);
                        Directory.Delete(dir);
                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = "Cannot delete directory: " + dir + " " + ex.Message;
                        });
                    }
                }));
              

            }
        }
            private void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
            if (buttonStartScan.Content == "Clean")
            {
                buttonStartScan.IsEnabled = false;
                cancelstatus = 0;
                scanstatus = 0;
                cancelclean = 1;

                Clean clean = new Clean(this);
                Thread t = new Thread(new ThreadStart(clean.run));
                t.Start();
                wrapPanelDirectories.Children.Clear();
            }
            else if (buttonStartScan.Content.Equals("Scan"))
            {

                wrapPanel1.Visibility = Visibility.Hidden;

                buttonStartScan.Content = "Cancel";
                Scan scan = new Scan(this);
                Thread t = new Thread(new ThreadStart(scan.run));
                t.Start();

                cancelclean = 0;
                cancelstatus = 0;
                scanstatus = 0;
            } else if (buttonStartScan.Content == "Cancel")
            {
                logfiles.Clear();
                if (scanstatus == 1)
                {

                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Scan";

                }
                else if (scanstatus == 2)
                {
                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Scan";
                    cancelstatus = 1;
                }
                else if (cancelclean == 1)
                {

                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    cancelclean = 2;
                    buttonStartScan.Content = "Scan";
                }
                else if (reset == 1)
                {

                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Scan";


                    reset = 0;
                }
            } 


        }

     
        
  
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
           
            settings.Show();
            settings.WindowState = WindowState.Normal;
        }
        Utilities utilities;
        int doit = 0;
        private void UtilitiesButton_Click(object sender, RoutedEventArgs e)
        {
           
                utilities.Show();
                settings.WindowState = WindowState.Normal;


        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

        }
      
        private void button2_Click(object sender, RoutedEventArgs e)
        {
     
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {

        }
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        private void ToggleThemeSwitch_Checked(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
            string fileContent = System.IO.File.ReadAllText(path);

            fileContent = fileContent.Replace("themes:0", "").TrimEnd().ToLower();

            if (!fileContent.Contains("themes:1"))
            {
                if (!fileContent.EndsWith(Environment.NewLine))
                    fileContent += Environment.NewLine;

                fileContent += "themes:1" + Environment.NewLine;
                System.IO.File.WriteAllText(path, fileContent);
            }

           
            string themePath = "Themes/Dark.xaml";
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };
            brush = (SolidColorBrush)resourceDictionary["Text"];

            themeselector.selector(new Uri(themePath, UriKind.Relative));

            foreach (CheckBox box in checkboxes2)
                box.Foreground = brush;

            foreach (Expander er in expanders)
                er.Foreground = brush;
        }

        private void ToggleThemeSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");

             
            string fileContent = System.IO.File.ReadAllText(path);
             
            fileContent = fileContent.Replace("themes:1", "").TrimEnd();

           
            if (!fileContent.Contains("themes:0"))
            {
              
                if (!fileContent.EndsWith(Environment.NewLine))
                    fileContent += Environment.NewLine;

                fileContent += "themes:0" + Environment.NewLine;
                System.IO.File.WriteAllText(path, fileContent);
            }

           
            string themePath = "Themes/Light.xaml";

            
            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

           
            brush = (SolidColorBrush)resourceDictionary["Text"];

         
            themeselector.selector(new Uri(themePath, UriKind.Relative));

             
            foreach (CheckBox box in checkboxes2)
                box.Foreground = brush;
             
            foreach (Expander er in expanders)
                er.Foreground = brush;
        }
 

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
using Hardcodet.Wpf.TaskbarNotification;
using LibreHardwareMonitor.Hardware;
using MFK;
using Microsoft.VisualBasic.Logging;
using MultronWinCleaner;
using System;
using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using System.Windows.Threading;
using System.Xml.Linq;
using static Multron_Win_Cleaner.MainWindow;
using static Multron_Win_Cleaner.MainWindow.Scan;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;


namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {


        public Settings settings;
        MainViewModel viewModel = new MainViewModel();
        MainViewModel viewModelbac = new MainViewModel();
        List<string> logfiles = new List<string>();
        SolidColorBrush brush;
        List<CheckBox> checkboxes2 = new List<CheckBox>();
        List<StackPanel> stackpanels = new List<StackPanel>();
        List<Expander> expanders = new List<Expander>();
        List<ListBox> listboxes = new List<ListBox>();
        byte autoclean = 0;
        byte onclean = 0;
       
        public MainWindow()
        {
            InitializeComponent();
        
            

        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            utilities = new Utilities(this);


            utilities.Show();
            utilities.Hide();


            progressBar1.ValueChanged += ProgressBar1_ValueChanged;
            settings = new Settings(utilities.memcleaner, utilities, this, utilities.startupmanager);

            settings.Show();
            settings.Hide();
            dataGridGroups.Visibility = Visibility.Hidden;
            Datagridscroll.Visibility = Visibility.Hidden;


            
            if (!System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt"))
            {
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
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkboxes2.Add(malscan);


            ComboBox scanOptionsComboBox = new ComboBox
            {
                Margin = new Thickness(10, 0, 10, 10),
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                Foreground = new SolidColorBrush(Colors.Black),
                Background = new SolidColorBrush(Colors.White),
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                IsEnabled = true,
                ItemsSource = new List<string> { "Full Scan", "Quick Scan", "Custom Scan" },
                SelectedIndex = 0
            };


            malscan.Checked += (s, e) => scanOptionsComboBox.IsEnabled = true;
            malscan.Unchecked += (s, e) => scanOptionsComboBox.IsEnabled = false;


            StackPanel groupBoxContent2 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackpanels.Add(groupBoxContent2);


            groupBoxContent2.Children.Add(malscan);
            groupBoxContent2.Children.Add(scanOptionsComboBox);


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
                IsEnabled = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };
            newExpander1.Content = groupBoxContent2;
            expanders.Add(newExpander1);


         

            try
            {
                wrapPanel1.Children.Add(newExpander1);
            }
            catch (Exception)
            {
            }
            CheckBox newCheckBox3 = new CheckBox
            {
                Content = "WinSxS Folder" + "=" + "C:\\Windows\\WinSxS" + "=warning=(No Warning)" + "=" + "winsxs",
                Margin = new Thickness(10),
                IsChecked = true,
                BorderThickness = new Thickness(0),
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                Foreground = brush,
                FontSize = 12,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                FontWeight = FontWeights.Regular,
                FontStyle = FontStyles.Normal,
            };
            checkboxes2.Add(newCheckBox3);
            database.Add("WinSxS Folder=C:\\Windows\\WinSxS=winsxs");
            StackPanel groupBoxContent5 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left

            };
            stackpanels.Add(groupBoxContent5);
            Expander newExpander4 = new Expander
            {
                Header = "WinSxS Folder",
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
            groupBoxContent5.Children.Add(newCheckBox3);
            newExpander4.Content = groupBoxContent5;
            expanders.Add(newExpander4);
            newCheckBox3.Checked += CheckBox_Checked;
            newCheckBox3.Unchecked += CheckBox_Unchecked;
            try
            {
                wrapPanel1.Children.Add(newExpander4);
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
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
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
            groupBoxContent3.Children.Add(newCheckBox);
            newExpander2.Content = groupBoxContent3;
            expanders.Add(newExpander2);
          
            try
            {
                wrapPanel1.Children.Add(newExpander2);
            }
            catch (Exception)
            {

            }
            newCheckBox.Checked += CheckBox_Checked;
            newCheckBox.Unchecked += CheckBox_Unchecked;
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {

                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                var load = new Load(this);
                await Task.Run(() => load.RunAsync());


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
           
        }
        private void ProgressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateArc(progressBar1.Value);
        }
 
         
        private System.Windows.Shapes.Path _arcPath;
        private void UpdateArc(double percent)
        {
            
            percent = Math.Max(0, Math.Min(100, percent));

            double angle = 360 * (percent / 100);
            double radius = progressBar1.ActualWidth / 2 - 7.5;
            var center = new System.Windows.Point(progressBar1.ActualWidth / 2, progressBar1.ActualHeight / 2);

            if (_arcPath == null)
                _arcPath = (System.Windows.Shapes.Path)progressBar1.Template.FindName("PART_Indicator", progressBar1);

            if (_arcPath == null) return;

            Geometry g;

            if (percent <= 0)
            {
                g = Geometry.Empty;
            }
            else if (percent >= 99.999) 
            {
                g = new EllipseGeometry(center, radius, radius);
            }
            else
            {
                var start = new System.Windows.Point(center.X, center.Y - radius);

                double rad = (Math.PI / 180) * (angle - 90);
                var end = new System.Windows.Point(
                    center.X + radius * Math.Cos(rad),
                    center.Y + radius * Math.Sin(rad));

                bool largeArc = angle > 180;

                var fig = new PathFigure { StartPoint = start };
                fig.Segments.Add(new ArcSegment(
                    end,
                    new System.Windows.Size(radius, radius),
                    0,
                    largeArc,
                    SweepDirection.Clockwise,
                    true));

                g = new PathGeometry(new[] { fig });
            }

            _arcPath.Data = g;
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
        public List<string> database = new List<string>();
        public int scanstatus = 0;
        public int cancelstatus = 0;
    
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
                            if (selectedContent == "Custom Days" && (dayCheckBox == null || dayCheckBox.IsChecked != true))
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

                            startTime = new DateTime(now.Year, now.Month, now.Day, startTime.Hour, startTime.Minute, 0);
                            endTime = new DateTime(now.Year, now.Month, now.Day, endTime.Hour, endTime.Minute, 0);

                         
                            if ((selectedContent == "Daily" || selectedContent == "Weekly" || selectedContent == "Custom Days"))
                            {
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
        private string formatsize(long size)
        {
            if (size < 0)
                return "0 Byte";

            string[] sizes = { "Byte", "KB", "MB", "GB", "TB" };
            double len = size;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
        private void FilesListBox_Loaded(object sender, RoutedEventArgs e)
        {
            var listBox = sender as ListBox;
            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += FilesListBox_ScrollChanged;
            }
        }

        private bool isLoading = false;

        private async void FilesListBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 100)
            {
                if (sender is ScrollViewer scrollViewer && scrollViewer.DataContext is GroupViewModel vm)
                {
                    if (isLoading) return;  

                    isLoading = true;
                    await vm.LoadMoreFilesAsync();
                    isLoading = false;
                }
            }
        }
         
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T t)
                    return t;

                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        } 
        public static T FindVisualParent<T>(DependencyObject obj) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is T t)
                    return t;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
        public class Scan
        {
            MainWindow main;
            long totalsize = 0;
            int winsxs = 0;
            CancellationTokenSource cts = new CancellationTokenSource();
            List<(string file, long size, string path)> checkboxData = new List<(string file, long size, string path)>();

            public Scan(MainWindow main)
            {
                this.main = main;
            }

        
          

            public class MainViewModel : INotifyPropertyChanged
            {
                public ObservableCollection<GroupViewModel> Groups { get; set; } = new ObservableCollection<GroupViewModel>();

                private GroupViewModel selectedGroup;
                public GroupViewModel SelectedGroup
                {
                    get => selectedGroup;
                    set
                    {
                        selectedGroup = value;
                        OnPropertyChanged();
                    }
                }

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public class PathGroup : INotifyPropertyChanged
            {
                public string Path { get; set; }
                public ObservableCollection<FileItem> Files { get; set; } = new ObservableCollection<FileItem>();

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string propName = null) =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            }

            public class FileItem : INotifyPropertyChanged
            {
               
                public string FileName { get; set; }
                public long SizeBytes { get; set; }
                public string File => $"File to delete={FileName}={formatsize(SizeBytes)}";

                private bool isChecked;
                public bool IsChecked
                {
                    get => isChecked;
                    set
                    {
                        isChecked = value;
                        OnPropertyChanged();
                    }
                }

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string name = null) =>
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                private string formatsize(long size)
                {
                    if ((size < 0))
                        return "0 Byte";

                    string[] sizes = { "Byte", "KB", "MB", "GB", "TB" };
                    double len = size;
                    int order = 0;
                    while (len >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        len /= 1024;
                    }
                    return $"{len:0.##} {sizes[order]}";
                }
            }

            public class GroupViewModel : INotifyPropertyChanged
            {
                private const int PageSize = 100;
                private int currentLoadedCount = 0;

                public string Path { get; private set; }

                public ObservableCollection<FileItem> Files { get; private set; } = new ObservableCollection<FileItem>();

                private List<FileItem> allFiles;

                public long TotalSizeBytes { get; private set; }
                public string TotalSizeFormatted => formatsize(TotalSizeBytes);
                public string ExpanderHeader => $"{Path} ({TotalSizeFormatted})";

                public GroupViewModel(string path, List<FileItem> allFiles)
                {
                    this.Path = path;
                    this.allFiles = allFiles;
                    this.TotalSizeBytes = allFiles.Sum(f => f.SizeBytes);

                    _ = LoadMoreFilesAsync();
                }
                private string formatsize(long size)
                {
                    if ((size < 0))
                        return "0 Byte";

                    string[] sizes = { "Byte", "KB", "MB", "GB", "TB" };
                    double len = size;
                    int order = 0;
                    while (len >= 1024 && order < sizes.Length - 1)
                    {
                        order++;
                        len /= 1024;
                    }
                    return $"{len:0.##} {sizes[order]}";
                }


                public async Task LoadMoreFilesAsync()
                {
                    int remaining = allFiles.Count - currentLoadedCount;
                    if (remaining <= 0) return;

                    int toLoad = Math.Min(PageSize, remaining);

                    for (int i = currentLoadedCount; i < currentLoadedCount + toLoad; i++)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            Files.Add(allFiles[i]);
                        }, DispatcherPriority.Background);
                    }

                    currentLoadedCount += toLoad;
                    OnPropertyChanged(nameof(Files));
                }

                public event PropertyChangedEventHandler PropertyChanged;
                protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                }

            
            }
            public async Task ScandotsAsync(string text, CancellationToken cancellationToken)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if (main.cancelstatus == 1)
                            break;
                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + ".";
                            main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "..";
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "...";
                        });

                        await Task.Delay(1000, cancellationToken);
                    }
                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = "Cancel Requested Please Wait!";
                        main.label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                    });
                }
                catch (TaskCanceledException)
                {
                
                }
            }
            private string formatsize(long size)
            {
                if ((size < 0))
                    return "0 Byte";

                string[] sizes = { "Byte", "KB", "MB", "GB", "TB" };
                double len = size;
                int order = 0;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
          
            private static Task<string> RunDismAnalyzeComponentStoreAsync()
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = "/Online /Cleanup-Image /AnalyzeComponentStore",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var tcs = new TaskCompletionSource<string>();
                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };

                string stdOut = "", stdErr = "";
                proc.OutputDataReceived += (s, e) => { if (e.Data != null) stdOut += e.Data + "\n"; };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) stdErr += e.Data + "\n"; };
                proc.Exited += (s, e) =>
                {
                    if (proc.ExitCode == 0)
                    {
                        tcs.SetResult(stdOut);
                    }
                    else
                    {
                        Console.WriteLine($"[DISM ERROR] Exit Code: {proc.ExitCode}");
                        Console.WriteLine($"[DISM ERROR] StdOut: \n{stdOut}");
                        Console.WriteLine($"[DISM ERROR] StdErr: \n{stdErr.Trim()}");
                        tcs.SetException(new InvalidOperationException(
                            $"DISM exited with code {proc.ExitCode}. StdErr: {stdErr.Trim()}"));
                    }
                    proc.Dispose();
                };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                return tcs.Task;
            }

     
            private static readonly Regex SizeLineRx = new Regex(
                @"^(?:\s*Actual (?:Component Store |Size of Component )?Size\s*|\s*Potentially Reclaimable Size\s*|\s*Backups and Disabled Features\s*):\s*([\d\.]+)\s*(KB|MB|GB|TB)$",
                RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
           

            private static (long Actual, long Reclaimable, long Backups) ParseSizes(string text)
            {
                long actual = -1;
                long reclaimable = -1;
                long backups = -1;

              
                Console.WriteLine("\n--- Raw Text for ParseSizes ---");
                Console.WriteLine(text);
                Console.WriteLine("-------------------------------\n");


                foreach (Match m in SizeLineRx.Matches(text))
                {
                    string key = m.Groups[1].Value;  
                    string num = m.Groups[1].Value;  
                    string unit = m.Groups[2].Value.ToUpperInvariant(); 

              
                    string fullLineMatch = m.Value; 

                    Console.WriteLine($"[DEBUG] Matched Full Line: '{fullLineMatch}'");
                    Console.WriteLine($"[DEBUG] Parsed: Number='{num}', Unit='{unit}'");

                    if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                    {
                        Console.WriteLine($"[DEBUG] Failed to parse number '{num}'");
                        continue;
                    }

                    long bytes = unit switch
                    {
                        "KB" => (long)Math.Round(val * 1_024L),
                        "MB" => (long)Math.Round(val * 1_024L * 1_024L),
                        "GB" => (long)Math.Round(val * 1_024L * 1_024L * 1_024L),
                        "TB" => (long)Math.Round(val * 1_024L * 1_024L * 1_024L * 1_024L),
                        _ => -1
                    };
 
                    if (fullLineMatch.IndexOf("Actual ", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        fullLineMatch.IndexOf(" Size", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        actual = bytes;
                    }
                    else if (fullLineMatch.IndexOf("Potentially Reclaimable", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        reclaimable = bytes;
                    }
                    else if (fullLineMatch.IndexOf("Backups and Disabled Features", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        backups = bytes;
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] Unrecognized line type: '{fullLineMatch}'");
                    }
                }

                Console.WriteLine($"[DEBUG] Parsed → Actual={actual}, Reclaimable={reclaimable}, Backups={backups}");
                return (actual, reclaimable, backups);
            }

            public async Task run()
            {
                try
                {
                    long size = main.database.Count();
                    long nowscanning = 0;
                    main.scanstatus = 2;
                    main.onclean = 1;
                
                    await main.Dispatcher.InvokeAsync(() =>
                    {

                   
                        var task = ScandotsAsync("Scanning", cts.Token);
                        main.wrapPanelDirectories.Visibility = Visibility.Visible;
                        main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                        main.progressBar1.Value = 0;
                    });

                    foreach (string directory in main.database)
                    {
                        if (main.cancelstatus == 1) break;

                        string name = main.stringtokenizer(directory, "=", 0);
                        string path = main.stringtokenizer(directory, "=", 1);
                        TextBlock directorytextblock = null;

                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            directorytextblock = new TextBlock
                            {
                                Text = $"Scanning: {name}",
                                Foreground = System.Windows.Media.Brushes.Goldenrod,
                                FontSize = 16,
                                Margin = new Thickness(5)
                            };
                            main.wrapPanelDirectories.Children.Add(directorytextblock);
                        });
                        if(directory.EndsWith("winsxs"))
                        {
                            string output = await RunDismAnalyzeComponentStoreAsync();

                            string appFolder = System.IO.Path.Combine(
   Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
   "Multron Win Cleaner");
                            Directory.CreateDirectory(appFolder);
                            string outputPath = System.IO.Path.Combine(appFolder, "dism_scan.log");
                        
                            await System.IO.File.WriteAllTextAsync(outputPath, output);
                            
                            var sizes = ParseSizes(output);
                            long actual = Math.Max(0, sizes.Actual);
                            long reclaimable = Math.Max(0, sizes.Reclaimable);
                            long backups = Math.Max(0, sizes.Backups);
                            long total = actual + reclaimable + backups;
                            totalsize += total;
                          
                            checkboxData.Add(("WinSxS Folder=C:\\Windows\\WinSxS", total, "C:\\Windows\\WinSxS"));
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                directorytextblock = new TextBlock
                                {
                                    Text = $"Completed: {name}",
                                    Foreground = System.Windows.Media.Brushes.Goldenrod,
                                    FontSize = 16,
                                    Margin = new Thickness(5)
                                };
                                main.wrapPanelDirectories.Children.Add(directorytextblock);
                            });
                        } else
                        {
                            if (directory.EndsWith("logscan"))
                            {
                                await ScanCDirectoryAsync(path, name, directorytextblock);

                            }

                            else if (System.IO.File.Exists(path))
                            {
                                long filelength = new FileInfo(path).Length;
                                totalsize += filelength;
                                checkboxData.Add((path, filelength, name));

                            }

                            else
                            {
                                await ScanDirectoryAsync(path, name, directorytextblock);
                            }


                            nowscanning++;
                            double percent = (double)nowscanning / size * 100;
                            await main.Dispatcher.InvokeAsync(() => main.progressBar1.Value = percent);
                        }
                    
                    }

                    await main.Dispatcher.InvokeAsync(async () =>
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();


                        main.wrapPanelDirectories.Children.Clear();
                        main.wrapPanelDirectories.Visibility = Visibility.Hidden;
                        main.wrapPanel1.Visibility = Visibility.Hidden;
                        main.dataGridGroups.Visibility = Visibility.Visible;
                        main.ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
                        main.Datagridscroll.Visibility = Visibility.Visible;
                        var task = ScandotsAsync("Scanning", cts.Token);
                        main.progressBar1.Value = 0;

                        main.DataContext = main.viewModel;

                        var groupedByPath = await Task.Run(() => checkboxData.GroupBy(x => x.path).ToList());

                        int totalGroups = groupedByPath.Count;
                        int currentGroup = 0;

                        foreach (var group in groupedByPath)
                        {
                            string path = group.Key;
                            var items = group.ToList();

                            var allFiles = items.Select(item => new FileItem
                            {
                              
                                FileName = item.file.Contains("WinSxS", StringComparison.OrdinalIgnoreCase)
    ? $"Clean WinSxS Folder={item.file}={formatsize(item.size)}"
    : $"File to delete={item.file}={formatsize(item.size)}",

                                SizeBytes = item.size,
                                IsChecked = true
                            }).ToList();

                            GroupViewModel groupVm = new GroupViewModel(path, allFiles);
                          
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.viewModel.Groups.Add(groupVm);

                                currentGroup++;
                                main.label1_Copy.Text = $"Loading group {currentGroup} / {totalGroups}";
                                main.progressBar1.Value = (double)currentGroup / totalGroups * 100;
                            });

                            await Task.Delay(10);
                        }

                        cts.Cancel();
                        main.label1_Copy.Text = "Loading Completed!";
                        main.progressBar1.Value = 100;
                        main.label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                        main.buttonReset.Visibility = Visibility.Visible;
                        main.buttonStartScan.IsEnabled = true;
                        main.buttonStartScan.Content = "Clean";
                        if (main.autoclean == 1)
                        {
                            main.buttonStartScan.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            main.label1_Copy.Text = $"Auto scan completed! + {formatsize(totalsize)}  Useless file found! {DateTime.Now}";
                        }
                        else
                        {
                            main.label1_Copy.Text = $"Scan completed! + {formatsize(totalsize)}  Useless file found! {DateTime.Now}";
                        }

                       
                    });

                    main.scanstatus = 1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }

             

            public async Task ScanCDirectoryAsync(string directory, string name, TextBlock directorytextblock)
            {
                long dirSize = await GetCDirectorySizeAsync(directory);
                totalsize += dirSize;
                await directorytextblock.Dispatcher.InvokeAsync(() =>
                    directorytextblock.Text = $"Completed: {name} {formatsize(dirSize)}");
            }

            public async Task<long> GetCDirectorySizeAsync(string path)
            {
                long size = 0;
                try
                {
                    if (Directory.Exists(path))
                    {
                        foreach (string dir in Directory.GetDirectories(path))
                        {
                            if (main.cancelstatus == 1) break;
                            size += await GetCDirectorySizeAsync(dir);
                        }
                          
                    

                        foreach (string file in Directory.GetFiles(path))
                        {
                            if (main.cancelstatus == 1) break;

                            if (!main.settings.excludedfiles.Contains(file))
                            {
                                if (".log.etl.dmp.trace.tmp.temp.bak.swp".Split('.').Any(ext => file.EndsWith($".{ext}")))
                                {
                                    long fSize = new FileInfo(file).Length;
                                    size += fSize;
                                    totalsize += fSize;
                                    checkboxData.Add((file, fSize, "Deep log scans finded"));
                                }
                            }
                        }
                    }
                }
                catch { }

                return size;
            }

            public async Task ScanDirectoryAsync(string directory, string name, TextBlock directorytextbox)
            {
                long dirSize = await GetDirectorySizeAsync(directory);
                totalsize += dirSize;
                await directorytextbox.Dispatcher.InvokeAsync(() =>
                    directorytextbox.Text = $"Completed: {name} {formatsize(dirSize)}");
            }

            public async Task<long> GetDirectorySizeAsync(string path)
            {
                long size = 0;
                try
                {
                    if (Directory.Exists(path))
                    {
                        foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                        {
                            if (main.cancelstatus == 1) break;
                            if (!main.settings.excludedfiles.Contains(file))
                            {
                                FileInfo fi = new FileInfo(file);
                                size += fi.Length;
                                checkboxData.Add((file, fi.Length, path));
                            }
                        }
                    }
                }
                catch { }

                return size;
            }

          
        }
      
        public class Load
        {
            MainWindow main;
            CancellationTokenSource cts = new CancellationTokenSource();
            string currentid = null;
            int comboid = 0;
            public Load(MainWindow main)
            {
                this.main = main;
             
            }
            public async Task ScandotsAsync(string text, CancellationToken cancellationToken)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + ".";
                            main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "..";
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "...";
                        });

                        await Task.Delay(1000, cancellationToken);
                    }
                }
                catch (TaskCanceledException)
                {

                }
            }
            private void Profilelist_SelectionChanged(object sender, SelectionChangedEventArgs e, int comboid)
            {
                try
                {
                    ComboBox comboBox = sender as ComboBox;
                    if (comboBox == null) return;

                    string newText = (comboBox.SelectedItem?.ToString() ?? "").Trim();

                    foreach (var oldItem in e.RemovedItems)
                    {
                        string oldText = oldItem?.ToString()?.Trim() ?? "";

                      
                        Expander parentExpander = FindParent<Expander>(comboBox);
                        string expanderName = parentExpander.Header?.ToString()?.Trim() ?? "";
                        if (parentExpander == null) return;
                    
                        var checkBoxes = FindChildren<CheckBox>(parentExpander);
                        foreach (var box in checkBoxes)
                        {
                            string contentText = box.Content?.ToString() ?? "";
                           
                            if (contentText.Contains(oldText))
                            {
                                string updatedContent = contentText.Replace(oldText, newText);
                                box.Content = updatedContent;

                                for (int i = 0; i < main.database.Count; i++)
                                {
                                 
                                    if (main.database[i].Contains(expanderName))
                                    {
                                        string key = main.stringtokenizer(main.database[i], "=", 0);
                                        string newValue = main.stringtokenizer(updatedContent, "=", 1);
                                        string newEntry = key + "=" + newValue;
                                         
                                        main.database[i] = newEntry;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata: " + ex.Message + "\n" + ex.StackTrace);
                }
            }
            public static IEnumerable<T> FindChildren<T>(DependencyObject parent) where T : DependencyObject
            {
                if (parent == null) yield break;

                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                    if (child is T t)
                        yield return t;

                    foreach (T descendant in FindChildren<T>(child))
                        yield return descendant;
                }
            }
            public static T FindParent<T>(DependencyObject child) where T : DependencyObject
            {
                DependencyObject parent = VisualTreeHelper.GetParent(child);
                while (parent != null)
                {
                    if (parent is T typedParent)
                        return typedParent;

                    parent = VisualTreeHelper.GetParent(parent);
                }
                return null;
            }
            private Expander FindExpanderFromScrollViewer(ScrollViewer scrollViewer)
            {
                DependencyObject current = scrollViewer;

                while (current != null)
                {
                    if (current is Expander expander)
                        return expander;

                    current = VisualTreeHelper.GetParent(current);
                }

                return null;
            }
            private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
            {
                var scrollViewer = sender as ScrollViewer;




                double verticalOffset = scrollViewer.VerticalOffset;
                double viewportHeight = scrollViewer.ViewportHeight;
                double extentHeight = scrollViewer.ExtentHeight;

                if (extentHeight <= 0 || viewportHeight <= 0) return;

                const double threshold = 20.0;


                bool isAtBottom = (extentHeight - (verticalOffset + viewportHeight)) <= threshold;

                if (!isAtBottom) return;
 

                var listbox = FindListBoxFromScrollViewer(scrollViewer);
                var expander = FindExpanderFromScrollViewer(scrollViewer);

                if (listbox != null && expander != null)
                {
                    string groupid = expander.Name;   
                    await LoadMoreItemsForListBox_ByGroup(listbox, groupid);
                    

                    
                }
 
            }
            private ListBox FindListBoxFromScrollViewer(ScrollViewer sv)
            {
                foreach (var lb in main.listboxes)
                {
                    if (VisualTreeHelper.GetChildrenCount(lb) > 0)
                    {
                        var border = VisualTreeHelper.GetChild(lb, 0) as Border;
                        if (border != null && VisualTreeHelper.GetChildrenCount(border) > 0)
                        {
                            var scrollViewer = VisualTreeHelper.GetChild(border, 0) as ScrollViewer;
                            if (scrollViewer == sv)
                                return lb;
                        }
                    }
                }
                return null;
            }
            private async Task LoadMoreItemsForListBox_ByGroup(ListBox listbox, string groupid)
            {
                var allCheckboxes = main.checkboxes2
                    .Where(cb => cb?.Name != null && cb.Name.ToString() == groupid)
                    .ToList();

                var existingBoxes = listbox.Items
                    .OfType<CheckBox>()
                    .ToList();
                 
           
                int startIndex = 0;

                if (existingBoxes.Any())
                {
                    var lastBox = existingBoxes.Last();
                    
              
                    startIndex = allCheckboxes.FindLastIndex(cb => cb.Name == lastBox.Name && Equals(cb.Content, lastBox.Content)) + 1;

                  

                    if (startIndex < 0) startIndex = 0;
                }

                var nextBatch = allCheckboxes.Skip(startIndex).Take(3).ToList();
 

                await main.Dispatcher.InvokeAsync(() =>
                {
                    nextBatch.ForEach(originalBox =>
                    {
                        var newBox = new CheckBox
                        {
                            Name = originalBox.Name,
                            Content = originalBox.Content,
                            IsChecked = originalBox.IsChecked,
                            Margin = originalBox.Margin,
                            BorderThickness = new Thickness(0),
                            BorderBrush = new SolidColorBrush(Colors.Transparent),
                            Background = new SolidColorBrush(Colors.White),
                            Foreground = main.brush,
                            FontSize = 12,
                            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
                            FontWeight = FontWeights.Regular,
                            FontStyle = FontStyles.Normal,
                        };

                        newBox.Checked += main.CheckBox_Checked;
                        newBox.Unchecked += main.CheckBox_Unchecked;

                        listbox.Items.Add(newBox);

                     
                    });
                });

          
            }
            public string CreateRandomId(int length = 8)
            {
                const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

                Random rnd = new Random();

             
                char firstChar = letters[rnd.Next(letters.Length)];

              
                string rest = new string(Enumerable.Repeat(chars, length - 1)
                    .Select(s => s[rnd.Next(s.Length)]).ToArray());

                return firstChar + rest;
            }
            public async Task RunAsync()
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
                                var task = ScandotsAsync("Loading Database", cts.Token);
                                main.buttonStartScan.IsEnabled = false;


                            });
                            int groupboxmode = 0;
                            int created = 0;
                            int profileget = 0;
                            string groupboxcontent = "";
                            GroupBox newGroupBox = null;
                            ListBox groupBoxContent = null;
                            Expander newExpander = null;
                            ComboBox profilelist = null;
                            ScrollViewer scrollViewer = null;
                            string profile = "";
                            while ((line = reader.ReadLine()) != null)
                            {
                                currentLine++;
                                double progress = (double)currentLine / totalLines * 100;
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
                                            groupBoxContent.Items.Add(profilelist);
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
                                                Foreground = System.Windows.Media.Brushes.Blue,
                                                FontSize = 16,
                                                Margin = new Thickness(5),
                                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                                VerticalAlignment = VerticalAlignment.Top
                                            };


                                            comboid++;
                                            profilelist.SelectionChanged += (s, e) => Profilelist_SelectionChanged(s, e, comboid);
                                        });

                                        string[] profileFolders = Directory.GetDirectories(path);
                                        int i = 0;
                                        int selectedindex = 0;
                                        foreach (string profileFolder in profileFolders)
                                        {
                                         
                                            string folderName = new DirectoryInfo(profileFolder).Name;
                                          
                                                await main.Dispatcher.InvokeAsync(() =>
                                                {
                                                    profilelist.Items.Add(folderName); 
                                                 
                                                });


                                        }
                                     
                                        foreach(string name1 in profilelist.Items)
                                        {

                                   
                                            if (name1.StartsWith("Profile") ||name1.Contains("Default", StringComparison.OrdinalIgnoreCase) || name1.EndsWith(".default-release", StringComparison.OrdinalIgnoreCase))
                                                {

                                                     selectedindex = i;
                                                
                                                     break;
                                                    
                                                }
                                                else
                                                {
                                                    selectedindex = 0;
                                                 
                                                }
                                                i++;

                                           
                                        }
                                        await main.Dispatcher.InvokeAsync(() =>
                                        {
                                            profilelist.SelectedIndex = selectedindex;
                                        });
                                        if (profilelist != null && profilelist.Items.Count > 0)
                                        {
                                            await main.Dispatcher.InvokeAsync(() =>
                                            {
                                                profile = path + profilelist.Items[selectedindex];
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
                                        Debug.WriteLine(line);
                                        if (recommended)
                                        {
                                            main.database.Add(name + "=" + path);
                                        }


                                  


                                        await main.progressBar1.Dispatcher.InvokeAsync(() =>
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
                                                        this.currentid = CreateRandomId(8);
                                                        groupBoxContent = new ListBox
                                                        {
                                                           
                                                            ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel))),
                                                            VerticalAlignment = VerticalAlignment.Stretch,
                                                            HorizontalAlignment = HorizontalAlignment.Stretch,
                                                            Margin = new Thickness(5),
                                                            Background = System.Windows.Media.Brushes.Transparent,
                                                            BorderThickness = new Thickness(0),
                                                            Foreground = main.brush,

                                                            MaxHeight = SystemParameters.WorkArea.Height,  
                                                            MaxWidth = SystemParameters.WorkArea.Width
                                                            
                                                        };



                                                        groupBoxContent.Loaded += (s, e) =>
                                                        {
                                                            var listBox = s as ListBox;
                                                            if (listBox == null) return;

                                                            var scrollViewer = FindVisualChild<ScrollViewer>(listBox);
                                                            if (scrollViewer != null)
                                                            {
                                                                scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
                                                            }
                                                        };

                                                        newExpander = new Expander
                                                        {
                                                            Name = currentid,
                                                            Header = groupboxcontent,
                                                            Margin = new Thickness(5),
                                                            Background = new SolidColorBrush(Colors.Transparent),
                                                            Foreground = main.brush,
                                                            BorderBrush = new SolidColorBrush(Colors.Transparent),
                                                            BorderThickness = new Thickness(2),
                                                            FontSize = 14,
                                                            FontWeight = FontWeights.Regular,
                                                            HorizontalAlignment = HorizontalAlignment.Left,
                                                            VerticalAlignment = VerticalAlignment.Top,
                                                            Content = scrollViewer

                                                        };

                                                        created = 1;
                                                    }



                                                    CheckBox newCheckBox = new CheckBox
                                                    {
                                                        Name = currentid,
                                                        Content = name + "=" + path + "=warning(" + "no warning" + ")",
                                                        Margin = new Thickness(10),
                                                        IsChecked = recommended,
                                                        BorderThickness = new Thickness(0),
                                                        BorderBrush = new SolidColorBrush(Colors.Transparent),


                                                        Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                        Foreground = main.brush,

                                                        FontSize = 12,
                                                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
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
                                                    main.listboxes.Add(groupBoxContent);
                                                    main.expanders.Add(newExpander);
                                                  
                                                    if (groupBoxContent.Items.Count != 100)
                                                    {
                                                        groupBoxContent.Items.Add(newCheckBox);

                                                        
                                                        newExpander.Content = groupBoxContent;
                                                 
                                                        try
                                                        {
                                                            if (main.wrapPanel1.Children.Count != 100)
                                                            {
                                                                main.wrapPanel1.Children.Add(newExpander);
                                                            }

                                                        }
                                                        catch (Exception)
                                                        {

                                                        }
                                                    }

                                                   



                                                });




                                            }
                                            else
                                            {
                                                await main.Dispatcher.InvokeAsync(() =>
                                                {
                                                    CheckBox newCheckBox = new CheckBox
                                                    {
                                                        Name = currentid,
                                                        Content = name + "=" + path,
                                                        Margin = new Thickness(5),
                                                        IsChecked = recommended,
                                                        BorderThickness = new Thickness(0),
                                                        BorderBrush = new SolidColorBrush(Colors.White),


                                                        Background = new SolidColorBrush(System.Windows.Media.Colors.White),

                                                        Foreground = main.brush,
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        FontSize = 12,
                                                        FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
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
                              
                                main.label1_Copy.Text = "Ready to scan";
                                main.buttonStartScan.IsEnabled = true;
                                main.progressBar1.Value = 0;
                                main.UpdateArc(main.progressBar1.Value);
                                cts.Cancel();
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
            string content = checkBox.Content.ToString();
            if (content.Contains("logscan"))
            {
                return;
            }
            else if (content.Contains("WinSxS"))


            {
                string name = stringtokenizer(content, "=", 0);
                string path = stringtokenizer(content, "=", 3);
                database.Add("WinSxS Folder=C:\\Windows\\WinSxS=winsxs");

            } else
            {
                string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
                settings.removeexception(file);
            }
              
            
        }
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            
            CheckBox checkBox = sender as CheckBox;
            string content = checkBox.Content.ToString();
            if (content.Contains("logscan"))
            {
                return;
            }
            else if (content.Contains("WinSxS"))

            {
                string name = stringtokenizer(content, "=", 0);
                string path = stringtokenizer(content, "=", 3);
                database.RemoveAll(item => item.EndsWith("=winsxs"));
            
            } else
            {
                string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
                settings.addexception(file);
            }

           
    
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
               
                } else if (content.Contains("winsxs")) 
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);
                    database.Add(name + "=" + path + "=" + "winsxs");
             

                } else
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);

                    database.Add(name + "=" + path);
                     
                }
          
            }
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                string content = checkBox.Content.ToString();
                
                if (content.Contains("winsxs"))
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);
                    database.RemoveAll(item => item.EndsWith("=winsxs"));

                }
                else
                {
                    if (content.Contains("logscan"))
                    {
                        string name = stringtokenizer(content, "=", 0);
                        string path = stringtokenizer(content, "=", 1);
                        database.RemoveAll(item => item.EndsWith("=logscan"));

                    }
                    else
                    {
                        string name = stringtokenizer(content, "=", 0);
                        string path = stringtokenizer(content, "=", 1);
                        database.Remove(name + "=" + path);

                    }
                }





            }
        }
        private async void ScrollViewerWrap_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
            if (sender is not ScrollViewer scrollViewer) return;

            double verticalOffset = scrollViewer.VerticalOffset;
            double viewportHeight = scrollViewer.ViewportHeight;
            double extentHeight = scrollViewer.ExtentHeight;

         
           

            const double threshold = 20.0;
            bool isAtBottom = (extentHeight - (verticalOffset + viewportHeight)) <= threshold;

            if (!isAtBottom) return;


            foreach(Expander expander in expanders)
            {
                  if(!wrapPanel1.Children.Contains(expander))
                {
                    wrapPanel1.Children.Add(expander);
                    break;
                }
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
            private readonly MainWindow main;
            private long totalsize;
            private long cleaned;
            int winsxs = 0;
            CancellationTokenSource cts = new CancellationTokenSource();
            public Clean(MainWindow main)
            {
                this.main = main;
            }
            public async Task ScandotsAsync(string text, CancellationToken cancellationToken)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        if(main.cancelclean == 2)
                            break;
                       
                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + ".";
                            main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "..";
                        });

                        await Task.Delay(1000, cancellationToken);

                        await main.Dispatcher.InvokeAsync(() => {
                            main.label1_Copy.Text = text + "...";
                        });

                        await Task.Delay(1000, cancellationToken);
                    }
                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = "Cancel Requested Please Wait!";
                        main.label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                    });
                }
                catch (TaskCanceledException)
                {

                }
            }
            public async Task CleanupWinSxSWithRealProgress(
             string logPath,
             ProgressBar progressBar)
            {
              
                var dir = System.IO.Path.GetDirectoryName(logPath)!;
                Directory.CreateDirectory(dir);
                if (System.IO.File.Exists(logPath))
                {
                    try
                    {
                        System.IO.File.Delete(logPath);
                    }
                    catch (IOException ex)
                    {
                     
                    }
                }
                 
                var psi = new ProcessStartInfo
                {
                    FileName = "dism.exe",
                    Arguments = $"/Online /Cleanup-Image /StartComponentCleanup /ResetBase /LogPath:\"{logPath}\"",
                    UseShellExecute = false, 
                    RedirectStandardOutput = true,  
                    RedirectStandardError = true,  
                    CreateNoWindow = true,        
                    Verb = "runas"                 
                };

             
                using var dismProcess = new Process();
                dismProcess.StartInfo = psi;

               
                dismProcess.OutputDataReceived += async (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                     
                        var pctRx = new Regex(@"(\d{1,3})\.?\d*\s?%", RegexOptions.Compiled);  
                        var m = pctRx.Match(e.Data);
                        if (m.Success && int.TryParse(m.Groups[1].Value, out int pct))
                        {
                            pct = Math.Max(0, Math.Min(100, pct));
                            await progressBar.Dispatcher.InvokeAsync(() =>
                            {
                                progressBar.IsIndeterminate = false;
                                progressBar.Value = pct;
                             
                            });
                        }
                    }
                };

              
                 
                try
                {
                    dismProcess.Start();
                    dismProcess.BeginOutputReadLine();  
                    dismProcess.BeginErrorReadLine(); 

                
                    await progressBar.Dispatcher.InvokeAsync(() =>
                    {
                        progressBar.IsIndeterminate = true;
                    });

                    
                    await Task.Run(() => dismProcess.WaitForExit());
 
                    await progressBar.Dispatcher.InvokeAsync(() =>
                    {
                        progressBar.IsIndeterminate = false;
                        progressBar.Value = 100;
                    });
                     
                    using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string finalLogContent = await reader.ReadToEndAsync();
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                         AddStatusTextBlock("Cleaned: WinSxS Folder"); ;
                    });

                }
                catch (Exception ex)
                {
                 
                    await progressBar.Dispatcher.InvokeAsync(() =>
                    {
                        progressBar.IsIndeterminate = false;
                        progressBar.Value = 0; 
                    });
                }
            }
            public async Task run()
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.wrapPanelDirectories.Children.Clear();
                    main.wrapPanelDirectories.Visibility = Visibility.Visible;
                    main.wrapPanel1.Visibility = Visibility.Visible;
                    main.dataGridGroups.Visibility = Visibility.Hidden;
                    main.ScrollViewerDetectedFiles.Visibility = Visibility.Visible;
                    main.Datagridscroll.Visibility = Visibility.Hidden;
                    main.buttonStartScan.Content = "Cancel";
                    main.buttonReset.Visibility = Visibility.Hidden;
                    main.progressBar1.Value = 0;
                });
                 
                var task = ScandotsAsync("Cleaning", cts.Token);
                main.database.RemoveAll(item => item.EndsWith("=logscan"));
                int totalCount = main.database.Count + main.logfiles.Count;
                int cleanedCount = 0;
                totalsize = new DriveInfo("C:\\").AvailableFreeSpace;

                await UpdateStatusColor(System.Windows.Media.Brushes.Blue);
             
                if (main.database[0].Contains("WinSxS") && winsxs == 0)
                {
                  
                 
                    await AddStatusTextBlock("Cleaning: WinSxS Folder");
                     
                    string appFolder = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Multron Win Cleaner");
                    Directory.CreateDirectory(appFolder);
                    string logFile = System.IO.Path.Combine(appFolder, "dism_cleanup.log");
                    if (System.IO.File.Exists(logFile)) System.IO.File.Delete(logFile);
                     
                    await CleanupWinSxSWithRealProgress(logFile, main.progressBar1);
                    winsxs = 1;

                }
                if (main.logfiles.Any())
                {
                    var logBlock = await AddStatusTextBlock("Cleaning: Deep Log Scan Files");

                    foreach (string file in main.logfiles)
                    {
                        if (main.cancelclean == 2) break;

                        cleanedCount++;
                        await UpdateProgress(cleanedCount, totalCount);

                        if (!System.IO.File.Exists(file)) { continue; }

                        if (main.settings.excludedfiles.Contains(file))
                        {
                            
                            continue;
                        }

                        try
                        {
                            long fileSize = new FileInfo(file).Length;
                            System.IO.File.Delete(file);
                            cleaned += fileSize;
                        }
                        catch (Exception ex)
                        {
                            
                        }
                      
                    }

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        logBlock.Text = $"Cleaned: Deep Log Scan Files - {main.formatsize(cleaned)}";
                    });
                    cleaned = 0;
                }

                for(int i = 1; i < main.database.Count; i++)
                {
                    string item = main.database[i];
                    if (main.cancelclean == 2) break;

                    cleanedCount++;
                    await UpdateProgress(cleanedCount, totalCount);

                    string name = GetToken(item, 0);
                    string path = GetToken(item, 1);

                    var statusBlock = await AddStatusTextBlock($"Cleaning: {name}");

                    try
                    {
                        if (System.IO.File.Exists(path))
                        {
                            if (!main.settings.excludedfiles.Contains(item))
                            {
                                long size = new FileInfo(path).Length;
                                System.IO.File.Delete(path);
                                cleaned += size;
                            }
                         
                        }
                        else if (Directory.Exists(path))
                        {
                            await CleanDirectory(path);
                        }

                       
                        statusBlock = await AddStatusTextBlock($"Cleaned: {name}");
                        statusBlock.Text = $"Cleaned: {name} - {main.formatsize(cleaned)}";
                        cleaned = 0;
                       
                    }
                    catch (Exception ex)
                    {
                        
                    }
               
                }

                await FinalizeCleaning();
            }

           

           

            private async Task CleanDirectory(string path)
            {
                var excluded = new HashSet<string>(main.settings.excludedfiles, StringComparer.OrdinalIgnoreCase);
                await Task.Run(() => RecursiveClean(path, excluded));
            }

            private async Task RecursiveClean(string path, HashSet<string> excluded)
            {
                string[] files = await Task.Run(() =>
                {
                    try
                    {
                        return Directory.GetFiles(path);
                    }
                    catch (Exception ex)
                    {
                   
                        return Array.Empty<string>();
                    }
                });

                string[] dirs = await Task.Run(() =>
                {
                    try
                    {
                        return Directory.GetDirectories(path);
                    }
                    catch (Exception ex)
                    {
                      
                        return Array.Empty<string>();
                    }
                });

                foreach (var file in files)
                {
                    if (main.cancelclean == 2) return;

                  

                    if (!excluded.Contains(file))
                    {
                        var result = await Task.Run(() =>
                        {
                            try
                            {
                                long size = new FileInfo(file).Length;
                                System.IO.File.Delete(file);
                                return (true, size, string.Empty);
                            }
                            catch (Exception ex)
                            {
                                return (false, 0L, ex.Message);
                            }
                        });

                        if (result.Item1)
                        {
                            cleaned += result.Item2;
                        }
                       
                    }
                }

                foreach (var dir in dirs)
                {
                    if (main.cancelclean == 2) return;

                    await RecursiveClean(dir, excluded);

                    var result = await Task.Run(() =>
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            return (true, string.Empty);
                        }
                        catch (Exception ex)
                        {
                            return (false, ex.Message);
                        }
                    });

                 
                }
            }
 
            private async Task FinalizeCleaning()
            {
                cts.Cancel();
                long freedSpace = new DriveInfo("C:\\").AvailableFreeSpace - totalsize;
                string resultMessage = main.autoclean == 1
                    ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}"
                    : $"Cleaning done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}";
             
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Text = resultMessage;
                    main.label1_Copy.Foreground = System.Windows.Media.Brushes.Green;
                    main.reset = 1;
                    main.cancelclean = 0;
                    main.cancelstatus = 0;
                    main.autoclean = 0;
                    main.onclean = 0;
                    main.buttonStartScan.Content = "Scan";
                    main.buttonStartScan.IsEnabled = false;
                    main.buttonReset.Visibility = Visibility.Visible;
                    var action = (main.settings.cmbPostCleanupAction.SelectedItem as ComboBoxItem)?.Content?.ToString();
              
                    switch (action)
                    {
                        case "Restart": SystemActions.Restart(); break;
                        case "LogOff": SystemActions.LogOff(); break;
                        case "Shutdown": SystemActions.Shutdown(); break;
                    }
                });
            }

        

            private async Task<TextBlock> AddStatusTextBlock(string text)
            {
                TextBlock tb = null;
                await main.Dispatcher.InvokeAsync(() =>
                {
                    tb = new TextBlock
                    {
                        Text = text,
                        Foreground = System.Windows.Media.Brushes.Blue,
                        FontSize = 16,
                        Margin = new Thickness(5),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Top
                    };
                    main.wrapPanelDirectories.Children.Add(tb);
                });
                return tb;
            }

            private async Task UpdateProgress(int current, int total)
            {
                double percent = (double)current / total * 100;
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.progressBar1.Value = percent;
                  
                });
            }

         
            

            private async Task UpdateStatusColor(System.Windows.Media.Brush brush)
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Foreground = brush;
                });
            }

            private string GetToken(string source, int index)
            {
                var parts = source.Split('=');
                return (index >= 0 && index < parts.Length) ? parts[index] : "";
            }
        }
   
        private async void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
            if (buttonStartScan.Content == "Clean")
            {
                cancelstatus = 0;
                scanstatus = 0;
                cancelclean = 0;
                wrapPanelDirectories.Children.Clear();
                Clean clean = new Clean(this);
                await Task.Run(() => clean.run());
                
            }
            else if (buttonStartScan.Content.Equals("Scan"))
            {

                wrapPanel1.Visibility = Visibility.Hidden;

                buttonStartScan.Content = "Cancel";
               Scan scan = new Scan(this);
                await Task.Run(() => scan.run());
                

                logfiles.Clear();
                cancelclean = 0;
                cancelstatus = 0;
                scanstatus = 0;
            } else if (buttonStartScan.Content == "Cancel")
            {
                cancelstatus = 1;
                cancelclean = 2;
                buttonReset.Visibility = Visibility.Visible;
         
            } else
            {
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


      



private void ButtonReset_Click(object sender, RoutedEventArgs e)
        {
             wrapPanelDirectories.Visibility = Visibility.Hidden;
             
            dataGridGroups.Visibility = Visibility.Hidden;
             ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
            Datagridscroll.Visibility = Visibility.Hidden;
            ScrollViewerDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Visibility = Visibility.Hidden;
            wrapPanelDirectories.Children.Clear();
            viewModel.Groups.Clear();
            Datagridscroll.Visibility = Visibility.Hidden;
            dataGridGroups.Visibility = Visibility.Hidden;
            buttonReset.Visibility = Visibility.Hidden;
            ScrollViewerDetectedFiles.Visibility = Visibility.Visible;
            wrapPanel1.Visibility = Visibility.Visible;

            buttonStartScan.Content = "Scan";
            buttonStartScan.IsEnabled = true;

             
            progressBar1.Value = 0;

            scanstatus = 0;
            reset = 0;
        }
        int backuped = 0;

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            string searchText = textBox.Text.Trim().ToLower();

            if (backuped == 0)
            {
                viewModelbac.Groups.Clear();
                foreach (var group in viewModel.Groups)
                    viewModelbac.Groups.Add(group);

                backuped = 1;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                viewModel.Groups.Clear();
                foreach (var group in viewModelbac.Groups)
                    viewModel.Groups.Add(group);

                backuped = 0;
                return;
            }

             
            var filteredList = await Task.Run(() =>
                viewModelbac.Groups
                    .Where(g => !string.IsNullOrEmpty(g.Path) && g.Path.ToLower().Contains(searchText))
                    .ToList());
             
            viewModel.Groups.Clear();
            foreach (var group in filteredList)
                viewModel.Groups.Add(group);
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
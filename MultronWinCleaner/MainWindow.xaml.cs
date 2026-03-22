using Hardcodet.Wpf.TaskbarNotification;
using MFK;
using Microsoft.VisualBasic.Logging;
using MultronWinCleaner;
using MultronWinCleaner.Processes;
using Ookii.Dialogs.Wpf;
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
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Automation;
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
using WindowsInput.Events.Sources;
using static Multron_Win_Cleaner.MainWindow;
using static MultronWinCleaner.Processes.Clean;
using static MultronWinCleaner.Processes.Scan;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Header;


namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {


        public Settings settings;
        public MultronWinCleaner.Processes.Scan.MainViewModel viewModel = new MultronWinCleaner.Processes.Scan.MainViewModel();
        public MultronWinCleaner.Processes.Scan.MainViewModel viewModelbac = new MultronWinCleaner.Processes.Scan.MainViewModel();
        public List<string> logfiles = new List<string>();
        public SolidColorBrush brush;
        public List<CheckBox> checkboxes2 = new List<CheckBox>();
        public List<StackPanel> stackpanels = new List<StackPanel>();
        public List<Expander> expanders = new List<Expander>();
        public List<ListBox> listboxes = new List<ListBox>();
        public List<ComboBox> comboboxlist = new List<ComboBox>();
        public List<string> paths = new List<string>();
        public CancellationTokenSource dismcancel = new CancellationTokenSource();
        public Utilities utilities;
        public byte autoclean = 0;
        public byte onclean = 0;
        public byte killer = 0;
        public string extensions = ".log.etl.dmp.trace.tmp.temp.bak.swp";
        public MainWindow()
        {
            InitializeComponent();
            string settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");

            if (!System.IO.File.Exists(settingsPath))
            {
                string defaultSettings = "minutes:0" + Environment.NewLine +
                                         "autoclean:0" + Environment.NewLine +
                                         "trayicon:0" + Environment.NewLine +
                                         "themes:0";
                System.IO.File.WriteAllText(settingsPath, defaultSettings);
            }

            string fileContent = System.IO.File.ReadAllText(settingsPath);

            string themePath = fileContent.Contains("themes:1") ? "Themes/Dark.xaml" : "Themes/Light.xaml";
            ToggleThemeSwitch.IsChecked = fileContent.Contains("themes:1");

            themeselector.selector(new Uri(themePath, UriKind.Relative));

            var resourceDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };
            brush = (SolidColorBrush)resourceDictionary["Text"];



        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ReloadDb.IsEnabled = false;
            MultronWinCleaner.Processes.Updater updater = new MultronWinCleaner.Processes.Updater(this);
            await Task.Run(() => updater.run());
            string updaterfile = Environment.CurrentDirectory + "\\Update\\mwc\\Updater.exe";
            string updatesfolder = Environment.CurrentDirectory + "\\Update\\mwc";

            if (System.IO.File.Exists(updaterfile))
            {
                System.IO.File.Copy(updaterfile, Environment.CurrentDirectory + "\\Updater.exe", overwrite: true);
            }
            if (Directory.Exists(updatesfolder))
            {
                foreach (string file in Directory.GetFiles(updatesfolder))
                {
                    try
                    {
                        System.IO.File.Delete(file);
                    }
                    catch (Exception ex)
                    {

                    }

                }
                if (Directory.Exists(updatesfolder))
                {
                    try
                    {
                        Directory.Delete(updatesfolder);
                    }
                    catch (Exception ex)
                    {

                    }

                }
            }
            utilities = new Utilities(this);


            utilities.Show();
            utilities.Hide();


            progressBar1.ValueChanged += ProgressBar1_ValueChanged;
            settings = new Settings(utilities.memcleaner, utilities, this, utilities.startupmanager);

            settings.Show();
            settings.Hide();
            dataGridGroups.Visibility = Visibility.Hidden;
            Datagridscroll.Visibility = Visibility.Hidden;

            loadothers();
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {

                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                var load = new MultronWinCleaner.Processes.Load(this);
                await Task.Run(() => load.RunAsync());


                this.previousWidth = this.Width;
                this.previousHeight = this.Height;
                this.previousLeft = this.Left;
                this.previousTop = this.Top;

            }
            else
            {
                MessageBoxResult result = MessageBox.Show(
       "The database.txt file could not be found. This may be due to an internet connectivity issue, as the program attempts to download the latest database.txt file from GitHub but was unable to do so.\n\nWould you like to be redirected to the GitHub page to manually download the latest database.txt file?",
       "Multron Windows Cleaner",
       MessageBoxButton.YesNo,
       MessageBoxImage.Question
   );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/winball501/MultronWcleaner-Database",
                        UseShellExecute = true
                    });
                }

            }

            TrayIconWindow trayiconwindow = new TrayIconWindow(this);

            Thread traythread = new Thread(trayiconwindow.run);
            traythread.Start();

            TrayIcon.TrayMouseDoubleClick += TrayIcon_MouseDoubleClick;
            ReloadDb.IsEnabled = true;

        }
        public void loadothers()
        {
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
                Content = "Dism.exe" + "=" + "WinSxS Clean" + "=warning=(No Warning)" + "=" + "winsxs",
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
            checkboxes2.Add(newCheckBox3);
          
            StackPanel groupBoxContent5 = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left

            };
            stackpanels.Add(groupBoxContent5);
            Expander newExpander4 = new Expander
            {
                Header = "Dism.exe",
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
                Content = "Deep Log Files Scan" + "=" + "C:\\" + "=warning=Its can take long time." + "=" + "logscan",
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
       
        }
        private void ProgressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateArc(progressBar1.Value);
        }
 
         
        private System.Windows.Shapes.Path _arcPath;
       public void UpdateArc(double percent)
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
        public static int scanstatus = 0;
        public CancellationTokenSource cancelstatus = new CancellationTokenSource();

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
         
        public string formatsize(long size)
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
                if (sender is ScrollViewer scrollViewer && scrollViewer.DataContext is MultronWinCleaner.Processes.Scan.GroupViewModel vm)
                {
                    if (isLoading) return;  

                    isLoading = true;
                    await vm.LoadMoreFilesAsync();
                    isLoading = false;
                }
            }
        }
         
        public T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
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
                return;
            } else
            {
                string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
                settings.addexception(file);
            }

           
    
        }
      
       
        public void CheckBox_Checked(object sender, RoutedEventArgs e)
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
        public void CheckBox_Unchecked(object sender, RoutedEventArgs e)
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
     
       
        public int cancelclean = 0;
        public int reset = 0;
      
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
        public class Kill
        {
            private MainWindow main;
            private long totalsize = 0;
            private CancellationTokenSource cts = new CancellationTokenSource();

            public Kill(MainWindow main)
            {
                this.main = main;
            }

            public async Task ScandotsAsync(string text, CancellationTokenSource cancellationToken)
            {
                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = text + ".";
                            main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                        });
                        await Task.Delay(1000, cancellationToken.Token);

                        await main.Dispatcher.InvokeAsync(() => { main.label1_Copy.Text = text + ".."; });
                        await Task.Delay(1000, cancellationToken.Token);

                        await main.Dispatcher.InvokeAsync(() => { main.label1_Copy.Text = text + "..."; });
                        await Task.Delay(1000, cancellationToken.Token);
                    }
                }
                catch (TaskCanceledException) { }
            }
            void HideChildrenOfAllDockPanels(Panel parent)
            {
                foreach (UIElement child in parent.Children)
                { 
                    if (child is DockPanel dockPanel)
                    {
                        foreach (UIElement dpChild in dockPanel.Children)
                        {
                            dpChild.Visibility = Visibility.Hidden;
                        }
                    }
                     
                    if (child is Panel nestedPanel)
                    {
                        HideChildrenOfAllDockPanels(nestedPanel);
                    }
                }
            }
            public async Task run()
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    HideChildrenOfAllDockPanels(main.Panel);
                    main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                    main.wrapPanelDirectories.Visibility = Visibility.Visible;
                    main.progressBar1.Minimum = 0;
                    main.progressBar1.Maximum = main.paths.Count;
                    main.progressBar1.Value = 0;
                     
                    main.wrapPanelDirectories.Children.Add(new TextBlock
                    {
                        Text = "Started...",
                        Foreground = System.Windows.Media.Brushes.Black,
                        Margin = new Thickness(5)
                    });
                });

                var dotsTask = ScandotsAsync("Killing", cts);
                int progress = 0;
                int killed = 0;
                for (int i = 0; i < main.paths.Count; i++)
                {
                    if (main.cancelstatus.IsCancellationRequested) break;

                    string path = main.stringtokenizer(main.paths[i], "=", 0);

                    if (System.IO.File.Exists(path))
                    {
                        var procs = whousef.WhoIsLocking(path);
                        if (procs != null && procs.Count > 0)
                        {
                            foreach (Process proc in procs)
                            {
                                try
                                {
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.wrapPanelDirectories.Children.Add(new TextBlock
                                        {
                                            Text = $"Killing: {proc.ProcessName} (PID {proc.Id}) for {path}",
                                            Foreground = System.Windows.Media.Brushes.Red,
                                            FontSize = 16,
                                            Margin = new Thickness(5)
                                        });
                                    });

                                    proc.Kill();
                                    proc.WaitForExit();
                                }
                                catch (Exception ex)
                                {
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.wrapPanelDirectories.Children.Add(new TextBlock
                                        {
                                            Text = $"Cannot Kill: {proc.ProcessName} (PID {proc.Id}) - {ex.Message}",
                                            Foreground = System.Windows.Media.Brushes.Goldenrod,
                                            FontSize = 16,
                                            Margin = new Thickness(5)
                                        });
                                    });
                                }
                            }

                       
                            try
                            {
                                if (System.IO.File.Exists(path))
                                {
                                    FileInfo info = new FileInfo(path);
                                    long fileSize = info.Length;
                                    System.IO.File.Delete(path);
                                    killed++;
                                    totalsize += fileSize;

                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.wrapPanelDirectories.Children.Add(new TextBlock
                                        {
                                            Text = $"Deleted: {path} ({main.formatsize(fileSize)})",
                                            Foreground = System.Windows.Media.Brushes.Green,
                                            FontSize = 16,
                                            Margin = new Thickness(5)
                                        });
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.wrapPanelDirectories.Children.Add(new TextBlock
                                    {
                                        Text = $"Cannot Delete: {path} - {ex.Message}",
                                        Foreground = System.Windows.Media.Brushes.Goldenrod,
                                        FontSize = 16,
                                        Margin = new Thickness(5)
                                    });
                                });
                            }
                        }
                        else
                        {
                      
                            try
                            {
                                FileInfo info = new FileInfo(path);
                                long fileSize = info.Length;
                                System.IO.File.Delete(path);
                                killed++;
                                totalsize += fileSize;

                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.wrapPanelDirectories.Children.Add(new TextBlock
                                    {
                                        Text = $"Deleted (no longer locked): {path} ({main.formatsize(fileSize)})",
                                        Foreground = System.Windows.Media.Brushes.Green,
                                        FontSize = 16,
                                        Margin = new Thickness(5)
                                    });
                                });
                            }
                            catch (Exception ex)
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.wrapPanelDirectories.Children.Add(new TextBlock
                                    {
                                        Text = $"Cannot Delete: {path} - {ex.Message}",
                                        Foreground = System.Windows.Media.Brushes.Goldenrod,
                                        FontSize = 16,
                                        Margin = new Thickness(5)
                                    });
                                });
                            }
                        }
                    }
                    else
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.wrapPanelDirectories.Children.Add(new TextBlock
                            {
                                Text = $"File no longer exists: {path}",
                                Foreground = System.Windows.Media.Brushes.Blue,
                                FontSize = 16,
                                Margin = new Thickness(5)
                            });
                        });
                    }

                    progress++;
                    main.Dispatcher.Invoke(() =>
                    {
                        double percent = (progress * 100.0) / main.paths.Count;
                        main.progressBar1.Value = percent;
                        main.UpdateArc(percent);
                    });
                }

                await cts.CancelAsync(); 
                await dotsTask; 

                await main.Dispatcher.InvokeAsync(() =>
                {
                    string message;
                    System.Windows.Media.Brush labelColor;

                    if (main.cancelstatus.IsCancellationRequested)
                    {
                        message = $"Killing Canceled. {killed} Process Killed. Freed Space: {main.formatsize(totalsize)}";
                        labelColor = System.Windows.Media.Brushes.Goldenrod;
                    }
                    else
                    {
                        message = $"Killing Done! {killed} Process Killed. Freed Space: {main.formatsize(totalsize)}";
                        labelColor = System.Windows.Media.Brushes.Red;
                    }

                    main.label1_Copy.Text = message;
                    main.label1_Copy.Foreground = labelColor;
                    main.buttonReset.Visibility = Visibility.Visible;
                    main.paths.Clear();
                });
            }
        }
        public ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel> LockedFileGroups { get; } = new();
        public CollectionViewSource groupedProcesses = new CollectionViewSource();
        public List<MultronWinCleaner.Processes.Clean.LockedProcessViewModel> _allLockedFileGroups = new List<MultronWinCleaner.Processes.Clean.LockedProcessViewModel>();
        public ObservableCollection<MultronWinCleaner.Processes.Clean.LockedProcessViewModel> LockedProcesses = new ObservableCollection<MultronWinCleaner.Processes.Clean.LockedProcessViewModel>();
        public int _itemsLoaded = 0;
        public int _pageSize = 50;

        
        public async Task startscan()
        {
            if (buttonStartScan.Content == "Clean")
            {
                cancelstatus = new CancellationTokenSource();
                dismcancel = new CancellationTokenSource();
                scanstatus = 0;
                cancelclean = 0;

                wrapPanelDirectories.Children.Clear();
                MultronWinCleaner.Processes.Clean clean = new MultronWinCleaner.Processes.Clean(this);
                await Task.Run(() => clean.run());

            }
            else if (buttonStartScan.Content.Equals("Scan"))
            {
                cancelstatus = new CancellationTokenSource();
                dismcancel = new CancellationTokenSource();
                wrapPanel1.Visibility = Visibility.Hidden;

                buttonStartScan.Content = "Cancel";
         
                cancelclean = 0;
                MultronWinCleaner.Processes.Scan scan = new MultronWinCleaner.Processes.Scan(this);

                await Task.Run(() => scan.run());


               
                scanstatus = 0;
            }
            else if (buttonStartScan.Content == "Cancel")
            {
                cancelstatus.Cancel();
                cancelclean = 2;
                buttonReset.Visibility = Visibility.Visible;

                dismcancel.Cancel();


            }
            else if (buttonStartScan.Content == "Kill")
            {

                if (paths.Count != 0)
                {
                    killer = 1;
                    wrapPanelDirectories.Children.Clear();
                    wrapPanelDirectories.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Cancel";
                    Kill kill = new Kill(this);
                    await Task.Run(() => kill.run());
                }
                else
                {
                    label1_Copy.Text = "No Process Selected.";
                    label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                }


            }
            {
                if (scanstatus == 1)
                {

                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Scan";
                    cancelstatus.Cancel();
                    dismcancel.Cancel();

                }
                else if (scanstatus == 2)
                {
                    ScrollViewerDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Visibility = Visibility.Hidden;
                    wrapPanelDirectories.Children.Clear();
                    buttonStartScan.IsEnabled = true;

                    wrapPanel1.Visibility = Visibility.Visible;
                    buttonStartScan.Content = "Scan";
                    cancelstatus.Cancel();
                    dismcancel.Cancel();
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
        private async void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
                await startscan();
        }
        public void OpenDirectory_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);
                try
                {
                    if (System.IO.File.Exists(wrapdir))
                    {
                        string dir = System.IO.Path.GetDirectoryName(wrapdir);
                        if (dir != null && Directory.Exists(dir))
                        {
                            Process.Start("explorer.exe", dir);
                        }
                 
                        else
                        {
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                   
                    
                    else
                    {
                        if (wrapdir != null && Directory.Exists(wrapdir))
                            Process.Start("explorer.exe", wrapdir);
                        else
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location",  MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }


        }
        public void OpenFileLocation_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);
                try
                {
                    if (System.IO.File.Exists(wrapdir))
                        Process.Start("explorer.exe", $"/select,\"{wrapdir}\"");
                    else
                    {
                        string? dir = System.IO.Path.GetDirectoryName(wrapdir);
                        if (dir != null && Directory.Exists(dir))
                            Process.Start("explorer.exe", dir);
                        else
                            MessageBox.Show("Path not found:\n" + wrapdir, "Open File Location",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

         
        }

        public void CopyPath_MainMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            var target = contextMenu?.PlacementTarget as CheckBox;

            if (target != null && checkboxes2.Contains(target))
            {
                int index = checkboxes2.IndexOf(target);
                string wrap = checkboxes2[index].Content.ToString();
                string wrapdir = stringtokenizer(wrap, "=", 1);
              
                try
                {
                    if (wrapdir != null)
                    {
                        Clipboard.SetText(wrapdir);
                    }
                    else
                    {
                        MessageBox.Show("Directory returned null.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem) return;
            if (menuItem.DataContext is not Scan.FileItem fileItem) return;

            string path = fileItem.Path;
            try
            {
                if (System.IO.File.Exists(path))
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                else
                {
                    string? dir = System.IO.Path.GetDirectoryName(path);
                    if (dir != null && Directory.Exists(dir))
                        Process.Start("explorer.exe", dir);
                    else
                        MessageBox.Show("Path not found:\n" + path, "Open File Location",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error:\n" + ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyPath_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem &&
                menuItem.DataContext is Scan.FileItem fileItem)
            {
                Clipboard.SetText(fileItem.Path);
            }
        }


        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
           
            settings.Show();
            settings.WindowState = WindowState.Normal;
        }
   
        int doit = 0;
        private void UtilitiesButton_Click(object sender, RoutedEventArgs e)
        {
           
                utilities.Show();
                settings.WindowState = WindowState.Normal;


        }
        
        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }





        public class LoadLockedFiles
        {
            MainWindow main;
            public LoadLockedFiles(MainWindow main)
            {
                this.main = main;
            }

            public async Task CheckWhoUsesMultipleAsync(IEnumerable<string> paths, IProgress<(int current, int total)>? progress = null)
            {
                var results = new ConcurrentBag<LockedFileGroupViewModel>();
                var pathList = paths.ToList();
                int total = pathList.Count;
                int processedCount = 0;

                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Foreground = System.Windows.Media.Brushes.Blue;
                });

                var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < pathList.Count; i++)
                {
                    string path = main.stringtokenizer(pathList[i], "=", 0);
                    string savedId = main.stringtokenizer(pathList[i], "=", 1);
                    string savedName = main.stringtokenizer(pathList[i], "=", 2);
                    string groupName = main.stringtokenizer(pathList[i], "=", 3);

                    if (processed.Contains(path))
                    {
                        Interlocked.Increment(ref processedCount);
                        progress?.Report((processedCount, total));
                        continue;
                    }
                    processed.Add(path);

                    if (!System.IO.File.Exists(path))
                    {
                        Interlocked.Increment(ref processedCount);
                        progress?.Report((processedCount, total));
                        continue;
                    }

                    var processes = whousef.WhoIsLocking(path);

                    if (processes == null || processes.Count == 0)
                    {
                        results.Add(new LockedFileGroupViewModel
                        {
                            FilePath = path,
                            GroupName = groupName,
                            Processes = new ObservableCollection<LockedProcessViewModel>(
                                new[] { new LockedProcessViewModel
                    {
                        DisplayName = savedName + " (PID " + savedId + ")",
                        Id = savedId,
                        IsChecked = false
                    }})
                        });

                        Interlocked.Increment(ref processedCount);
                        progress?.Report((processedCount, total));
                        continue;
                    }

                    var processViewModels = new ObservableCollection<LockedProcessViewModel>(
                        processes.Select(p => new LockedProcessViewModel
                        {
                            DisplayName = $"{p.ProcessName} (PID {p.Id})",
                            Id = p.Id.ToString(),
                            IsChecked = false
                        }));

                    results.Add(new LockedFileGroupViewModel
                    {
                        FilePath = path,
                        GroupName = groupName,
                        Processes = processViewModels
                    });

                    Interlocked.Increment(ref processedCount);
                    progress?.Report((processedCount, total));
                }

                if (System.Windows.Application.Current != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var item in results)
                            main.LockedFileGroups.Add(item);
                    }, DispatcherPriority.Background);
                }
                else
                {
                    foreach (var item in results)
                        main.LockedFileGroups.Add(item);
                }
            }

            public async Task run()
            {
                var progress = new Progress<(int current, int total)>(async p =>
                {
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.label1_Copy.Text = $"Loading locked files: {p.current}/{p.total}";
                    });
                });
                System.Windows.MessageBox.Show($"paths count: {main.paths.Count}\n" +
        string.Join("\n", main.paths.Take(5)));
                await CheckWhoUsesMultipleAsync(main.paths, progress);

                await main.Dispatcher.InvokeAsync(() =>
                {
                    var allItems = new List<LockedProcessViewModel>();
                    var added = new HashSet<string>();

                    foreach (var g in main.LockedFileGroups)
                    {
                        string fullPath = g.FilePath;

                        FileInfo info = null;
                        string filesize = "0 Byte";
                        try
                        {
                            if (System.IO.File.Exists(fullPath))
                            {
                                info = new FileInfo(fullPath);
                                filesize = main.formatsize(info.Length);
                            }
                        }
                        catch { }

                        if (info == null)
                            continue;

                        foreach (var proc in g.Processes)
                        {
                            string uniqueKey = $"{proc.Id}|{fullPath}";

                            if (!added.Add(uniqueKey))
                                continue;

                            allItems.Add(new LockedProcessViewModel
                            {
                                FilePath = fullPath + " " + filesize,
                                FilePathWithoutSize = fullPath,
                                GroupName = g.GroupName,
                                DisplayName = proc.DisplayName,
                                Id = proc.Id,
                                IsChecked = true,
                                OnCheckedChanged = (model, state) =>
                                {
                                    string key = model.FilePathWithoutSize + "=" + model.Id + "=" + model.DisplayName;
                                    if (state)
                                    {
                                        if (!main.paths.Contains(key))
                                            main.paths.Add(key);
                                    }
                                    else
                                    {
                                        main.paths.RemoveAll(p =>
                                            main.stringtokenizer(p, "=", 0).Equals(model.FilePathWithoutSize, StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                            });
                        }
                    }

                    main._allLockedFileGroups = allItems;
                    main.LockedProcesses.Clear();

                    foreach (var item in allItems)
                        main.LockedProcesses.Add(item);

                    main._itemsLoaded = main.LockedProcesses.Count;

                    var cvs = new CollectionViewSource { Source = main.LockedProcesses };
                    cvs.GroupDescriptions.Add(new PropertyGroupDescription(nameof(LockedProcessViewModel.GroupName)));

                    var view = cvs.View;
                    string search = (main.SearchBox.Text ?? "").Trim().ToLower();

                    if (string.IsNullOrWhiteSpace(search))
                    {
                        view.Filter = null;
                    }
                    else
                    {
                        view.Filter = item =>
                        {
                            if (item is LockedProcessViewModel p)
                                return p.FilePathWithoutSize?.ToLower().Contains(search) == true ||
                                       p.GroupName?.ToLower().Contains(search) == true ||
                                       p.DisplayName?.ToLower().Contains(search) == true;
                            return false;
                        };
                    }

                    view.Refresh();
                    main.groupedProcesses = cvs;
                    main.listBoxProcesses.ItemsSource = view;
                     
                    var stillLockedPaths = main.LockedFileGroups
                        .Select(g => g.FilePath)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    main.paths.RemoveAll(p =>
                        !stillLockedPaths.Contains(main.stringtokenizer(p, "=", 0)));

                    main.label1_Copy.Text = $"Locked Files: {main.LockedFileGroups.Count} files, {allItems.Count} processes";
                });
            }
        }

        private async void ButtonLocked_Click(object sender, RoutedEventArgs e)
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
            ScrollViewerDetectedFiles.Visibility = Visibility.Hidden;
            wrapPanel1.Visibility = Visibility.Hidden;
            ButtonLockedFiles.Visibility = Visibility.Hidden;
            buttonStartScan.IsEnabled = true;
            buttonStartScan.Content = "Kill";
         
            LoadLockedFiles lockedfiles = new LoadLockedFiles(this);
            await Task.Run(() => lockedfiles.run());
           
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
            ButtonLockedFiles.Visibility = Visibility.Hidden;
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

        private ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel> _backupLockedFileGroups = new ObservableCollection<MultronWinCleaner.Processes.Clean.LockedFileGroupViewModel>();
        private async void ProcessBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = (sender as TextBox)?.Text?.Trim().ToLower() ?? "";

            await UpdateProcessViewModel(searchText);
        }
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = (sender as TextBox)?.Text?.Trim().ToLower() ?? "";
 
            await UpdateListViewAsync(searchText);
        }
        private async Task UpdateProcessViewModel(string searchText)
        {

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    groupedProcesses.View.Filter = null;
                    
                }
                else
                {
                  
                    groupedProcesses.View.Filter = item =>
                    {
                        if (item is MultronWinCleaner.Processes.Clean.LockedProcessViewModel proc)
                        {
                            return proc.DisplayName?.ToLower().Contains(searchText) == true;
                        }
                        return false;
                    };
                }
                listBoxProcesses.ItemsSource = groupedProcesses.View;
            });
        }
        private async Task UpdateListViewAsync(string searchText)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    groupedProcesses.View.Filter = null;
                }
                else
                {
                    string search = searchText.ToLower();
                    groupedProcesses.View.Filter = item =>
                    {
                        if (item is MultronWinCleaner.Processes.Clean.LockedProcessViewModel proc)
                        {
                            return proc.FilePathWithoutSize?.ToLower().Contains(search) == true ||
                                   proc.GroupName?.ToLower().Contains(search) == true ||
                                   proc.DisplayName?.ToLower().Contains(search) == true;
                        }
                        return false;
                    };
                }

                groupedProcesses.View.Refresh();
                listBoxProcesses.ItemsSource = groupedProcesses.View;
            });
        }
        private bool _isLoadingMore = false;

        private async void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 10)
            {
                if (!_isLoadingMore)
                {
                    _isLoadingMore = true;
                    await LoadMoreItemsAsync();
                    _isLoadingMore = false;
                }
            }
        }
  
        private void SelectAll_wpanel_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
             foreach(CheckBox box in checkboxes2)
            {
                
                box.IsChecked = true;
                i++;
            }
      
        }


        private void UnselectAll_wpanel_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox box in checkboxes2)
            {
              
                box.IsChecked = false;
            }
         
        }
        private void SelectAll_AllGroups_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in dataGridGroups.Items)
            {
                if (item is GroupViewModel group)
                { 
                    foreach (var file in group.allFiles)
                    {
                        file.IsChecked = true;
                    }
                     
                    var sp = stackpanels.FirstOrDefault(s => s.DataContext == group);
                    if (sp != null)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is CheckBox cb)
                            {
                                cb.IsChecked = true;
                            }
                        }
                    }
                }
            }
        }


        private void UnselectAll_AllGroups_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in dataGridGroups.Items)
            {
                if (item is GroupViewModel group)
                { 
                    foreach (var file in group.allFiles)
                    {
                        file.IsChecked = false;
                    }
 
                    var sp = stackpanels.FirstOrDefault(s => s.DataContext == group);
                    if (sp != null)
                    {
                        foreach (var child in sp.Children)
                        {
                            if (child is CheckBox cb)
                            {
                                cb.IsChecked = false;
                            }
                        }
                    }
                }
            }
        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is GroupViewModel groupVm)
                {
                    foreach (var file in groupVm.allFiles.Concat(groupVm.Files.Except(groupVm.Files)))
                    {
                        file.IsChecked = true;
                    }
                }
                else if (button.Tag is PathGroup group)
                {
                    foreach (var file in group.Files)
                    {
                        file.IsChecked = true;
                    }
                }
            }
        }

        private async void ReloadDatabase_Click(object sender, RoutedEventArgs e)
        {
            ReloadDb.IsEnabled = false;
            wrapPanel1.Children.Clear();
            wrapPanelDirectories.Children.Clear();
            ScrollViewerDirectories.Content = null;
            checkboxes2.Clear();
            listboxes.Clear();
            expanders.Clear();
            stackpanels.Clear();
            database.Clear();
            loadothers();
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {

                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                
                var load = new MultronWinCleaner.Processes.Load(this);
                await Task.Run(() => load.RunAsync());
                ReloadDb.IsEnabled = true;



            }
            else
            {
                MessageBox.Show("Any database file not found, Program closing...", "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
        }
        public async Task savetodatabase()
        {
            Dictionary<string, bool> checkBoxStates = null;

            
            await Dispatcher.InvokeAsync(() =>
            {
                checkBoxStates = new Dictionary<string, bool>();
                foreach (CheckBox box in checkboxes2)
                {
                 
                    string path = stringtokenizer(box.Content.ToString(), "=", 1);
                    checkBoxStates[path] = box.IsChecked == true;
                }
            });

         
            await Task.Run(async () =>
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory, "database.txt");
                List<string> lines = System.IO.File.ReadAllLines(filePath).ToList();

                string profile = "";
                int profilemode = 0;

                for (int i = 0; i < lines.Count; i++)
                {
                    string lin = lines[i];
                    if (lin.Contains("}")) profilemode = 0;
                    if (lin.StartsWith("#profile#"))
                    {
                        profile = stringtokenizer(lin, "=", 1);
                        profilemode = 1;
                    }
                    foreach (var kvp in checkBoxStates)
                    {
                        string path = kvp.Key;
                        bool isChecked = kvp.Value;
                        if (profilemode == 1)
                        {
                            if (lin.Contains("#profileget#")) {
                                string pat = profile;

                                pat = pat.Replace("\\\\", "\\");
                                pat = pat.Replace("{##}", Environment.UserName);
                                string fullpath = "";
                                 

                              await this.Dispatcher.InvokeAsync(() => {
                                 foreach (ComboBox box in comboboxlist)
                                 {

                                
                                    fullpath = pat  + box.SelectedItem.ToString()  + stringtokenizer(lin, "=", 2);
                                    if(System.IO.File.Exists(fullpath) || System.IO.Directory.Exists(fullpath))
                                    {
                                         break;
                                    }
                                }
                              });

                            
                                if (path.Contains(fullpath))
                                {
                                   
                                    lines[i] = isChecked
                                        ? lines[i].Replace("=false", "=true")
                                        : lines[i].Replace("=true", "=false");
                                }
                            }
                           
                        } else
                        {
                            if(lin.Contains("{##}"))
                            {
                                lin = lin.Replace("{##}", Environment.UserName);
                            }
                            if (lin.Contains(path))
                            {
                                lines[i] = isChecked
                                    ? lines[i].Replace("=false", "=true")
                                    : lines[i].Replace("=true", "=false");
                            }
                        }
                    
                    }
                }

                System.IO.File.WriteAllLines(filePath, lines);
            });
             
            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show("Settings saved to database sucessfully.");
                SaveSettings.IsEnabled = true;
                SaveSettings.Content = "Save Settings to Database";
            });
        }
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings.IsEnabled = false;
            SaveSettings.Content = "Saving...";
            await savetodatabase(); 
        }

        private void UnselectAll_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Tag is GroupViewModel groupVm)
                {
                    foreach (var file in groupVm.allFiles.Concat(groupVm.Files.Except(groupVm.Files)))
                    {
                        file.IsChecked = false;
                    }
                }
                else if (button.Tag is PathGroup group)
                {
                    foreach (var file in group.Files)
                    {
                        file.IsChecked = false;
                    }
                }
            }
        }
        private async Task LoadMoreItemsAsync()
        {
      
            var toAdd = _allLockedFileGroups.Skip(_itemsLoaded).Take(_pageSize).ToList();

            if (toAdd.Count == 0)
                return;

            await this.Dispatcher.InvokeAsync(() =>
            {
                foreach (var proc in toAdd)
                    LockedProcesses.Add(proc);
            });

            _itemsLoaded += toAdd.Count;
        }
      
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}

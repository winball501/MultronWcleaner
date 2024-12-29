using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Text;
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

namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {

    
        List<string> logfiles = new List<string>();
       
        public MainWindow()
        {
            InitializeComponent();
            if (System.IO.File.Exists(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\database.txt") == true)
            {
                buttonCancel.IsEnabled = false;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                Load load = new Load(this);
                Thread t = new Thread(new ThreadStart(load.run));
                t.Start();

                this.previousWidth = this.Width;
                this.previousHeight = this.Height;
                this.previousLeft = this.Left;
                this.previousTop = this.Top;
                CheckBox newCheckBox = new CheckBox
                {
                    Content = "Deep Log Files Scan " + "=" + "C:\\" + "=warning=Its can take long time." + "=" + "logscan",
                    Margin = new Thickness(10),
                    IsChecked = false,
                    BorderThickness = new Thickness(0),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 173, 216, 230)),
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 45, 45, 45)),
                    FontSize = 12,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontWeight = FontWeights.Regular,
                    FontStyle = FontStyles.Normal,
                };
               StackPanel  groupBoxContent = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left

                };

                 Expander newExpander = new Expander
                {
                    Header = "Deep Log Files Scan",
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Colors.Transparent),
                    BorderBrush = new SolidColorBrush(Colors.Transparent),
                    BorderThickness = new Thickness(2),
                    FontSize = 14,
                    FontWeight = FontWeights.Regular,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top
                };
                groupBoxContent.Children.Add(newCheckBox);
                newExpander.Content = groupBoxContent;
                try
                {
                    wrapPanel1.Children.Add(newExpander);
                }
                catch (Exception)
                {

                }
                newCheckBox.Checked +=  CheckBox_Checked;
                newCheckBox.Unchecked +=  CheckBox_Unchecked;
            }
            else
            {
                MessageBox.Show("Any database file not found, Program closing...", "Multron Windows Cleaner", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

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
        List<string> database = new List<string>();
        public int scanstatus = 0;
        public int cancelstatus = 0;
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
                    TextBlock directorytextblock = null;

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.wrapPanelDirectories.Visibility = Visibility.Visible;
                        main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                        main.label1_Copy.Foreground = Brushes.Black;
                    });
                 
                        foreach (var directory in main.database)
                        {

                            if (main.cancelstatus == 1)
                            {
                                break;
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
                                if (File.Exists(directory))
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

                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.wrapPanelDirectories.Children.Add(directorytextblock);
                                    });
                                    long totalsize = new FileInfo(directory).Length;
                                    string sizeformatted = formatsize(totalsize);
                                    this.totalsize += totalsize;

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


                                    await ScanDirectoryAsync(path, name, directorytextblock);
                                }
                            }
                            nowscanning++;
                            double percentage = (double)nowscanning / size * 100;
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                               main.progressBar1.Value = percentage;
                            });



                        }
                        if (main.cancelstatus == 1)
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "Scanning: " + "Canceled";
                                main.buttonCancel.IsEnabled = false;
                                main.ScrollViewerDirectories.Visibility = Visibility.Hidden;
                                main.wrapPanelDirectories.Visibility = Visibility.Hidden;
                                main.wrapPanelDirectories.Children.Clear();
                                main.buttonStartScan.IsEnabled = true;
                                main.ScrollViewer.Visibility = Visibility.Visible;
                                main.wrapPanel1.Visibility = Visibility.Visible;
                                main.cancelstatus = 0;
                                main.progressBar1.Value = 0;
                            });

                        }
                        else
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "Scanning: " + "Completed. + " + formatsize(totalsize) + "  Useless file found!";

                                main.label1_Copy.Foreground = Brushes.Goldenrod;
                                main.buttonStartScan.IsEnabled = true;
                                main.buttonStartScan.Content = "Clean";
                            });

                        }

                        main.scanstatus = 1;
                     
                } catch (Exception ex)
                {

                }
               
            }

            public async Task ScanCDirectoryAsync(string directory, string name, TextBlock directorytextblock)
            {
            
              
                long totalsize = await GetCDirectorySizeAsync(directory);
                string sizeformatted = formatsize(totalsize);
                this.totalsize += totalsize;

                await main.Dispatcher.InvokeAsync(() =>
                {
                    directorytextblock.Text = $"Completed: {name} - {sizeformatted} found";
                });
            }
            public async Task<long> GetCDirectorySizeAsync(string path)
            {
                try
                {
                    long size = 0;

                    if (Directory.Exists(path))
                    {

                        string[] files = Directory.GetFiles(path);
                        string[] dirs = Directory.GetDirectories(path);
                        foreach(string dir in dirs)
                        {
                            if (main.cancelstatus == 1)
                            {
                                break;
                            }
                            try
                            {
                                size += await GetCDirectorySizeAsync(dir); 
                            }
                            catch (UnauthorizedAccessException)
                            {
                          
                               
                            }

                        }
                        for (int i = 0; i < files.Length; i++)
                        {
                            if (main.cancelstatus == 1)
                            {
                                break;
                            }
                            var file = files[i];


                            
                            string[] logExtensions = { ".log", ".etl", ".dmp", ".trace", ".tmp", ".temp", ".bak", ".swp"};
                            foreach (var extension in logExtensions)
                            {
                                if (file.EndsWith(extension))
                                {
                                    main.logfiles.Add(file);
                                    long totalsize1 = new FileInfo(file).Length;
                                    string sizeformatted1 = formatsize(totalsize1);
                                    this.totalsize += totalsize1;
                                    FileInfo fileInfo = new FileInfo(file);
                                    size += fileInfo.Length;
                                    break;
                                }
                            }

                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = $"Scanning: {file}";
                            });
                        }
                    }

                    return size;
                }
                catch (Exception ex)
                {
                     return 0;
                }

            }
        
        public async Task ScanDirectoryAsync(string directory, string name, TextBlock directorytextblock)
            {
                long totalsize = await GetDirectorySizeAsync(directory);
                string sizeformatted = formatsize(totalsize);
                this.totalsize += totalsize;
          
                await main.Dispatcher.InvokeAsync(() =>
                {
                    directorytextblock.Text = $"Completed: {name} - {sizeformatted} found";
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

                        string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);

                        for (int i = 0; i < files.Length; i++)
                        {
                            if (main.cancelstatus == 1)
                            {
                                break;
                            }
                            var file = files[i];


                            FileInfo fileInfo = new FileInfo(file);
                            size += fileInfo.Length;


                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = $"Scanning: {file}";
                            });
                        }
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
                    if (File.Exists(filePath))
                    {
                        int totalLines = File.ReadLines(filePath).Count();
                        int currentLine = 0;


                        using (StreamReader reader = new StreamReader(filePath))
                        {
                            string line;


                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "Loading Database...";
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
                                    path = path.Replace("{##}", Environment.UserName);
                                }
                                if (line.Contains("#profile#="))
                                {
                                    path = main.stringtokenizer(line, "=", 1);
                                    if (line.Contains("{##}"))
                                    {
                                        path = path.Replace("{##}", Environment.UserName);
                                    }
                                  


                                        if (Directory.Exists(path))
                                        {
                                        await main.Dispatcher.InvokeAsync(async () =>
                                        {
                                            profilelist = new ComboBox
                                            {

                                                Foreground = Brushes.Black,
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
                                         

                                                if (profilelist != null && profilelist.Items.Count < 0)
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

                                    if (Directory.Exists(path) || File.Exists(path))
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
                                                            HorizontalAlignment = HorizontalAlignment.Left

                                                        };

                                                        newExpander = new Expander
                                                        {
                                                            Header = groupboxcontent,
                                                            Margin = new Thickness(5),
                                                            Background = new SolidColorBrush(Colors.Transparent),
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


                                                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 173, 216, 230)),

                                                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 45, 45, 45)),

                                                        FontSize = 12,
                                                        FontFamily = new FontFamily("Segoe UI"),
                                                        FontWeight = FontWeights.Regular,
                                                        FontStyle = FontStyles.Normal,
                                                    };
                                                    if (haswarning != null)
                                                    {
                                                        newCheckBox.Content = name + "=" + path + "=warning(" + haswarning + ")";
                                                    }
                                                    groupBoxContent.Children.Add(newCheckBox);


                                                    newExpander.Content = groupBoxContent;
                                                    try
                                                    {
                                                        main.wrapPanel1.Children.Add(newExpander);
                                                    }
                                                    catch (Exception)
                                                    {

                                                    }


                                                    newCheckBox.Checked += main.CheckBox_Checked;
                                                    newCheckBox.Unchecked += main.CheckBox_Unchecked;


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


                                                        Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 173, 216, 230)),


                                                        Foreground = new SolidColorBrush(Colors.Black),
                                                        VerticalAlignment = VerticalAlignment.Center,
                                                        FontSize = 12,
                                                        FontFamily = new FontFamily("Segoe UI"),
                                                        FontWeight = FontWeights.Bold,
                                                        FontStyle = FontStyles.Normal,
                                                        Padding = new Thickness(10),
                                                        Cursor = Cursors.Hand
                                                    };

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
                                main.label1_Copy.Content = "Scanning: ";
                                main.buttonStartScan.IsEnabled = true;
                                
                                main.progressBar1.Value = 0;
                            });

                        }
                    }
                } catch (Exception ex)
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
            this.Close();
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
                    Debug.WriteLine("added " + name + "=" + path);
                } else
                {
                    string name = stringtokenizer(content, "=", 0);
                    string path = stringtokenizer(content, "=", 1);

                    database.Add(name + "=" + path);
                    Debug.WriteLine("added " + name + "=" + path);
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
            }
        }
   
        private double previousWidth, previousHeight, previousLeft, previousTop;
        private double customMaximizedWidth, customMaximizedHeight;
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
        int cancelclean = 0;
        int reset = 0;
        public class Clean
        {
            MainWindow main;
            FileInfo fileinfo;
            long totalsize;
            public Clean(MainWindow main)
            {
                this.main = main;
            }
            public async void run()
            {
                for(int i = 0; i < main.database.Count; i++)
                {
                    if(main.database[i].EndsWith("=logscan")) {
                        main.database.RemoveAt(i);
                        break;
                    }
                }
                long size = main.database.Count();
                long nowcleaning = 0;
                TextBlock directorytextblock = null;
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
                        });
                        nowcleaning++;
                        if (fileinfo.Exists)
                        {
                            try
                            {
                                File.Delete(file);
                                long filesize = fileinfo.Length;
                                totalsize += filesize;
                            } catch (Exception ex)
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Content = "Cannot Delete : " + file + " " + ex.Message;
                                });
                                continue;
                            }
                        }
                        else
                        {
                         
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "File not found: " + file;
                            });
                            continue;
                        }
                    }
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.progressBar1.Value = 0;
                        nowcleaning = 0;
                    });
                }
                foreach (string file in main.database)
                {
                    if (main.cancelclean == 2)
                    {
                        break;
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
                    });
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        main.wrapPanelDirectories.Children.Add(directorytextblock);
                    });

                    try
                    {
                        nowcleaning++;
                        double percentage = (double)nowcleaning / size * 100;
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.progressBar1.Value = percentage;
                        });

                        if (File.Exists(path))
                        {

                            fileinfo = new FileInfo(path);
                            long filesize = fileinfo.Length;

                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "Deleting file: " + path;
                            });

                           
                            File.Delete(path);


                            totalsize += filesize;
                           
                        }
                        else if (Directory.Exists(path))
                        {
                            await cleandir(path);
                        } else
                        {
                            main.label1_Copy.Content = "File not found: " + path;
                        }

                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Cannot delete: " + path + " " + ex.Message;
                        });
                    }
                }

                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.label1_Copy.Content = "Cleaning done! " + formatsize(totalsize) + " Cleaned.";
                    main.label1_Copy.Foreground = Brushes.Green;
                    main.buttonCancel.Content = "Reset";
                    main.reset = 1;
                    main.cancelclean = 0;
                    main.cancelstatus = 0;
                });
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
            public async Task cleandir(string path)
            {
                string[] files = Directory.GetFiles(path);
                string[] dirs = Directory.GetDirectories(path);
                foreach (string file in files)
                {
                    if (main.cancelclean == 2)
                    {
                        break;
                    }
                    try
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Deleting file: " + file;
                        });


                        fileinfo = new FileInfo(file);
                        long filesize = fileinfo.Length;


                        File.Delete(file);


                        totalsize += filesize;
                        

                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Cannot delete file: " + file + " " + ex.Message;
                        });
                    }
                }
                foreach (string dir in dirs)
                {
                    if (main.cancelclean == 2)
                    {
                        break;
                    }
                    try
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Deleting directory: " + dir;
                        });

                        await cleandir(dir);
                        Directory.Delete(dir);
                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Cannot delete directory: " + dir + " " + ex.Message;
                        });
                    }
                }
            }
        }
            private void ButtonStartScan_Click(object sender, RoutedEventArgs e)
        {
            if(buttonStartScan.Content == "Clean")
            {
                buttonStartScan.IsEnabled = false;
                cancelstatus = 0;
                scanstatus = 0;
                cancelclean = 1;
                Clean clean = new Clean(this);
                Thread t = new Thread(new ThreadStart(clean.run));
                t.Start();
                wrapPanelDirectories.Children.Clear();
            } else
            {
                ScrollViewer.Visibility = Visibility.Hidden;
                wrapPanel1.Visibility = Visibility.Hidden;
                buttonCancel.IsEnabled = true;
                buttonStartScan.IsEnabled = false;
                Scan scan = new Scan(this);
                Thread t = new Thread(new ThreadStart(scan.run));
                t.Start();
                cancelclean = 0;
                cancelstatus = 0;
                scanstatus = 0;
            }
          

        }

   
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            logfiles.Clear();
            if (scanstatus == 1)
            {
                buttonCancel.IsEnabled = false;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Children.Clear();
                buttonStartScan.IsEnabled = true;
                ScrollViewer.Visibility = Visibility.Visible;
                wrapPanel1.Visibility = Visibility.Visible;
                buttonStartScan.Content = "Scan";

            } else if(scanstatus == 2) 
            {
                cancelstatus = 1;
            } else if(cancelclean == 1)
            {
                buttonCancel.IsEnabled = false;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Children.Clear();
                buttonStartScan.IsEnabled = true;
                ScrollViewer.Visibility = Visibility.Visible;
                wrapPanel1.Visibility = Visibility.Visible;
                cancelclean = 2;
                buttonStartScan.Content = "Scan";
            } else if(reset == 1)
            {
                buttonCancel.IsEnabled = false;
                ScrollViewerDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Visibility = Visibility.Hidden;
                wrapPanelDirectories.Children.Clear();
                buttonStartScan.IsEnabled = true;
                ScrollViewer.Visibility = Visibility.Visible;
                wrapPanel1.Visibility = Visibility.Visible;
                buttonStartScan.Content = "Scan";
                buttonCancel.Content = "Cancel";

                reset = 0;
            }
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
    }
}
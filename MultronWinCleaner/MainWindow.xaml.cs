using MFK;
using MultronWinCleaner;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Tracing;
using System.Drawing;
using System.IO;
using System.Reflection.Emit;
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
using static System.Net.WebRequestMethods;

namespace Multron_Win_Cleaner
{

    public partial class MainWindow : Window
    {

    
        List<string> logfiles = new List<string>();
        HashSet<string> excludedfiles = new HashSet<string>();
        public MainWindow()
        {
            InitializeComponent();
            if(System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt"))
            {
                string text = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt");
                if(text == "1")
                {
                    themeselector.selector(new Uri("Themes/Dark.xaml", UriKind.Relative));
                    CheckBox1.IsChecked = true;
                } else
                {
                    themeselector.selector(new Uri("Themes/Light.xaml", UriKind.Relative));
                    CheckBox1.IsChecked = false;
                }
            }
         
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
                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
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
                     Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
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
        HashSet<string> database = new HashSet<string>();
        public int scanstatus = 0;
        public int cancelstatus = 0;
        byte created = 0;
        List<CheckBox> checkboxes = new List<CheckBox>();
        StackPanel groupBoxContent = null;
        Expander expander = null;
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
                                main.logfiles.Add(file);
                                long fileSize = new FileInfo(file).Length;
                                size += fileSize;
                                this.totalsize += fileSize;

                                 
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Content = $"Scanning: {file}";
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
                                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            FontSize = 12,
                                            FontFamily = new FontFamily("Segoe UI"),
                                            FontWeight = FontWeights.Regular,
                                            FontStyle = FontStyles.Normal,
                                        };

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
                                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            BorderBrush = new SolidColorBrush(Colors.Transparent),
                                            BorderThickness = new Thickness(2),
                                            FontSize = 14,
                                            FontWeight = FontWeights.Regular,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            VerticalAlignment = VerticalAlignment.Top
                                        };
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
                                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            FontSize = 12,
                                            FontFamily = new FontFamily("Segoe UI"),
                                            FontWeight = FontWeights.Regular,
                                            FontStyle = FontStyles.Normal,

                                        };
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
                          


                            FileInfo fileInfo = new FileInfo(file);
                            size += fileInfo.Length;


                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = $"Scanning: {file}";
                            });
                            if(System.IO.File.Exists(file))
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
                                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            FontSize = 12,
                                            FontFamily = new FontFamily("Segoe UI"),
                                            FontWeight = FontWeights.Regular,
                                            FontStyle = FontStyles.Normal,
                                        };

                                        main.groupBoxContent = new StackPanel
                                        {
                                            Orientation = Orientation.Vertical,
                                            VerticalAlignment = VerticalAlignment.Top,
                                            HorizontalAlignment = HorizontalAlignment.Left

                                        };

                                        main.expander = new Expander
                                        {
                                            Header = "Select files in " + path,
                                            Margin = new Thickness(5),
                                            Background = new SolidColorBrush(Colors.Transparent),
                                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            BorderBrush = new SolidColorBrush(Colors.Transparent),
                                            BorderThickness = new Thickness(2),
                                            FontSize = 14,
                                            FontWeight = FontWeights.Regular,
                                            HorizontalAlignment = HorizontalAlignment.Left,
                                            VerticalAlignment = VerticalAlignment.Top
                                        };
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
                                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
                                            FontSize = 12,
                                            FontFamily = new FontFamily("Segoe UI"),
                                            FontWeight = FontWeights.Regular,
                                            FontStyle = FontStyles.Normal,

                                        };

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
                                                            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 176, 176, 176)),
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
                                                    if (haswarning != null)
                                                    {
                                                        newCheckBox.Content = name + "=" + path + "=warning(" + haswarning + ")";
                                                    }
                                                    newCheckBox.Checked += main.CheckBox_Checked;
                                                    newCheckBox.Unchecked += main.CheckBox_Unchecked;
                                                    groupBoxContent.Children.Add(newCheckBox);

                                                  
                                                    newExpander.Content = groupBoxContent;
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
        private void CheckBox2_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
            excludedfiles.Remove(file);
        }
        private void CheckBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            string file = stringtokenizer(checkBox.Content.ToString(), "=", 1);
            excludedfiles.Add(file);
           
        }
        private void CheckBox1_Checked(object sender, RoutedEventArgs e)
        {

            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" +  "Settings.txt", "1");
            themeselector.selector(new Uri("Themes/Dark.xaml", UriKind.Relative));
        }
        private void CheckBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            themeselector.selector(new Uri("Themes/Light.xaml", UriKind.Relative));
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt", "0");
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
                              
                              
                                if (!main.excludedfiles.Contains(file))
                                {
                                    fileinfo = new FileInfo(file);
                                    long filesize = fileinfo.Length;
                                 
                                    System.IO.File.Delete(file);
                                    totalsize += filesize;
                                    cleaned += filesize;
                           
                                } else
                                {
                                    await main.Dispatcher.InvokeAsync(() =>
                                    {
                                        main.label1_Copy.Content = "Skipping : " + file;
                                    });
                                }
                              
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
                        directorytextblock.Text = "Cleaned: " + "Deep Log Scan Files" + " " + main.formatsize(cleaned);
                    });
                    cleaned = 0;
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
                        });

                        if (System.IO.File.Exists(path))
                        {
                            if (!main.excludedfiles.Contains(file))
                            {

                                fileinfo = new FileInfo(path);
                                long filesize = fileinfo.Length;
                             
                                System.IO.File.Delete(path);

                                cleaned += filesize;
                                totalsize += filesize;

                            }
                            else
                            {
                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.label1_Copy.Content = "Skipping : " + file;
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
                                main.label1_Copy.Content = "File not found: " + path;
                            });

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
                    main.label1_Copy.Content = "Cleaning done! " + main.formatsize(totalsize) + " Cleaned.";
                    main.label1_Copy.Foreground = Brushes.Green;
                    main.buttonCancel.Content = "Reset";
                    main.reset = 1;
                    main.cancelclean = 0;
                    main.cancelstatus = 0;
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
                            main.label1_Copy.Content = "Deleting file: " + file;
                        });
                        if (!main.excludedfiles.Contains(file))
                        {
                            fileinfo = new FileInfo(file);
                            long filesize = fileinfo.Length;


                            System.IO.File.Delete(file);

                            cleaned += filesize;
                            totalsize += filesize;
                        }
                        else
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Content = "Skipping : " + file;
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Cannot delete file: " + file + " " + ex.Message;
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
                            main.label1_Copy.Content = "Deleting directory: " + dir;
                        });

                        await cleandir(dir, directorytextblock, name);
                        Directory.Delete(dir);
                    }
                    catch (Exception ex)
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Content = "Cannot delete directory: " + dir + " " + ex.Message;
                        });
                    }
                }));
              

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
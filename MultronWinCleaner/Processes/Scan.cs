using Multron_Win_Cleaner;
using Ookii.Dialogs.Wpf;
using System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace MultronWinCleaner.Processes
{

    public class Scan
    {
        MainWindow main;
        long totalsize = 0;
        long deeplogscantotal;
        long currentscan = 0;
        int winsxs = 0;
        int logscan = 0;
        int olderindex = 0;
        int accessindex = 0;
        CancellationTokenSource cts = new CancellationTokenSource();
        List<(string file, long size, string path, int days, string date, string modified)> checkboxData = new List<(string file, long size, string path, int days, string date, string modified)>();

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
            public string Path { get; set; }
            public long SizeBytes { get; set; }
            public int AgeDays { get; set; }
            public string CreatedDate { get; set; }
            public string ModifiedDate { get; set; }
            public string File => $"File to delete={FileName}={formatsize(SizeBytes)} | Created: {CreatedDate} ({AgeDays} days ago) | Modified: {ModifiedDate}";



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

            public List<FileItem> allFiles;

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
                    if (main.cancelstatus.IsCancellationRequested)
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

            }
            catch (TaskCanceledException)
            {

            }
        }

        public static async Task<string> RunDismAnalyzeComponentStoreAsync(
        CancellationToken externalToken,
        ProgressBar progressBar,
        int timeoutMinutes = 1)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = "/Online /Cleanup-Image /AnalyzeComponentStore /NoRestart",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                Verb = "runas",
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            linkedCts.CancelAfter(TimeSpan.FromMinutes(timeoutMinutes));

            proc.OutputDataReceived += async (s, e) =>
            {
                if (e.Data != null)
                {
                    outputBuilder.AppendLine(e.Data);


                    var pctRx = new Regex(@"(\d{1,3})(?:\.\d+)?\s?%", RegexOptions.Compiled);
                    var match = pctRx.Match(e.Data);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pct))
                    {
                        pct = Math.Clamp(pct, 0, 100);

                        await progressBar.Dispatcher.InvokeAsync(() =>
                        {
                            progressBar.IsIndeterminate = false;
                            progressBar.Value = pct;

                        });
                    }
                    else
                    {

                        await progressBar.Dispatcher.InvokeAsync(() =>
                        {
                            progressBar.IsIndeterminate = true;
                        });
                    }
                }
            };

            proc.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            try
            {
                if (!proc.Start())
                    throw new InvalidOperationException("DISM process could not be started.");

                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                Task<int> waitForExitTask = Task.Run(() =>
                {
                    proc.WaitForExit();
                    return proc.ExitCode;
                });

                Task completed = await Task.WhenAny(waitForExitTask, Task.Delay(Timeout.Infinite, linkedCts.Token));

                if (completed != waitForExitTask)
                {
                    try
                    {
                        if (!proc.HasExited)
                        {
                            proc.Kill(entireProcessTree: true);
                            await Task.Delay(500);
                        }
                    }
                    catch { }
                    linkedCts.Token.ThrowIfCancellationRequested();
                }

                var exitCode = await waitForExitTask;

                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    proc.Kill(entireProcessTree: true);
                    progressBar.Value = 100;
                });

                return outputBuilder.ToString() + "#=#" + exitCode;


            }
            catch (OperationCanceledException oce)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    proc.Kill(entireProcessTree: true);
                    progressBar.Value = 0;
                });
                return oce.Message;
            }
            catch (Exception ex)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    proc.Kill(entireProcessTree: true);
                    progressBar.Value = 0;
                });
                return ex.Message;
            }
        }



        private static readonly Regex ExtraSizeRx = new Regex(
      @"^\s*(?<label>Backups and Disabled Features|Cache and Temporary Data)\s*:\s*(?<value>[\d\.]+)\s*(?<unit>KB|MB|GB|TB)",
      RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.IgnoreCase);


        private static (long BackupsAndDisabledFeatures, long CacheAndTemporaryData) ParseSizes(string text)
        {
            long backups = -1;
            long cache = -1;

            foreach (Match match in ExtraSizeRx.Matches(text))
            {
                string label = match.Groups["label"].Value;
                string value = match.Groups["value"].Value;
                string unit = match.Groups["unit"].Value.ToUpperInvariant();

                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                    continue;

                long bytes = unit switch
                {
                    "KB" => (long)(number * 1024),
                    "MB" => (long)(number * 1024 * 1024),
                    "GB" => (long)(number * 1024 * 1024 * 1024),
                    "TB" => (long)(number * 1024L * 1024L * 1024L * 1024L),
                    _ => -1
                };

                if (label.Contains("Backups", StringComparison.OrdinalIgnoreCase))
                    backups = bytes;
                else if (label.Contains("Cache", StringComparison.OrdinalIgnoreCase))
                    cache = bytes;
            }

            return (backups, cache);
        }

        public async Task run()
        {
            try
            {
                long size = main.database.Count();
                long nowscanning = 0;
                MainWindow.scanstatus = 2;
                main.onclean = 1;


                await main.Dispatcher.InvokeAsync(() =>
                {

                    olderindex = main.settings.cmbAgePreset.SelectedIndex;
                    var task = ScandotsAsync("Scanning", cts.Token);
                    main.wrapPanelDirectories.Visibility = Visibility.Visible;
                    main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                    main.progressBar1.Value = 0;
                });

                foreach (string directory in main.database)
                {
                    if (main.cancelstatus.IsCancellationRequested) break;

                    string name = main.stringtokenizer(directory, "=", 0);
                    string path = main.stringtokenizer(directory, "=", 1);
                    TextBlock directorytextblock = null;
                    if(!name.Contains("Dism.exe")) {
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
                    } else
                    {
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            directorytextblock = new TextBlock
                            {
                                Text = $"{name} Scanning WinSxS",
                                Foreground = System.Windows.Media.Brushes.Goldenrod,
                                FontSize = 16,
                                Margin = new Thickness(5)
                            };
                            main.wrapPanelDirectories.Children.Add(directorytextblock);
                        });
                    }
                

                    if (name.Contains("Dism.exe") && winsxs == 0)
                    {
                        try
                        {
                            string output = await RunDismAnalyzeComponentStoreAsync(main.dismcancel.Token, main.progressBar1);

                            if (!main.cancelstatus.IsCancellationRequested)
                            {


                                string outputPath = System.IO.Path.Combine(Environment.CurrentDirectory, "dism_scan.log");

                                await System.IO.File.WriteAllTextAsync(outputPath, output);

                                var sizes = ParseSizes(output);
                                long backups = Math.Max(0, sizes.BackupsAndDisabledFeatures);
                                long cache = Math.Max(0, sizes.CacheAndTemporaryData);
                                long total = cache + backups;
                                totalsize += total;


                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    main.wrapPanelDirectories.Children.Remove(directorytextblock);
                                    if (output.EndsWith("#=#3010"))
                                    {

                                        directorytextblock = new TextBlock
                                        {
                                            Text = $"Completed: {name + " " + main.formatsize(total)} " + "(Dism Requires System Restart)",
                                            Foreground = System.Windows.Media.Brushes.Goldenrod,
                                            FontSize = 16,
                                            Margin = new Thickness(5)
                                        };
                                        checkboxData.Add(("(Dism Requires System Restart)=", total, "WinSxS Clean", 0, "", ""));

                                    }
                                    else
                                    {

                                        directorytextblock = new TextBlock
                                        {
                                            Text = $"Completed: {name + " " + main.formatsize(total)}",
                                            Foreground = System.Windows.Media.Brushes.Goldenrod,
                                            FontSize = 16,
                                            Margin = new Thickness(5)
                                        };
                                        checkboxData.Add(("Dism.exe", total, "WinSxS Scan Result", 0, "", ""));

                                    }


                                    main.wrapPanelDirectories.Children.Add(directorytextblock);
                                });
                            }
                            winsxs = 1;
                        }
                        catch (OperationCanceledException)
                        {
                            winsxs = 1;
                            main.cancelstatus.Cancel();
                            break;
                        }


                    }
                    else
                    {
                      if (name.Contains("Deep Log Files Scan") && logscan == 0)
                        {
                            cts.Cancel();
                            current = 0;
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                var animation = new DoubleAnimation
                                {
                                    From = 0,
                                    To = 100,
                                    Duration = TimeSpan.FromSeconds(2),
                                    RepeatBehavior = RepeatBehavior.Forever
                                };
                                main.progressBar1.BeginAnimation(ProgressBar.ValueProperty, animation);
                            });
                      
                            await ScanCDirectoryAsync(path);
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.progressBar1.BeginAnimation(ProgressBar.ValueProperty, null);
                                main.progressBar1.Value = 100;
                            });
                            await directorytextblock.Dispatcher.InvokeAsync(() =>  directorytextblock.Text = $"Completed: {name} {main.formatsize(deeplogscantotal)}");

                            logscan = 1;
                        
                        }
                        else if (System.IO.File.Exists(path) && !main.settings.excludedfiles.Contains(path))
                        {
                            await addtocheckbox(path, name, "Direct Files");
                        } 

                        else
                        {
                            currentscan = 0;

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


                 
                    var groupedByPath = await Task.Run(() => checkboxData.GroupBy(x => x.path).ToList());

                    int totalGroups = groupedByPath.Count;
                    int currentGroup = 0;
                    long newtotalsize = 0;
                    cts.Cancel();
                    foreach (var group in groupedByPath)
                    {

                        string path = group.Key;
                        var items = group.ToList();

                        var allFiles = items.Select(item => new FileItem
                        {
                            FileName = item.file.Contains("Dism.exe", StringComparison.OrdinalIgnoreCase)
             ? $"Clean WinSxS Folder={item.file}=WinSxS Scan Result {main.formatsize(item.size)}"
             : $"{item.file}={main.formatsize(item.size)} | Created: {item.date} ({item.days} days) | Last Access: {item.modified}",
                            SizeBytes = item.size,
                            IsChecked = true,
                            Path = item.file,
                            AgeDays = item.days,
                            CreatedDate = item.date,
                            ModifiedDate = item.modified
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

                    main.DataContext = main.viewModel;





                    main.label1_Copy.Text = "Loading Completed!";
                    main.progressBar1.Value = 100;
                    main.label1_Copy.Foreground = System.Windows.Media.Brushes.Goldenrod;
                    main.buttonReset.Visibility = Visibility.Visible;
                    main.buttonStartScan.IsEnabled = true;
                    main.buttonStartScan.Content = "Clean";

                    if (main.autoclean == 1)
                    {
                        if (main.cancelstatus.IsCancellationRequested)
                        {
                            main.buttonStartScan.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            main.label1_Copy.Text = $"Auto scan canceled! + {main.formatsize(totalsize)}  Useless file found! {DateTime.Now}";
                        } else
                        {
                            main.buttonStartScan.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                            main.label1_Copy.Text = $"Auto scan completed! + {main.formatsize(totalsize)}  Useless file found! {DateTime.Now}";
                        }

                    }
                    else
                    {
                        if (main.cancelstatus.IsCancellationRequested)
                        {
                            main.label1_Copy.Text = $"Scan canceled! + {main.formatsize(totalsize)}  Useless file found! {DateTime.Now}";

                        } else
                        {
                            main.label1_Copy.Text = $"Scan completed! + {main.formatsize(totalsize)}  Useless file found! {DateTime.Now}";
                        }

                    }


                });



                MainWindow.scanstatus = 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }


        public async Task addtocheckbox(string file, string path, string process)
        {

            await main.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    if (!System.IO.File.Exists(file))
                    {

                        return;

                    }
                    FileInfo fileinfo = new FileInfo(file); 

                    DateTime datetime = fileinfo.CreationTime;
                    DateTime accessdate = fileinfo.LastAccessTime;
                    DateTime modifiedate = fileinfo.LastWriteTime;

                    double days = (DateTime.Now - datetime).TotalDays;
                    double date = (DateTime.Now - accessdate).TotalDays;
                 
                    if (file.Contains("Dism.exe"))
                    {
                        totalsize += int.Parse(path);
                        currentscan += int.Parse(path);
                        checkboxData.Add((file, int.Parse(path), process, (int)days, datetime.ToString(), modifiedate.ToString()));
                        return;
                    }

                    void AddEntry()
                    {
                        long fSize = fileinfo.Length;
                        totalsize += fSize;
                        currentscan += fSize;
                     
                        if (process.Contains("Deep Log Scanner Result"))
                        {
                            main.logfiles.Add(file);
                            deeplogscantotal += fSize;
                        }
                        checkboxData.Add((file, fSize, path, (int)days, datetime.ToString(), modifiedate.ToString()));
                    }
                   
                    if (main.settings.OldScan.IsChecked == true || main.settings.AccessScan.IsChecked == true)
                    {
                        if (main.settings.AccessScan.IsChecked == true)
                        {
                            bool pass = accessindex switch
                            {
                                0 => date > 7,
                                1 => date > 30,
                                2 => date > 90,
                                3 => int.TryParse(main.settings.txtCustomAccess.Text, out int ca) && date > ca,
                                _ => false
                            };
                            if (pass) AddEntry();
                        }

                        if (main.settings.OldScan.IsChecked == true)
                        {
                            bool pass = olderindex switch
                            {
                                0 => days > 7,
                                1 => days > 30,
                                2 => days > 90,
                                3 => int.TryParse(main.settings.txtCustomDay.Text, out int cd) && days > cd,
                                _ => false
                            };
                            if (pass) AddEntry();
                        }
                    }
                    else 
                    {
                        AddEntry();
                    }
                }
                catch (Exception ex)
                {
                    catchexception(ex.Message + " " + ex.StackTrace);
                }
            });
        }

        public void catchexception(string message)
        {
            using (StreamWriter writer = new StreamWriter(Environment.CurrentDirectory + "\\exceptions.txt"))
            {
                writer.WriteLine(message);
            }
        }


        public static long total = 0;
        public static long current = 0;
        public async Task ScanCDirectoryAsync(string path)
        {
         
            try
            {
                if (Directory.Exists(path))
                {
                    foreach (string dir in Directory.GetDirectories(path))
                    {
                        if (main.cancelstatus.IsCancellationRequested) break;
                        await ScanCDirectoryAsync(dir);
                    }
                    foreach (string file in Directory.GetFiles(path))
                    {
                        if (main.cancelstatus.IsCancellationRequested) break;

                        if (!main.settings.excludedfiles.Contains(file))
                        {
                            if (main.extensions.Split('.').Any(ext => file.EndsWith($".{ext}")))
                            {
                                await addtocheckbox(file, path, "Deep Log Scanner Result");
                            }
                        }

                        current++;
                        if (current % 100 == 0)
                        {
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                main.label1_Copy.Text = $"Scanning C:\\ — {current} files checked, {main.formatsize(deeplogscantotal)} found";
                            });
                        }
                    }

                }
            }
            catch (Exception Ex) {
             
            }

           
        }

        public async Task ScanDirectoryAsync(string path, string name, TextBlock directorytextbox)
        {
            try
            {
                if (Directory.Exists(path))
                {
                     
                    foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    {
                        if (main.cancelstatus.IsCancellationRequested) break;
                        if (!main.settings.excludedfiles.Contains(file))
                        {
                            await addtocheckbox(file, path, name);
                           
                     
                        }
                    }
                }
            }
            catch { }

            await directorytextbox.Dispatcher.InvokeAsync(() => directorytextbox.Text = $"Completed: {name} {main.formatsize(currentscan)}");
          
        }

     


    }
}

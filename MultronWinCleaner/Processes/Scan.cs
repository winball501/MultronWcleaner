using Multron_Win_Cleaner;
using Ookii.Dialogs.Wpf;
using System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
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
        int health = 0;
        int sfc = 0;
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
        public static async Task<string> RunSfcCommandAsync(
    string arguments,
    CancellationToken externalToken,
    ProgressBar progressBar,
    int timeoutMinutes = 20)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sfc.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.Unicode, 
                StandardErrorEncoding = Encoding.Unicode,
                UseShellExecute = false,
                Verb = "runas",
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var outputBuilder = new StringBuilder();

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

            proc.ErrorDataReceived += (s, e) => { };

            try
            {
                if (!proc.Start())
                    throw new InvalidOperationException("sfc.exe could not be started.");

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

                await waitForExitTask;

                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 100;
                });

                return outputBuilder.ToString();
            }
            catch (OperationCanceledException oce)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                return oce.Message;
            }
            catch (Exception ex)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                return ex.Message;
            }
        }
        public static async Task<string> RunDismCommandWithProgressAsync(
   string arguments,
   CancellationToken externalToken,
   ProgressBar progressBar,
   int timeoutMinutes = 10)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = arguments,
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
                    progressBar.Value = 100;
                });

                return outputBuilder.ToString() + "#=#" + exitCode;
            }
            catch (OperationCanceledException oce)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                return oce.Message;
            }
            catch (Exception ex)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                return ex.Message;
            }
        }

        
        public static Task<string> RunDismAnalyzeComponentStoreAsync(
            CancellationToken externalToken, ProgressBar progressBar, int timeoutMinutes = 1)
            => RunDismCommandWithProgressAsync("/Online /Cleanup-Image /AnalyzeComponentStore /NoRestart", externalToken, progressBar, timeoutMinutes);


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
        public async void dropscanmessage(string name, string Color)
        {
            await main.Dispatcher.InvokeAsync(() =>
            {
               
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (System.Windows.Media.Brush)converter.ConvertFromString(Color);

                TextBlock directorytextblock = new TextBlock
                {
                    Text = name,
                    Foreground = brush,  
                    FontSize = 16,
                    Margin = new Thickness(5)
                };
                main.wrapPanelDirectories.Children.Add(directorytextblock);
            });
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
                    main.AnimateIcon(true);
                });
   
                foreach (string directory in main.database)
                {
                    if (main.cancelstatus.IsCancellationRequested) break;

                    string name = main.stringtokenizer(directory, "=", 0);
                    string path = main.stringtokenizer(directory, "=", 1);

 


                    if (name.Contains("cleanmgr.exe"))
                    {
                        dropscanmessage("The cleanmgr.exe command will run after the scan is completed and you click the Clean button!", "#0078d7");
                        continue;
                    }
                    else if (name.Contains("Dism.exe") && !path.Contains("RestoreHealth", StringComparison.OrdinalIgnoreCase) && winsxs == 0)
                    {
                   
                        try
                        {
                            dropscanmessage("Running Command: " + name, "#0078d7");
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
                                    if (output.EndsWith("#=#3010"))
                                    {
                                        checkboxData.Add(("Dism.exe(Dism Requires System Restart)=", total, "Dism Command: /Online /Cleanup-Image /StartComponentCleanup", 0, "", ""));
                                        dropscanmessage("Dism.exe scan completed but requires system restart to free up space. Please restart your computer to complete the cleanup process. " + main.formatsize(total), "#0078d7");
                                    }
                                    else
                                    {
                                        checkboxData.Add(("Dism.exe=", total, "Dism Command: /Online /Cleanup-Image /StartComponentCleanup", 0, "", ""));
                                        dropscanmessage("Dism.exe scan completed! " + main.formatsize(total), "#107c10");
                                    }
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

                    else if (name.Contains("Dism.exe") && path.Contains("RestoreHealth", StringComparison.OrdinalIgnoreCase) && health == 0)
                    {
                        try
                        {
                            dropscanmessage("Checking component store health (Dism.exe /ScanHealth)...", "#0078d7");

                            string output = await RunDismCommandWithProgressAsync(
                                "/Online /Cleanup-Image /ScanHealth /NoRestart",
                                main.dismcancel.Token,
                                main.progressBar1,
                                timeoutMinutes: 10);

                            if (!main.cancelstatus.IsCancellationRequested)
                            {
                                bool isCorrupt = output.Contains("is repairable", StringComparison.OrdinalIgnoreCase);

                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    if (isCorrupt)
                                    {
                                        checkboxData.Add(("Dism.exe(Restore Health)=", 0, "Dism Command: /Online /Cleanup-Image /RestoreHealth Restore Health command is recommended.", 0, "", ""));
                                        dropscanmessage("Component store corruption detected! Restore Health command is recommended.", "#D83B01");
                                    }
                                    else
                                    {
                                        checkboxData.Add(("Dism.exe(Restore Health)=", 0, "Dism Command: /Online /Cleanup-Image /RestoreHealth Restore Health is not needed", 0, "", ""));
                                        dropscanmessage("No component store corruption detected. Restore Health is not needed.", "#107c10");
                                    }
                                });
                            }
                            health = 1;
                        }
                        catch (OperationCanceledException)
                        {
                            main.cancelstatus.Cancel();
                            break;
                        }
                    }
                    else if (name.Contains("Deep Log Files Scan") && logscan == 0)
                    {
                        dropscanmessage("Running Deep Log Files Scan", "#0078d7");
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
                        dropscanmessage("Deep Log Files Scan Completed!", "#107c10");

                        logscan = 1;




                    }
                    else if (name.ToLower().Contains("sfc.exe") && sfc == 0)
                    {
                        try
                        {
                            dropscanmessage("Checking system files (sfc /verifyonly)...", "#0078d7");

                            string output = await RunSfcCommandAsync(
                                "/verifyonly",
                                main.dismcancel.Token,
                                main.progressBar1,
                                timeoutMinutes: 20);

                            if (!main.cancelstatus.IsCancellationRequested)
                            {
                                bool isClean = output.IndexOf("did not find any integrity violations", StringComparison.OrdinalIgnoreCase) >= 0;

                                await main.Dispatcher.InvokeAsync(() =>
                                {
                                    if (!isClean)
                                    {
                                        checkboxData.Add(("sfc.exe(System File Repair)=", 0,    "SFC Command: /scannow", 0, "", ""));
                                        dropscanmessage("System file integrity violations detected! sfc /scannow repair is recommended.", "#D83B01");
                                    }
                                    else
                                    {
                                        dropscanmessage("No system file integrity violations detected. sfc /scannow is not needed.", "#107c10");
                                    }
                                });
                            }
                            sfc = 1;
                        }
                        catch (OperationCanceledException)
                        {
                            main.cancelstatus.Cancel();
                            break;
                        }
                      
                    }
                    else
                    {
                        dropscanmessage("Scanning: " + name, "#0078d7");
                        if (System.IO.File.Exists(path) && !main.settings.excludedfiles.Contains(path))
                        {
                            await addtocheckbox(path, name, "Direct Files");
                        }

                        else
                        {
                            currentscan = 0;

                            await ScanDirectoryAsync(path, name);
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
                            FileName = $"{item.file}={main.formatsize(item.size)} | Created: {item.date} ({item.days} days) | Last Access: {item.modified}",
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
                
                    main.AnimateIcon(false);
                 
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
                            main.Dispatcher.Invoke(() =>
                            {
                                if (main.settings.chkEnableNotifyScan.IsChecked == true && main.Visibility == Visibility.Hidden)
                                {
                                    Notify notify = new Notify("Scan Information", $"Your system scan done!\r\n", $"{main.formatsize(totalsize)}");
                                    notify.Show();
                                }
                            });
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
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                if (main.settings.chkEnableNotifyScan.IsChecked == true && main.Visibility == Visibility.Hidden)
                                {
                                    Notify notify = new Notify("Scan Information", $"Your system scan done!\r\n", $"{main.formatsize(totalsize)}");
                                    notify.Show();
                                }
                            });
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
            try
            {
         
                if (!System.IO.File.Exists(file))
                {
                    return;
                }

                FileInfo fileinfo = new FileInfo(file);

                DateTime creationTime = fileinfo.CreationTime;
                DateTime accessDate = fileinfo.LastAccessTime;
                DateTime modifiedDate = fileinfo.LastWriteTime; 
                long fSize = fileinfo.Length;
                 
                double modifiedDays = (DateTime.Now - modifiedDate).TotalDays;
                double accessDays = (DateTime.Now - accessDate).TotalDays;
                 
                bool isAccessChecked = false;
                bool isOldChecked = false;
                int customAccess = 0;
                int customOld = 0;
                 
                await main.Dispatcher.InvokeAsync(() =>
                {
                    isAccessChecked = main.settings.AccessScan.IsChecked == true;
                    isOldChecked = main.settings.OldScan.IsChecked == true;

                    int.TryParse(main.settings.txtCustomAccess.Text, out customAccess);
                    int.TryParse(main.settings.txtCustomDay.Text, out customOld);
                });

                bool shouldAdd = false;
                 
                if (isAccessChecked || isOldChecked)
                {
                    if (isAccessChecked)
                    {
                        shouldAdd = accessindex switch
                        {
                            0 => accessDays > 7,
                            1 => accessDays > 30,
                            2 => accessDays > 90,
                            3 => customAccess > 0 && accessDays > customAccess,  
                            _ => false
                        };
                    }
                    else if (isOldChecked)  
                    {
                        shouldAdd = olderindex switch
                        {
                            0 => modifiedDays > 7,
                            1 => modifiedDays > 30,
                            2 => modifiedDays > 90,
                            3 => customOld > 0 && modifiedDays > customOld,
                            _ => false
                        };
                    }
                }
                else
                { 
                    shouldAdd = true;
                } 
                if (shouldAdd)
                {
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        totalsize += fSize;
                        currentscan += fSize;

                        if (process.Contains("Deep Log Scanner Result"))
                        {
                            main.logfiles.Add(file);
                            deeplogscantotal += fSize;
                        }
                         
                        checkboxData.Add((file, fSize, path, (int)modifiedDays, creationTime.ToString(), modifiedDate.ToString()));
                    });
                }
            }
            catch (Exception ex)
            {
          
                await main.Dispatcher.InvokeAsync(() =>
                {
                    catchexception(ex.Message + " " + ex.StackTrace);
                });
            }
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
                                 
                                var converter = new System.Windows.Media.BrushConverter();
                                main.label1_Copy.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString("#0078d7");
                            });
                        }
                    }

                }
            }
            catch (Exception Ex) {
             
            }

           
        }

        public async Task ScanDirectoryAsync(string path, string name)
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
            dropscanmessage($"Completed: {name} {main.formatsize(currentscan)}", "#107c10");
          
          
        }

     


    }
}

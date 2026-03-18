using MFK;
using Microsoft.VisualBasic.Logging;
using Multron_Win_Cleaner;
using MultronWinCleaner;
using System;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using static Multron_Win_Cleaner.MainWindow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
namespace MultronWinCleaner.Processes
{
    public class Clean
    {
        public MainWindow main;
        private long totalsize;
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
                    if (main.cancelclean == 2)
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
      public async Task CleanupWinSxSWithRealProgress(
      string logPath,
      ProgressBar progressBar,
      CancellationToken cancellationToken)
        {
            var dir = Path.GetDirectoryName(logPath)!;
            Directory.CreateDirectory(dir);

            if (File.Exists(logPath))
            {
                try { File.Delete(logPath); }
                catch (IOException) { }
            }

            var psi = new ProcessStartInfo
            {
                FileName = "dism.exe",
                Arguments = $"/Online /Cleanup-Image /StartComponentCleanup /NoRestart /LogPath:\"{logPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Verb = "runas"
            };

            using var dismProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            var progressRegex = new Regex(@"(\d{1,3})\.?\d*\s?%", RegexOptions.Compiled);

            dismProcess.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);

                    var match = progressRegex.Match(e.Data);
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int pct))
                    {
                        pct = Math.Clamp(pct, 0, 100);
                        _ = progressBar.Dispatcher.InvokeAsync(() =>
                        {
                            progressBar.IsIndeterminate = false;
                            progressBar.Value = pct;
                        });
                    }
                }
            };

            dismProcess.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            try
            {
                if (!dismProcess.Start())
                    throw new InvalidOperationException("DISM process could not be started.");

                dismProcess.BeginOutputReadLine();
                dismProcess.BeginErrorReadLine();

                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = true;
                    progressBar.Value = 0;
                });

                var waitForExitTask = Task.Run(() =>
                {
                    dismProcess.WaitForExit();
                    return dismProcess.ExitCode;
                });

                var completed = await Task.WhenAny(waitForExitTask, Task.Delay(Timeout.Infinite, cancellationToken));

                if (completed != waitForExitTask)
                {
                    try
                    {
                        if (!dismProcess.HasExited)
                        {
                            dismProcess.Kill(entireProcessTree: true);
                            await Task.Delay(500);
                        }
                    }
                    catch { }

                    throw new OperationCanceledException("DISM operation was cancelled.");
                }

                var exitCode = await waitForExitTask;

                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 100;
                });

                if (exitCode != 0)
                {
                    AddStatusTextBlock($"Error: DISM exited with code {exitCode}. Error:\n{errorBuilder}");
                }
                else
                {
                    AddStatusTextBlock("Cleaned: WinSxS Folder");
                }

                 
                if (File.Exists(logPath))
                {
                    using var fs = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string finalLogContent = await reader.ReadToEndAsync();
              
                }
            }
            catch (OperationCanceledException)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                AddStatusTextBlock("Warning: Cleanup cancelled.");
            }
            catch (Exception ex)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                AddStatusTextBlock($"Error: {ex.Message}");
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
            int totalCount = main.database.Count;
            int cleanedCount = 0;
            totalsize = new DriveInfo("C:\\").AvailableFreeSpace;

            await UpdateStatusColor(System.Windows.Media.Brushes.Blue);
         
            if (main.logfiles.Count > 0)
            {
                var status = await AddStatusTextBlock("Cleaning: Finded Log Files in C:\\ ");
                int current = 0;
                foreach (string logfile in main.logfiles)
                {
                    try
                    {
                        var checkedFiles = main.viewModel.Groups.SelectMany(g => g.allFiles).Where(f => f.IsChecked).ToList();


                        var fileToDelete = checkedFiles.FirstOrDefault(f => string.Equals(f.Path, logfile, StringComparison.OrdinalIgnoreCase));

                        if (fileToDelete != null)
                        {
                            if (!main.settings.excludedfiles.Contains(logfile))
                            {
                                if (File.Exists(logfile))
                                {
                                    File.Delete(logfile);

                                }
                            }
                        }
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            main.label1_Copy.Text = $"Cleaning {current} \\ {main.logfiles.Count}";

                        });
                        current++;
                        await UpdateProgress(current, main.logfiles.Count);
                    }
                    catch (IOException ioEx)
                    {
                        int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;
                        if (errorCode == 0x20 || errorCode == 0x21)
                            main.paths.Add(logfile + "=" + "Deep Log Scanner Result");
                    }
                    catch (UnauthorizedAccessException)
                    {
                        main.paths.Add(logfile + "=" + "Deep Log Scanner Result");
                    }
                    catch (Exception ex)
                    {
                         
                    }


                }
                await main.Dispatcher.InvokeAsync(() =>
                {
                    status.Text = $"Cleaned: Finded Log Files in C:\\";
                });
                   

            }


            for (int i = 0; i < main.database.Count; i++)
            {
                string item = main.database[i];
            
                if (main.cancelclean == 2) break;

                cleanedCount++;
                await UpdateProgress(cleanedCount, totalCount);

                string name = GetToken(item, 0);
              
                string path = GetToken(item, 1);
               

                if (name.Contains("Dism.exe") && winsxs == 0)
                {

                    var checkedFiles = main.viewModel.Groups.SelectMany(g => g.allFiles).Where(f => f.IsChecked).ToList();


                    var isWinSxSChecked = checkedFiles.FirstOrDefault(f => string.Equals(f.Path, name, StringComparison.OrdinalIgnoreCase));
                    if (isWinSxSChecked == null  ||!isWinSxSChecked.IsChecked)
                    {
                        continue;
                    }
                    else
                    {
                        await AddStatusTextBlock("Dism.exe Cleaning WinSxS");



                        string logFile = System.IO.Path.Combine(Environment.CurrentDirectory, "dism_cleanup.log");
                        if (System.IO.File.Exists(logFile)) System.IO.File.Delete(logFile);

                        await CleanupWinSxSWithRealProgress(logFile, main.progressBar1, main.dismcancel.Token);
                        winsxs = 1;
                        continue;

                    }
                  
                }

                var statusBlock = await AddStatusTextBlock($"Cleaning: {name}");

                try
                {
                    int iscleaned = 0;
                    if (File.Exists(path))
                    {
                        var checkedFiles = main.viewModel.Groups.SelectMany(g => g.allFiles).Where(f => f.IsChecked).ToList();


                        var fileToDelete = checkedFiles.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));

                        if (fileToDelete != null)
                        {
                            if (!main.settings.excludedfiles.Contains(path))
                            {
                                if (File.Exists(path))
                                {
                                     File.Delete(path);
                                     iscleaned = 1;
                                }
                            }
                        }



                    }
                    else if (Directory.Exists(path))
                    {
                        var targetGroup = main.viewModel.Groups.FirstOrDefault(g => g.Path != null &&  g.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase));

                        if (targetGroup == null || targetGroup.allFiles == null || targetGroup.allFiles.Count == 0)
                        {
                            iscleaned = 2;
                        }
                        else
                        {
                            bool allUnchecked = targetGroup.allFiles.All(f => !f.IsChecked);

                            if (!allUnchecked)
                            {
                                await CleanDirectory(path, name);
                                await DeleteEmptyDirectories(path);
                                iscleaned = 1;
                            }
                        }



                    }
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        if (iscleaned == 0)
                        {
                            statusBlock.Text = $"Ignored: {name}";
                            statusBlock.Foreground = Brushes.Goldenrod;
                        }
                        else if (iscleaned == 2)
                        {
                            statusBlock.Text = $"Folder Empty: {name}";
                            statusBlock.Foreground = Brushes.DarkGreen;

                        } else if (name.Contains("Dism.exe")) {
                            statusBlock.Text = $"{name} Operation Done. Log files in current directory of mwc.";
                        } else
                        {
                            statusBlock.Text = $"Cleaned: {name}";
                            statusBlock.Foreground = Brushes.Blue;
                        }
                    });
                   
                  
                }
                catch (IOException ioEx)
                {
                    const int ERROR_SHARING_VIOLATION = 0x20;
                    const int ERROR_LOCK_VIOLATION = 0x21;
                    int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;
                    if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                    {
                        if (File.Exists(path))
                        {
                            main.paths.Add(path + "=" + name);
                        }
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            statusBlock.Text = $"Locked: {name}";
                            statusBlock.Foreground = Brushes.Red;
                        });
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    main.paths.Add(path + "=" + name);
                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        statusBlock.Text = $"Access Denied: {name}";
                        statusBlock.Foreground = Brushes.Red;
                    });
                }
                catch (Exception ex)
                {
                 
                }
            }

            await FinalizeCleaning();
        }
        public void catchexception(string message)
        {
            using (StreamWriter writer = new StreamWriter(Environment.CurrentDirectory + "\\exceptions.txt"))
            {
                writer.WriteLine(message);
            }
        }
        public class LockedFileGroupViewModel : INotifyPropertyChanged
        {
            public string GroupName { get; set; }

            public string DisplayName { get; set; }

            public string FilePath { get; set; }



            public ObservableCollection<LockedProcessViewModel> Processes { get; set; }

            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public class LockedProcessViewModel : INotifyPropertyChanged
        {
            public string DisplayName { get; set; }
            public string GroupName { get; set; }
            public string FilePath { get; set; }
            public string FilePathWithoutSize { get; set; }
            public string Id { get; set; }
            private bool _isChecked;
            public bool IsChecked
            {
                get => _isChecked;
                set
                {
                    if (_isChecked != value)
                    {
                        _isChecked = value;
                        OnPropertyChanged();
                        OnCheckedChanged?.Invoke(this, value);
                    }
                }
            }
            public Action<LockedProcessViewModel, bool> OnCheckedChanged { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        }

        public class LockedFileGroup
        {
            public string FilePath { get; set; }

        }




        private async Task CleanDirectory(string path, string name)
        {

            try
            {
                foreach (var subDir in Directory.GetDirectories(path))
                {
                    await CleanDirectory(subDir, name);
                }
            }
            catch (Exception ex)
            {
                
            }
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    try
                    {
                        var checkedFiles = main.viewModel.Groups.SelectMany(g => g.allFiles).Where(f => f.IsChecked).ToList();


                        var fileToDelete = checkedFiles.FirstOrDefault(f => string.Equals(f.Path, file, StringComparison.OrdinalIgnoreCase));

                        if (fileToDelete != null)
                        {
                            if (!main.settings.excludedfiles.Contains(file))
                            {
                                if (File.Exists(file))
                                {
                                    File.Delete(file);
                                }
                            }
                        }

                    }
                    catch (IOException ioEx)
                    {
                        const int ERROR_SHARING_VIOLATION = 0x20;
                        const int ERROR_LOCK_VIOLATION = 0x21;

                        int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;
                        if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                        {
                            main.paths.Add(file + "=" + name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        main.paths.Add(file + "=" + name);
                    }
                    catch (Exception ex)
                    {
                        catchexception(ex.Message + " " + ex.StackTrace);
                    }
                }
            } catch (Exception ex)
            {

            }
        

              
           
        
        }
        public async Task DeleteEmptyDirectories(string parentPath)
        {
   
               
                foreach (string subDir in Directory.GetDirectories(parentPath))
                {
                    await DeleteEmptyDirectories(subDir);

                  
                    if (Directory.Exists(subDir) && Directory.GetFileSystemEntries(subDir).Length == 0)
                    {
                        try
                        {
                            Directory.Delete(subDir);
                        }
                        catch (Exception ex)
                        {
                            catchexception(ex.Message + " " + ex.StackTrace);
                        }
                    }
                }
          
        }



        public class LockedFileViewModel
        {
            public string FilePath { get; set; }
            public string LockedByProcess { get; set; }
        }
        private async Task FinalizeCleaning()
        {

            string resultMessage = "";
            await cts.CancelAsync();
            long newFreeSpace = new DriveInfo("C:\\").AvailableFreeSpace;
            long freedSpace = newFreeSpace - totalsize;
            if (main.paths.Count > 0)
            {
                await main.Dispatcher.InvokeAsync(() =>
                {
                    main.ButtonLockedFiles.Visibility = Visibility.Visible;
                });
             
                resultMessage = main.autoclean == 1
                         ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}  Locked Files Found! {main.paths.Count}"
                         : $"Cleaning done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}";
             
            }
            else
            {
                resultMessage = main.autoclean == 1
                          ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}"
                          : $"Cleaning done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}";
            }



            await main.Dispatcher.InvokeAsync(() =>
            {
                main.label1_Copy.Text = resultMessage;
                main.label1_Copy.Foreground = System.Windows.Media.Brushes.Green;
                main.reset = 1;
                main.cancelclean = 0;
                
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
}

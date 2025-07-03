using MFK;
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
using System.IO;
using System.Linq;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Text.RegularExpressions;
using System.Windows.Data;
using static Multron_Win_Cleaner.MainWindow;
namespace MultronWinCleaner.Processes
{
    public class Clean
    {
        public MainWindow main;
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
                Arguments = $"/Online /Cleanup-Image /StartComponentCleanup /LogPath:\"{logPath}\"",
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
                    AddStatusTextBlock($"DISM exited with code {exitCode}. Error:\n{errorBuilder}");
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
                AddStatusTextBlock("Cleanup cancelled.");
            }
            catch (Exception ex)
            {
                await progressBar.Dispatcher.InvokeAsync(() =>
                {
                    progressBar.IsIndeterminate = false;
                    progressBar.Value = 0;
                });
                AddStatusTextBlock($"Error during cleanup: {ex.Message}");
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

                await CleanupWinSxSWithRealProgress(logFile, main.progressBar1, main.dismcancel.Token);
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
                    catch (System.IO.IOException ioEx)
                    {

                        const int ERROR_SHARING_VIOLATION = 0x20;
                        const int ERROR_LOCK_VIOLATION = 0x21;


                        int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;

                        if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                        {
                            main.paths.Add(file);

                        }

                    }

                }

                await main.Dispatcher.InvokeAsync(() =>
                {
                    logBlock.Text = $"Cleaned: Deep Log Scan Files - {main.formatsize(cleaned)}";
                });
                cleaned = 0;
            }

            for (int i = 1; i < main.database.Count; i++)
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
                catch (System.IO.IOException ioEx)
                {


                    const int ERROR_SHARING_VIOLATION = 0x20;
                    const int ERROR_LOCK_VIOLATION = 0x21;

                    int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;

                    if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                    {
                        if (System.IO.File.Exists(path))
                            main.paths.Add(path);
                    }

                }
                catch (Exception ex)
                {

                }

            }

            await FinalizeCleaning();
        }
        public class LockedFileGroupViewModel : INotifyPropertyChanged
        {
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

                            if (!System.IO.File.Exists(file))
                            {

                                return (true, 0L, "File did not exist.");
                            }


                            long fileSize = new FileInfo(file).Length;

                            System.IO.File.Delete(file);

                            return (true, fileSize, string.Empty);
                        }
                        catch (System.IO.IOException ioEx)
                        {

                            const int ERROR_SHARING_VIOLATION = 0x20;
                            const int ERROR_LOCK_VIOLATION = 0x21;


                            int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;

                            if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
                            {
                                main.paths.Add(file);

                                return (false, 0L, $"File is locked or in use by another process: {ioEx.Message}");
                            }
                            else
                            {

                                return (false, 0L, $"An I/O error occurred: {ioEx.Message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            return (false, 0L, $"An I/O error occurred: {ex.Message}");
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
        public class LockedFileViewModel
        {
            public string FilePath { get; set; }
            public string LockedByProcess { get; set; }
        }
        private async Task FinalizeCleaning()
        {

            string resultMessage = "";
            await cts.CancelAsync();
            long freedSpace = new DriveInfo("C:\\").AvailableFreeSpace - totalsize;
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

using MFK;
using Microsoft.VisualBasic.Logging;
using Multron_Win_Cleaner;
using MultronWinCleaner;
using Ookii.Dialogs.Wpf;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static Multron_Win_Cleaner.MainWindow;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
namespace MultronWinCleaner.Processes
{
    public class Clean
    {
        public MainWindow main;
        private int shortcut;
        private long totalsize;
        int winsxs = 0;
        private volatile string dotsText;
        private HashSet<string> checkedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> lockedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string currentItem;
        private int itemFiles;
        private long itemBytes;
        private int itemLocked;
        private int totalFiles;
        private long totalBytes;
        private int totalLocked;
        private int cleanedItems;
        private int skippedItems;
        private readonly Stopwatch cleanTimer = new Stopwatch();
        private static readonly SolidColorBrush BLUE = CreateBrush("#0078d7");
        private static readonly SolidColorBrush GREEN = CreateBrush("#00d700");


        CancellationTokenSource cts = new CancellationTokenSource();
        public Clean(MainWindow main)
        {
            this.main = main;
        }
        private static SolidColorBrush CreateBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();  
            return brush;
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
                        main.label1_Copy.Text = (dotsText ?? text) + ".";
                        main.label1_Copy.Foreground = BLUE;
                    });

                    await Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = (dotsText ?? text) + "..";
                    });

                    await Task.Delay(1000, cancellationToken);

                    await main.Dispatcher.InvokeAsync(() => {
                        main.label1_Copy.Text = (dotsText ?? text) + "...";
                    });

                    await Task.Delay(1000, cancellationToken);
                }
              
            }
            catch (TaskCanceledException)
            {

            }
        }
     
        private async Task ProcessShortcutAsync(string shortcutString)
        {
            string[] parts = shortcutString.Split('=');

            if (parts.Length >= 2)
            {
                string targetFile = parts[0];
                string arguments = parts[1];

                await RunCommandAsync(targetFile, arguments);
            }
        }
        private async Task ProcessCommandAsync(string shortcutString)
        {
            string[] parts = shortcutString.Split('=');

            if (parts.Length >= 2)
            {
                string targetFile = parts[0];
                string arguments = parts[1];

                if (targetFile.Equals("Dism.exe", StringComparison.OrdinalIgnoreCase))
                {
                    arguments = $"/c {targetFile} {arguments}";   
                    targetFile = "cmd.exe";
                }

                await RunCommandAsync(targetFile, arguments);
            }
        }
        private async Task RunCommandAsync(string fileName, string arguments)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas",
                WindowStyle = ProcessWindowStyle.Normal
            };

            try
            {
 
                await Task.Run(() =>
                {
                    using (Process process = Process.Start(startInfo))
                    {
                        if (process != null)
                        {
                            process.WaitForExit();
                        }
                    }
                });
            }
            catch (System.ComponentModel.Win32Exception)
            {
                 
                main.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Administrator permission was denied for '{arguments}'. Skipping...", "Cancelled", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
            catch (Exception ex)
            {
        
                main.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }
        public async Task run()
        {
       
            await main.Dispatcher.InvokeAsync(() =>
            {
                main.wrapPanelDirectories.Children.Clear();
                main.wrapPanelDirectories.Visibility = Visibility.Hidden;
                main.wrapPanel1.Visibility = Visibility.Hidden;
                main.dataGridGroups.Visibility = Visibility.Hidden;
                main.wrapPanelDirectories.Visibility = Visibility.Visible;
                main.ScrollViewerDirectories.Visibility = Visibility.Visible;
                main.Datagridscroll.Visibility = Visibility.Hidden;
                main.buttonStartScan.Content = "Cancel";
                main.buttonReset.Visibility = Visibility.Hidden;
                main.progressBar1.Value = 0;
                main.AnimateIcon(true);
                checkedPaths = new HashSet<string>(
                    main.viewModel.Groups.Where(g => g.allFiles != null).SelectMany(g => g.allFiles).Where(f => f.IsChecked).Select(f => f.Path),
                    StringComparer.OrdinalIgnoreCase);
            });

            cleanTimer.Restart();
            var task = ScandotsAsync("Cleaning", cts.Token);
            main.database.RemoveAll(item => item.EndsWith("=logscan"));
            int totalCount = main.database.Count;
            int cleanedCount = 0;
            totalsize = new DriveInfo("C:\\").AvailableFreeSpace;

            await UpdateStatusColor(BLUE);

            if (main.logfiles.Count > 0)
            {
                var status = await AddStatusTextBlock($"Cleaning: Found Log Files in C:\\ ({main.logfiles.Count} files)");
                int current = 0;
                StartItem("Deep Log Files");

                foreach (string logfile in main.logfiles)
                {
                    if (main.cancelclean == 2) break;

                    TryDeleteFile(logfile, "Deep Log Scan Result");
                    current++;
                    dotsText = $"Cleaning: Deep Log Files ({current} / {main.logfiles.Count})";
                    await UpdateProgress(current, main.logfiles.Count);
                }

                FinishItem();
                await main.Dispatcher.InvokeAsync(() =>
                {
                    status.Text = $"Cleaned: Found Log Files in C:\\ ({DetailText()})";
                    if (itemLocked > 0)
                        status.Foreground = Brushes.Goldenrod;
                });
            }

            for (int i = 0; i < main.database.Count; i++)
            {
                string item = main.database[i];

                if (main.cancelclean == 2) break;

                cleanedCount++;
                int iscleaned = 0;
                await UpdateProgress(cleanedCount, totalCount);

                string name = GetToken(item, 0);
                string path = GetToken(item, 1);
         

                if (name.Contains("cleanmgr.exe") && shortcut == 0)
                {
                    await AddStatusTextBlock("Running cleanmgr.exe");
                    await ProcessShortcutAsync(item);
                    shortcut = 1;
                    iscleaned = 3;
                }
                else if (name.Contains("Dism.exe"))
                {
                    string dismArguments = GetToken(item, 1);

                    if (dismArguments.Contains("RestoreHealth", StringComparison.OrdinalIgnoreCase))
                    {
                        
                        var restoreGroup = main.viewModel.Groups.FirstOrDefault(g =>
                            g.Path != null && g.Path.StartsWith("Dism Command: /Online /Cleanup-Image /RestoreHealth", StringComparison.OrdinalIgnoreCase)
                            && g.Path.Contains("recommended", StringComparison.OrdinalIgnoreCase));

                        bool shouldRun = restoreGroup != null
                                          && restoreGroup.allFiles != null
                                          && restoreGroup.allFiles.Any(f => f.IsChecked);

                        if (!shouldRun)
                        {
                            var skipBlock = await AddStatusTextBlock("Skipped: Dism.exe RestoreHealth (no corruption found or unchecked)");
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                skipBlock.Foreground = GREEN;
                            });
                        }
                        else
                        {
                            dotsText = "Cleaning: Dism.exe (Restore Health)";
                            await AddStatusTextBlock("Running Dism.exe (Restore Health)");
                            string cmdItem = $"cmd.exe=/c Dism.exe {dismArguments}";
                            await ProcessCommandAsync(cmdItem);
                            iscleaned = 3;
                        }
                    }
                    else if (winsxs == 0)
                    {
                        var cleanupGroup = main.viewModel.Groups.FirstOrDefault(g =>
                            g.Path != null && g.Path.Equals("Dism Command: /Online /Cleanup-Image /StartComponentCleanup", StringComparison.OrdinalIgnoreCase));

                        bool shouldRun = cleanupGroup != null
                                          && cleanupGroup.allFiles != null
                                          && cleanupGroup.allFiles.Any(f => f.IsChecked);

                        if (!shouldRun)
                        {
                            var skipBlock = await AddStatusTextBlock("Skipped: Dism.exe StartComponentCleanup (not scanned or unchecked)");
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                skipBlock.Foreground = GREEN;
                            });
                        }
                        else
                        {
                            dotsText = "Cleaning: Dism.exe (WinSxS)";
                            var runBlock = await AddStatusTextBlock($"Cleaning: Dism.exe {dismArguments}");
                            await ProcessCommandAsync($"cmd.exe=/c Dism.exe {dismArguments}");
                            await main.Dispatcher.InvokeAsync(() =>
                            {
                                runBlock.Text = $"Execute Done: Dism.exe {dismArguments}";
                            });
                            iscleaned = 3;
                        }
                        winsxs = 1;
                    }
                }
               
                else if (name.Contains("sfc.exe", StringComparison.OrdinalIgnoreCase))
                {
                    var sfcGroup = main.viewModel.Groups.FirstOrDefault(g =>
                        g.Path != null && g.Path.Equals("SFC Command: /scannow", StringComparison.OrdinalIgnoreCase));

                    bool shouldRun = sfcGroup != null
                                      && sfcGroup.allFiles != null
                                      && sfcGroup.allFiles.Any(f => f.IsChecked);

                    if (!shouldRun)
                    {
                        var skipBlock = await AddStatusTextBlock("Skipped: sfc.exe /scannow (no violations found or unchecked)");
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            skipBlock.Foreground = GREEN;
                        });
                    }
                    else
                    {
                        dotsText = "Cleaning: sfc.exe /scannow";
                        await AddStatusTextBlock("Running sfc.exe /scannow (this may take several minutes)");
                        await ProcessShortcutAsync("cmd.exe=/c sfc /scannow");
                        iscleaned = 3;
                    }
                }
                else if (Directory.Exists(path) || File.Exists(path))
                {
                    dotsText = $"Cleaning: {name}";
                    var statusBlock = await AddStatusTextBlock($"Cleaning: {name}: {path}");
                    StartItem(name);

                    if (File.Exists(path))
                    {
                        if (!checkedPaths.Contains(path) || main.settings.excludedfiles.Contains(path))
                        {
                            iscleaned = 0;
                        }
                        else
                        {
                            TryDeleteFile(path, name);
                            iscleaned = itemLocked > 0 ? 5 : 1;
                        }
                    }
                    else
                    {
                        var targetGroup = main.viewModel.Groups.FirstOrDefault(g => g.Path != null && g.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

                        if (targetGroup == null || targetGroup.allFiles == null || targetGroup.allFiles.Count == 0)
                        {
                            iscleaned = 2;
                        }
                        else if (targetGroup.allFiles.Any(f => f.IsChecked))
                        {
                            await CleanDirectory(path, name);
                            await DeleteEmptyDirectories(path);
                            iscleaned = itemLocked > 0 ? 6 : 1;
                        }
                    }

                    FinishItem();
                    if (iscleaned == 0)
                        skippedItems++;
                    string detail = DetailText();

                    await main.Dispatcher.InvokeAsync(() =>
                    {
                        if (iscleaned == 0)
                        {
                            statusBlock.Text = $"Ignored: {name}: {path}";
                            statusBlock.Foreground = GREEN;
                        }
                        else if (iscleaned == 2)
                        {
                            statusBlock.Text = $"Folder Empty: {name}: {path}";
                            statusBlock.Foreground = Brushes.Goldenrod;
                        }
                        else if (iscleaned == 1)
                        {
                            statusBlock.Text = $"Cleaned: {name}: {path} ({detail})";
                            statusBlock.Foreground = BLUE;
                        }
                        else if (iscleaned == 5)
                        {
                            statusBlock.Text = $"Locked: {name}: {path}";
                            statusBlock.Foreground = Brushes.Red;
                        }
                        else if (iscleaned == 6)
                        {
                            statusBlock.Text = $"Partly Cleaned: {name}: {path} ({detail})";
                            statusBlock.Foreground = Brushes.Goldenrod;
                        }
                    });
                }
            }

            cleanTimer.Stop();
            TimeSpan elapsed = cleanTimer.Elapsed;
            string summaryText = $"{(main.cancelclean == 2 ? "Cleaning canceled" : "Summary")}: {totalFiles} files deleted ({main.formatsize(totalBytes)}) from {cleanedItems} locations, {totalLocked} locked files, {skippedItems} unticked locations skipped. Took {(int)elapsed.TotalMinutes}m {elapsed.Seconds}s";
            var summaryBlock = await AddStatusTextBlock(summaryText);
            await main.Dispatcher.InvokeAsync(() =>
            {
                summaryBlock.FontWeight = FontWeights.Bold;
                summaryBlock.Foreground = totalLocked > 0 ? Brushes.Goldenrod : GREEN;
            });
            await FinalizeCleaning();
        }
        private void StartItem(string name)
        {
            currentItem = name;
            itemFiles = 0;
            itemBytes = 0;
            itemLocked = 0;
        }
        private void FinishItem()
        {
            totalFiles += itemFiles;
            totalBytes += itemBytes;
            totalLocked += itemLocked;
            if (itemFiles > 0)
                cleanedItems++;
        }
        private string DetailText()
        {
            string text = $"{itemFiles} {(itemFiles == 1 ? "file" : "files")}, {main.formatsize(itemBytes)}";
            return itemLocked > 0 ? $"{text}, {itemLocked} locked" : text;
        }
        private void TryDeleteFile(string file, string groupName)
        {
            if (!checkedPaths.Contains(file) || main.settings.excludedfiles.Contains(file))
                return;

            try
            {
                FileInfo info = new FileInfo(file);
                if (!info.Exists)
                    return;

                long length = info.Length;
                info.Delete();
                itemFiles++;
                itemBytes += length;
                if (itemFiles % 50 == 0)
                    dotsText = $"Cleaning: {currentItem} ({itemFiles} files)";
            }
            catch (Exception Ex)
            {
                if (File.Exists(file))
                {
                    itemLocked++;
                    catchlockedfile(Ex, file, groupName);
                }
                catchexception(Ex.Message + " " + Ex.StackTrace);
            }
        }
        public void catchlockedfile(Exception ex, string file, string groupName)
        {
            bool isLocked = false;

            if (ex is IOException ioEx)
            {
                int errorCode = Marshal.GetHRForException(ioEx) & 0x0000FFFF;
                if (errorCode == 0x20 || errorCode == 0x21)
                    isLocked = true;
            }
            else if (ex is UnauthorizedAccessException)
            {
                isLocked = true;
            }

            if (isLocked && lockedFiles.Add(file))
            {
                main.paths.Add(file + "=" + "0" + "=" + "Unknown Process" + "=" + groupName);
            }
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
                    if (main.cancelclean == 2) return;
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
                    if (main.cancelclean == 2) return;
                    TryDeleteFile(file, name);
                }
            }
            catch (Exception ex)
            {

            }
        }
        public async Task DeleteEmptyDirectories(string parentPath)
        {

            try
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
            catch (Exception ex)
            {
                catchexception(ex.Message + " " + ex.StackTrace);
            }
      
          
        }



        public class LockedFileViewModel
        {
            public string FilePath { get; set; }
            public string LockedByProcess { get; set; }
        }
        private async Task FinalizeCleaning()
        {
            main.AnimateIcon(false);
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
                    ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}" :   main.startupclean == 1   ? $"Startup Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}"  : $"Cleaning done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}";
            }
            else
            {
                resultMessage = main.autoclean == 1   ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}" : main.startupclean == 1   ? $"Startup Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}"    : $"Cleaning done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now}";
            }
        
            await main.Dispatcher.InvokeAsync(() =>
            {
                if (main.settings.chkShowLastLog.IsChecked == true)
                {
                    if(File.Exists(main.settings.logfilepath))
                    {
                        System.IO.File.AppendAllText(main.settings.logfilepath, resultMessage + "(last scan)" + Environment.NewLine);
                    }
                 
                }
                main.label1_Copy.Text = resultMessage;
                main.label1_Copy.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF8C00"));
                main.reset = 1;
               
                
                main.autoclean = 0;
                main.onclean = 0;
                main.buttonStartScan.Content = "Scan";
                main.buttonStartScan.IsEnabled = false;
                main.buttonReset.Visibility = Visibility.Visible;

                if (main.settings.chkEnableNotifyScan.IsChecked == true && main.Visibility == Visibility.Hidden || main.Visibility == Visibility.Collapsed || main.WindowState == WindowState.Minimized)
                {
                    Notify notify = new Notify("Clean Information", $"Your system cleaned!\r\n", $"{main.formatsize(freedSpace)}");
                    notify.Show();
                }
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
                    Foreground = BLUE,
                    FontSize = 16,
                    Margin = new Thickness(5),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Top
                };
                main.wrapPanelDirectories.Children.Add(tb);
                main.ScrollViewerDirectories.ScrollToBottom();
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

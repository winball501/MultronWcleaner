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
                        main.label1_Copy.Text = text + ".";
                        main.label1_Copy.Foreground = BLUE;
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
                    arguments = $"/k {targetFile} {arguments}";
                     
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

            await UpdateStatusColor(BLUE);

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
                    catch (Exception Ex)
                    {
                        if (File.Exists(logfile))
                        {
                            catchlockedfile(Ex, logfile, "Deep Log Scan Result");
                        }
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
                    await AddStatusTextBlock("Running Dism.exe (WinSxS Clean)");
                    string dismArguments = GetToken(item, 1);
                    string cmdKeepOpenItem = $"cmd.exe=/k Dism.exe {dismArguments}";
                    await ProcessCommandAsync(cmdKeepOpenItem);
                    iscleaned = 3;
                }
                else if (Directory.Exists(path) || File.Exists(path))
                {
                   
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
                                    try
                                    {
                                        File.Delete(path);
                                        iscleaned = 1;
                                    }
                                    catch (Exception Ex)
                                    {
                                        if (File.Exists(path))
                                        {
                                            catchlockedfile(Ex, path, name);
                                        }
                                        iscleaned = 5;
                                        catchexception(Ex.Message + " " + Ex.StackTrace);
                                    }
                                }
                            }
                        }
                        else
                        {
                            iscleaned = 4;  
                        }
                    }
                    else if (Directory.Exists(path))
                    {
                        var targetGroup = main.viewModel.Groups.FirstOrDefault(g => g.Path != null && g.Path.StartsWith(path, StringComparison.OrdinalIgnoreCase));

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
                    else
                    {
                        iscleaned = 4;
                    }

                    try
                    {
                        var statusBlock = await AddStatusTextBlock($"Cleaning: {name}");
                        await main.Dispatcher.InvokeAsync(() =>
                        {
                            if (iscleaned == 0)
                            {
                                statusBlock.Text = $"Ignored: {name}";
                                statusBlock.Foreground = GREEN;
                            }
                            else if (iscleaned == 2)
                            {
                                statusBlock.Text = $"Folder Empty: {name}";
                                statusBlock.Foreground = Brushes.Goldenrod;
                            }
                            else if (name.Contains("Dism.exe"))
                            {
                                statusBlock.Text = $"{name} Operation Done. Log files in current directory of mwc.";
                            }
                            else if (iscleaned == 1)
                            {
                                statusBlock.Text = $"Cleaned: {name}";
                                statusBlock.Foreground = BLUE;
                            }
                            else if (iscleaned == 3)
                            {
                                statusBlock.Text = $"Execute Done: {name}";
                                statusBlock.Foreground = BLUE;
                            }
                            else if (iscleaned == 4)
                            {
                                statusBlock.Text = $"Does not exist: {name}";
                                statusBlock.Foreground = Brushes.Red;
                            }
                            else if (iscleaned == 5)
                            {
                                statusBlock.Text = $"Locked: {name}";
                                statusBlock.Foreground = Brushes.Red;
                                iscleaned = 4;
                            }
                        });
                    }
                    catch (Exception Ex)
                    {
                        if (File.Exists(path))
                        {
                            catchlockedfile(Ex, path, name);
                        }
                    }
                }
            }
            await FinalizeCleaning();
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

            if (isLocked)
            {
                try
                {
                    var procs = whousef.WhoIsLocking(file);
                    if (procs != null && procs.Count > 0)
                    {
                        foreach (var proc in procs)
                            main.paths.Add(file + "=" + proc.Id + "=" + proc.ProcessName + "=" + groupName);
                    }
                    else
                    {
                        main.paths.Add(file + "=" + "0" + "=" + "Unknown Process" + "=" + groupName);
                    }
                }
                catch
                {
                    main.paths.Add(file + "=" + "0" + "=" + "Unknown Process" + "=" + groupName);
                }
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
                    catch (Exception Ex)
                    {
                        if (File.Exists(path))
                            catchlockedfile(Ex, path, name);
                        catchexception(Ex.Message + " " + Ex.StackTrace);
                    }

                }
            } catch (Exception ex)
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
                    ? $"Auto Clean done! {main.formatsize(freedSpace)} cleaned. {DateTime.Now} Locked Files Found! {main.paths.Count}"
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
                if (main.settings.chkShowLastLog.IsChecked == true)
                {
                    System.IO.File.AppendAllText(main.settings.logfilepath, resultMessage + "(last scan)" + Environment.NewLine);
                }
                main.label1_Copy.Text = resultMessage;
                main.label1_Copy.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FF8C00"));
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
                    Foreground = BLUE,
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

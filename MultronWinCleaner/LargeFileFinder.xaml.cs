using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ookii.Dialogs.Wpf;

namespace MultronWinCleaner
{
    public partial class LargeFileFinder : Window
    {
        private volatile bool isScanning = false;
        private byte cancel = 0;
        private bool sortDescending = true;
        private double previousWidth, previousHeight, previousLeft, previousTop;
        private bool isMaximized = false;
         
        private readonly List<string> excludedPaths = new();
         
        private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"
        };

        private static readonly HashSet<string> MusicExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".ogg", ".wma", ".m4a", ".alac", ".aiff", ".opus"
        };

        private static readonly HashSet<string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".tiff", ".raw", ".ico", ".heic"
        };

        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".iso", ".cab", ".bz2", ".xz", ".tgz"
        };

        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".csv", ".odt", ".ods", ".odp"
        };

        public LargeFileFinder()
        {
            InitializeComponent();

            FolderListBox.Items.Clear();
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            FolderListBox.Items.Add(userProfile);

            LargeFilesDataGrid.ItemsSource = null;

            SizeThresholdTextBox.Text = "1";
            SetDefaultUnitToGB();
        }

        private void SetDefaultUnitToGB()
        {
            foreach (ComboBoxItem item in SizeUnitComboBox.Items)
            {
                if (item.Content?.ToString() == "GB")
                {
                    SizeUnitComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
               Visibility = Visibility.Collapsed;
        }

        public class LargeFileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string FormattedSize { get; set; }
            public long FileSizeBytes { get; set; }
            public DateTime LastModified { get; set; }
            public DateTime LastAccessed { get; set; }

            public string FormattedLastModified => LastModified.ToString("dd.MM.yyyy HH:mm");
            public string FormattedLastAccessed => LastAccessed.ToString("dd.MM.yyyy HH:mm");

            public string TimeAgo
            {
                get
                {
                    TimeSpan span = DateTime.Now - LastAccessed;
                    if (span.TotalDays >= 365)
                        return $"{(int)(span.TotalDays / 365)} years ago";
                    if (span.TotalDays >= 30)
                        return $"{(int)(span.TotalDays / 30)} months ago";
                    if (span.TotalDays >= 1)
                        return $"{(int)span.TotalDays} days ago";
                    if (span.TotalHours >= 1)
                        return $"{(int)span.TotalHours} hours ago";
                    return "Recently";
                }
            }
        } 
        private bool IsPathExcluded(string targetPath)
        {
            if (string.IsNullOrWhiteSpace(targetPath) || excludedPaths.Count == 0)
                return false;

            foreach (var excluded in excludedPaths)
            {
            
                if (targetPath.Equals(excluded, StringComparison.OrdinalIgnoreCase) ||
                    targetPath.StartsWith(excluded.EndsWith(Path.DirectorySeparatorChar.ToString()) ? excluded : excluded + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private async void StartScan_Click(object sender, RoutedEventArgs e)
        {
            if (isScanning)
            {
                cancel = 1;
                ScanResultLabel.Text = "Cancelling scan...";
                StartScan.IsEnabled = false;
                return;
            }

            if (!double.TryParse(SizeThresholdTextBox.Text, out double minSize))
            {
                ScanResultLabel.Text = "Please enter a valid size threshold.";
                return;
            }

            bool applyDateFilter = EnableDateFilterCheckBox.IsChecked == true;
            int minDaysOld = 0;
            if (applyDateFilter && !int.TryParse(DaysOldTextBox.Text, out minDaysOld))
            {
                ScanResultLabel.Text = "Please enter a valid number of days for the date filter.";
                return;
            }
            string selectedDateType = (DateTypeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Last Accessed";

            bool allowVideos = ChkVideos.IsChecked == true;
            bool allowMusic = ChkMusic.IsChecked == true;
            bool allowPhotos = ChkPhotos.IsChecked == true;
            bool allowArchives = ChkArchives.IsChecked == true;
            bool allowDocuments = ChkDocuments.IsChecked == true;
            bool allowOthers = ChkOthers.IsChecked == true;

            if (!allowVideos && !allowMusic && !allowPhotos && !allowArchives && !allowDocuments && !allowOthers)
            {
                ScanResultLabel.Text = "Please select at least one file type category to scan.";
                return;
            }

            isScanning = true;
            cancel = 0;
            UpdateScanButtonUI(true);

            ScanResultLabel.Text = "Calculating...";
            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Value = 0;
            LargeFilesDataGrid.ItemsSource = null;

            string selectedUnit = (SizeUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "GB";

            if (SizeColumn != null)
            {
                SizeColumn.Header = $"Size ({selectedUnit})";
            }

            long multiplier = selectedUnit switch
            {
                "KB" => 1024L,
                "MB" => 1024L * 1024L,
                "GB" => 1024L * 1024L * 1024L,
                _ => 1024L * 1024L * 1024L
            };

            long minBytes = (long)(minSize * multiplier);

            try
            {
                var foundFiles = new List<LargeFileInfo>();
                var sw = Stopwatch.StartNew();

                var allFiles = await Task.Run(() =>
                {
                    var files = new List<string>();
                    foreach (string folder in FolderListBox.Items)
                    {
                        if (cancel == 1) break;

                        if (Directory.Exists(folder) && !IsPathExcluded(folder))
                        {
                            try
                            {
                                files.AddRange(SafeFileEnumerator(folder));
                            }
                            catch { }
                        }
                    }
                    return files;
                });

                int totalFiles = allFiles.Count;
                if (totalFiles == 0)
                {
                    ScanResultLabel.Text = "No files found in the selected folders.";
                    return;
                }

                int processed = 0;

                await Task.Run(() =>
                {
                    foreach (var file in allFiles)
                    {
                        if (cancel == 1) break;
                         
                        if (IsPathExcluded(file))
                        {
                            processed++;
                            continue;
                        }

                        try
                        {
                            var info = new FileInfo(file);

                            string ext = info.Extension;
                            bool isVideo = VideoExtensions.Contains(ext);
                            bool isMusic = MusicExtensions.Contains(ext);
                            bool isPhoto = PhotoExtensions.Contains(ext);
                            bool isArchive = ArchiveExtensions.Contains(ext);
                            bool isDoc = DocumentExtensions.Contains(ext);
                            bool isOther = !isVideo && !isMusic && !isPhoto && !isArchive && !isDoc;

                            bool matchesCategory =
                                (isVideo && allowVideos) ||
                                (isMusic && allowMusic) ||
                                (isPhoto && allowPhotos) ||
                                (isArchive && allowArchives) ||
                                (isDoc && allowDocuments) ||
                                (isOther && allowOthers);

                            if (!matchesCategory)
                            {
                                processed++;
                                continue;
                            }

                            if (info.Length >= minBytes)
                            {
                                if (applyDateFilter)
                                {
                                    DateTime checkDate = (selectedDateType == "Last Modified")
                                        ? info.LastWriteTime
                                        : info.LastAccessTime;

                                    double fileAgeInDays = (DateTime.Now - checkDate).TotalDays;

                                    if (fileAgeInDays < minDaysOld)
                                    {
                                        processed++;
                                        continue;
                                    }
                                }

                                double displaySize = selectedUnit switch
                                {
                                    "KB" => info.Length / 1024d,
                                    "MB" => info.Length / (1024d * 1024d),
                                    "GB" => info.Length / (1024d * 1024d * 1024d),
                                    _ => info.Length / (1024d * 1024d * 1024d)
                                };

                                lock (foundFiles)
                                {
                                    foundFiles.Add(new LargeFileInfo
                                    {
                                        Name = info.Name,
                                        Path = info.FullName,
                                        FormattedSize = $"{displaySize:F2} {selectedUnit}",
                                        FileSizeBytes = info.Length,
                                        LastModified = info.LastWriteTime,
                                        LastAccessed = info.LastAccessTime
                                    });
                                }
                            }
                        }
                        catch { }

                        processed++;

                        if (processed % 25 == 0 || processed == totalFiles)
                        {
                            double progress = (double)processed / totalFiles * 100;
                            TimeSpan elapsed = sw.Elapsed;
                            TimeSpan estimatedTotal = TimeSpan.FromMilliseconds(elapsed.TotalMilliseconds * totalFiles / processed);
                            TimeSpan remaining = estimatedTotal - elapsed;

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ScanProgressBar.Value = progress;
                                ScanResultLabel.Text = $"Scanning... {progress:F1}% - ETA: {remaining:mm\\:ss}";
                            });
                        }
                    }
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var sortedList = foundFiles.OrderByDescending(f => f.FileSizeBytes).ToList();
                    LargeFilesDataGrid.ItemsSource = sortedList;

                    if (cancel == 1)
                    {
                        ScanResultLabel.Text = $"{sortedList.Count} large files found. Scan Cancelled!";
                    }
                    else
                    {
                        ScanResultLabel.Text = $"{sortedList.Count} large files found.";
                    }
                });
            }
            catch (Exception ex)
            {
                ScanResultLabel.Text = $"An error occurred: {ex.Message}";
            }
            finally
            {
                isScanning = false;
                UpdateScanButtonUI(false);
                ScanProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateScanButtonUI(bool scanning)
        {
            StartScan.IsEnabled = true;
            if (StartScan.Content is TextBlock tb)
            {
                tb.Text = scanning ? "CANCEL" : "SCAN";
            }
            else
            {
                StartScan.Content = scanning ? "CANCEL" : "SCAN";
            }
        }

        private IEnumerable<string> SafeFileEnumerator(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                if (cancel == 1) break;
                string currentDir = pending.Pop();
                 
                if (IsPathExcluded(currentDir)) continue;

                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch { continue; }

                foreach (var dir in subDirs)
                {
                    if (cancel == 1) break;
                    if (!IsPathExcluded(dir))
                    {
                        pending.Push(dir);
                    }
                }

                string[] files;
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch { continue; }

                foreach (var file in files)
                {
                    if (cancel == 1) break;
                    if (!IsPathExcluded(file))
                    {
                        yield return file;
                    }
                }
            }
        }
         
        private void AddFolderToExclusions_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to exclude",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                AddExclusionPath(dialog.SelectedPath);
            }
        }

        private void AddExclusionPath(string path)
        {
            if (!excludedPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                excludedPaths.Add(path);

            
                var listBox = (FindName("ExcludedListBox") ??
                               FindName("ExcludedFolderListBox") ??
                               FindName("ExcludedFolderList")) as ListBox;

                if (listBox != null && !listBox.Items.Contains(path))
                {
                    listBox.Items.Add(path);
                }
            }
        }

        private void RemoveExclusion_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("ExcludedListBox") is ListBox excludedListBox && excludedListBox.SelectedItem != null)
            {
                string pathToRemove = excludedListBox.SelectedItem.ToString();
                excludedPaths.RemoveAll(p => p.Equals(pathToRemove, StringComparison.OrdinalIgnoreCase));
                excludedListBox.Items.Remove(excludedListBox.SelectedItem);
            }
        }
 
        private void ExcludeSelectedFile_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile && !string.IsNullOrEmpty(selectedFile.Path))
            {
                AddExclusionPath(selectedFile.Path);
                MessageBox.Show($"File added to exclusions:\n{selectedFile.Path}", "Excluded", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ExcludeSelectedFolder_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile && !string.IsNullOrEmpty(selectedFile.Path))
            {
                string directory = Path.GetDirectoryName(selectedFile.Path);
                if (!string.IsNullOrEmpty(directory))
                {
                    AddExclusionPath(directory);
                    MessageBox.Show($"Folder added to exclusions:\n{directory}", "Excluded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void LargeFilesDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column == SizeColumn)
            {
                e.Handled = true;
                var list = LargeFilesDataGrid.ItemsSource as List<LargeFileInfo>;
                if (list == null) return;

                list = sortDescending
                    ? list.OrderByDescending(f => f.FileSizeBytes).ToList()
                    : list.OrderBy(f => f.FileSizeBytes).ToList();

                sortDescending = !sortDescending;

                LargeFilesDataGrid.ItemsSource = null;
                LargeFilesDataGrid.ItemsSource = list;
            }
        }

        private void SelectAllCategories_Click(object sender, RoutedEventArgs e)
        {
            SetCategoriesState(true);
        }

        private void DeselectAllCategories_Click(object sender, RoutedEventArgs e)
        {
            SetCategoriesState(false);
        }

        private void SetCategoriesState(bool isChecked)
        {
            ChkVideos.IsChecked = isChecked;
            ChkMusic.IsChecked = isChecked;
            ChkPhotos.IsChecked = isChecked;
            ChkArchives.IsChecked = isChecked;
            ChkDocuments.IsChecked = isChecked;
            ChkOthers.IsChecked = isChecked;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile && !string.IsNullOrEmpty(selectedFile.Path))
            {
                try
                {
                    if (File.Exists(selectedFile.Path))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = selectedFile.Path,
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        MessageBox.Show("File no longer exists.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile && !string.IsNullOrEmpty(selectedFile.Path))
            {
                try
                {
                    if (File.Exists(selectedFile.Path))
                    {
                        string argument = "/select, \"" + selectedFile.Path + "\"";
                        Process.Start("explorer.exe", argument);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyFilePath_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile && !string.IsNullOrEmpty(selectedFile.Path))
            {
                Clipboard.SetText(selectedFile.Path);
            }
        }
 

        private void RemoveSelectedFolder_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.SelectedItem != null)
            {
                FolderListBox.Items.Remove(FolderListBox.SelectedItem);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder",
                UseDescriptionForTitle = true
            };

            bool? result = dialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                if (!FolderListBox.Items.Contains(dialog.SelectedPath))
                {
                    FolderListBox.Items.Add(dialog.SelectedPath);
                }
            }
        }

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            LargeFilesDataGrid.ItemsSource = null;
            ScanResultLabel.Text = "List Cleared!";
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void AddExcludedFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to exclude",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                AddExclusionPath(dialog.SelectedPath);
            }
        }

        private void RemoveSelectedExcludedFolder_Click(object sender, RoutedEventArgs e)
        {
            var listBox = (FindName("ExcludedListBox") ?? FindName("ExcludedFolderListBox")) as ListBox;
            if (listBox?.SelectedItem != null)
            {
                string pathToRemove = listBox.SelectedItem.ToString();
                excludedPaths.RemoveAll(p => p.Equals(pathToRemove, StringComparison.OrdinalIgnoreCase));
                listBox.Items.Remove(listBox.SelectedItem);
            }
        }
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
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using Ookii.Dialogs.Wpf;

namespace MultronWinCleaner
{
    public partial class Duplicate_File_Finder : Window
    {
        public ObservableCollection<DuplicateFile> DuplicateFiles { get; } = new ObservableCollection<DuplicateFile>();
        private List<DuplicateFileModel> allDuplicates = new List<DuplicateFileModel>();
        private HashSet<string> foundDuplicatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string duplicatesFilePath = Path.Combine(Environment.CurrentDirectory, "duplicates.json");
        private ScrollViewer dataGridScrollViewer;
        private const int PageSize = 100;
        private int currentPage = 0;

        public Duplicate_File_Finder()
        {
            InitializeComponent();
            DuplicatesDataGrid.ItemsSource = DuplicateFiles;
            DuplicatesDataGrid.Loaded += DuplicatesDataGrid_Loaded;
            LoadExistingDuplicates();
        }

        private class DuplicateFileModel
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public long FileSize { get; set; }
            public string Hash { get; set; }
        }

        public class DuplicateFile
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public long FileSize { get; set; }
            public string Hash { get; set; }
        }

        private async void LoadExistingDuplicates()
        {
            if (!File.Exists(duplicatesFilePath)) return;

            var json = await File.ReadAllTextAsync(duplicatesFilePath);
            allDuplicates = JsonConvert.DeserializeObject<List<DuplicateFileModel>>(json) ?? new List<DuplicateFileModel>();

            DuplicateFiles.Clear();
            foundDuplicatePaths.Clear();
            currentPage = 0;
            LoadNextPage();
            SetupGrouping();
        }

        private void DuplicatesDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridScrollViewer = GetDescendantByType(DuplicatesDataGrid, typeof(ScrollViewer)) as ScrollViewer;
            if (dataGridScrollViewer != null)
            {
                dataGridScrollViewer.ScrollChanged += DataGridScrollViewer_ScrollChanged;
            }
        }

        private void DataGridScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 50)  
            {
                LoadNextPage();
            }
        }

        private static Visual GetDescendantByType(Visual element, Type type)
        {
            if (element == null) return null;
            if (element.GetType() == type) return element;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i) as Visual;
                var found = GetDescendantByType(child, type);
                if (found != null) return found;
            }
            return null;
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.Items.Count == 0)
            {
                MessageBox.Show("Please select at least one folder.");
                return;
            }

            DuplicateFiles.Clear();
            allDuplicates.Clear();
            foundDuplicatePaths.Clear();
            ScanButton.IsEnabled = false;
            StatusText.Text = "Scanning...";

            var hashMap = new ConcurrentDictionary<string, string>();
            var newDuplicates = new ConcurrentBag<DuplicateFileModel>();
            var allFiles = new List<string>();

            foreach (string dir in FolderListBox.Items)
            {
                allFiles.AddRange(SafeEnumerateFiles(dir, "*.*"));
            }

            var totalFiles = allFiles.Count;
            int processedCount = 0;
            var lastUpdateTime = DateTime.MinValue;
            var updateInterval = TimeSpan.FromMilliseconds(500);

            await Task.Run(() =>
            {
                Parallel.ForEach(Partitioner.Create(allFiles, true), file =>
                {
                    try
                    {
                        using var stream = File.OpenRead(file);
                        using var md5 = MD5.Create();
                        var hashBytes = md5.ComputeHash(stream);
                        var hash = BitConverter.ToString(hashBytes);

                        var fileInfo = new FileInfo(file);
                        var fileModel = new DuplicateFileModel
                        {
                            FileName = fileInfo.Name,
                            FilePath = file,
                            FileSize = fileInfo.Length,
                            Hash = hash
                        };

                        if (hashMap.TryGetValue(hash, out var original))
                        { 
                            if (foundDuplicatePaths.Add(original))
                            {
                                var origInfo = new FileInfo(original);
                                newDuplicates.Add(new DuplicateFileModel
                                {
                                    FileName = origInfo.Name,
                                    FilePath = original,
                                    FileSize = origInfo.Length,
                                    Hash = hash
                                });
                            }
                             
                            if (foundDuplicatePaths.Add(file))
                            {
                                newDuplicates.Add(fileModel);
                            }
                        }
                        else
                        {
                            hashMap[hash] = file;
                        }
                    }
                    catch { }

                    Interlocked.Increment(ref processedCount);
                    if ((DateTime.Now - lastUpdateTime) > updateInterval)
                    {
                        lastUpdateTime = DateTime.Now;
                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Text = $"Scanning... {processedCount}/{totalFiles} files\n" +
                                              $"Found {newDuplicates.Count} duplicate files";
                        });
                    }
                });
            });

             
            allDuplicates = newDuplicates.OrderBy(d => d.FileName).ToList();

           
            try
            {
                var json = JsonConvert.SerializeObject(allDuplicates, Formatting.Indented);
                await File.WriteAllTextAsync(duplicatesFilePath, json);
            }
            catch { }

           
            DuplicateFiles.Clear();
            foundDuplicatePaths.Clear();
            currentPage = 0;
            LoadNextPage();
            SetupGrouping();
            ScanButton.IsEnabled = true;
            StatusText.Text = $"Scan Complete. \n" +
                $"Found {allDuplicates.Count} duplicate files.";
        }

        private void LoadNextPage()
        {
            int start = currentPage * PageSize;
            if (start >= allDuplicates.Count) return;

            var nextItems = allDuplicates.Skip(start).Take(PageSize);
            foreach (var d in nextItems)
            {
                if (foundDuplicatePaths.Add(d.FilePath))
                {
                    DuplicateFiles.Add(new DuplicateFile
                    {
                        FileName = d.FileName,
                        FilePath = d.FilePath,
                        FileSize = d.FileSize,
                        Hash = d.Hash
                    });
                }
            }

            currentPage++;
        }

        private void SetupGrouping()
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(DuplicateFiles);
            if (view != null)
            {
                view.GroupDescriptions.Clear();
                view.GroupDescriptions.Add(new PropertyGroupDescription("Hash"));
            }
        }

        private IEnumerable<string> SafeEnumerateFiles(string root, string pattern)
        {
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var path = stack.Pop();

                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(path, pattern);
                }
                catch { }

                foreach (var file in files)
                    yield return file;

                string[] dirs = Array.Empty<string>();
                try
                {
                    dirs = Directory.GetDirectories(path);
                }
                catch { }

                foreach (var dir in dirs)
                    stack.Push(dir);
            }
        }
        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (DuplicatesDataGrid.SelectedItem is DuplicateFile sel)
            {
                try
                {
                    File.Delete(sel.FilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file: {ex.Message}");
                    return;
                }
                DuplicateFiles.Remove(sel);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (DuplicatesDataGrid.SelectedItem is DuplicateFile sel)
            {
               
                string argument = "/select, \"" + sel.FilePath + "\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog { Description = "Select folder" };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                FolderListBox.Items.Add(dlg.SelectedPath);
        }

        private void RemoveFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.SelectedItem is string s) FolderListBox.Items.Remove(s);
        }

     
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private bool isMax = false;
        private double prevW, prevH, prevX, prevY;

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            DuplicateFiles.Clear();
            allDuplicates.Clear();
            foundDuplicatePaths.Clear();
            currentPage = 0;
            StatusText.Text = "List cleared.";
            System.IO.File.Delete(duplicatesFilePath);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.SelectedItem != null)
                FolderListBox.Items.Remove(FolderListBox.SelectedItem);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isMax)
            {
                prevW = Width; prevH = Height; prevX = Left; prevY = Top;
                Left = SystemParameters.WorkArea.Left; Top = SystemParameters.WorkArea.Top;
                Width = SystemParameters.WorkArea.Width; Height = SystemParameters.WorkArea.Height;
            }
            else
            {
                Width = prevW; Height = prevH; Left = prevX; Top = prevY;
            }
            isMax = !isMax;
        }

        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}

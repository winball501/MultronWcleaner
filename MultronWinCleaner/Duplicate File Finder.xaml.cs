using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace MultronWinCleaner
{
    public class DuplicateFileModel
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string Hash { get; set; }
    }

    public class FolderNodeModel : INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public ObservableCollection<object> Children { get; set; } = new ObservableCollection<object>();

        public IEnumerable<FolderNodeModel> SubFolders => Children.OfType<FolderNodeModel>();
        public IEnumerable<FileNodeModel> Files => Children.OfType<FileNodeModel>();

        public int TotalDuplicateCount => Files.Count() + SubFolders.Sum(s => s.TotalDuplicateCount);

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class FileNodeModel : INotifyPropertyChanged
    {
        private string _fileName = string.Empty;
        private string _filePath = string.Empty;
        private string _fileIcon = "📄";
        private string _formattedSize = string.Empty;
        private bool _isSelectedForDeletion;
        private bool _isHashVisible;

        public string FileName { get => _fileName; set { if (_fileName != value) { _fileName = value; OnPropertyChanged(nameof(FileName)); } } }
        public string FilePath { get => _filePath; set { if (_filePath != value) { _filePath = value; OnPropertyChanged(nameof(FilePath)); } } }
        public string FileIcon { get => _fileIcon; set { if (_fileIcon != value) { _fileIcon = value; OnPropertyChanged(nameof(FileIcon)); } } }
        public string FormattedSize { get => _formattedSize; set { if (_formattedSize != value) { _formattedSize = value; OnPropertyChanged(nameof(FormattedSize)); } } }
        public bool IsSelectedForDeletion { get => _isSelectedForDeletion; set { if (_isSelectedForDeletion != value) { _isSelectedForDeletion = value; OnPropertyChanged(nameof(IsSelectedForDeletion)); } } }
        public bool IsHashVisible { get => _isHashVisible; set { if (_isHashVisible != value) { _isHashVisible = value; OnPropertyChanged(nameof(IsHashVisible)); } } }

        public string Hash { get; set; }
        public long FileSize { get; set; }

        public List<string> MatchedFilePaths { get; } = new List<string>();

        public int MatchCount => MatchedFilePaths.Count;
        public bool HasMatches => MatchCount > 0;

        public string MatchSummary => MatchCount switch
        {
            0 => string.Empty,
            1 => $"1 match found — {Path.GetFileName(MatchedFilePaths[0])}",
            _ => $"{MatchCount} matches found — view"
        };

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }

    public partial class Duplicate_File_Finder : Window
    {
        public ObservableCollection<FolderNodeModel> ExplorerTree { get; set; } = new ObservableCollection<FolderNodeModel>();
        private List<DuplicateFileModel> allDuplicates = new List<DuplicateFileModel>();

        private Dictionary<string, List<FileNodeModel>> _duplicateGroupIndex = new Dictionary<string, List<FileNodeModel>>(StringComparer.OrdinalIgnoreCase);

        private string duplicatesFilePath = Path.Combine(Environment.CurrentDirectory, "duplicates.json");

        private bool _isScanning = false;
        private bool _isScanned = false;
        private CancellationTokenSource _cts;

        public Duplicate_File_Finder()
        {
            InitializeComponent();

            DuplicatesTreeView.ItemsSource = ExplorerTree;
            this.Loaded += Window_Loaded;

            SetSearchPlaceholder();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(userProfile) && FolderListBox.Items.Count == 0)
            {
                FolderListBox.Items.Add(userProfile);
            }

            await LoadDuplicatesDataAsync();
        }

        #region Centralized Data Management (Save & Load)

        private async Task SaveDuplicatesDataAsync()
        {
            try
            {
       
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(allDuplicates, options);
                await File.WriteAllTextAsync(duplicatesFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save duplicates.json data:\n{ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadDuplicatesDataAsync()
        {
            if (!File.Exists(duplicatesFilePath))
            {
                StatusText.Text = "Ready to scan target directories.";
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;

            try
            {
                List<DuplicateFileModel> loaded = await Task.Run(() =>
                {
                    using (var stream = new FileStream(duplicatesFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        return JsonSerializer.Deserialize<List<DuplicateFileModel>>(
                            stream,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                });

                if (loaded == null || loaded.Count == 0) return;

                allDuplicates = loaded;

                bool isHashVisible = ShowHashCheckBox?.IsChecked == true;

                var (newRoots, groupIndex) = await Task.Run(() => BuildFolderTreeNodes(allDuplicates, isHashVisible));
                _duplicateGroupIndex = groupIndex;

                DuplicatesTreeView.ItemsSource = null;
                ExplorerTree.Clear();

                foreach (var root in newRoots)
                {
                    ExplorerTree.Add(root);
                }

                DuplicatesTreeView.ItemsSource = ExplorerTree;

                StatusText.Text = $"Successfully loaded {allDuplicates.Count} duplicate files from previous scan.";

                await CalculateTotalDuplicateSizeAsync();

                if (allDuplicates.Count > 0)
                {
                    _isScanned = true;
                    MainActionButton.Content = "CLEAN";
                    MainActionButton.Background = new SolidColorBrush(Colors.Crimson);
                }
            }
            catch
            {
                StatusText.Text = "Failed to load previous scan data.";
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private async void MainActionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isScanning)
            {
                _cts?.Cancel();
                MainActionButton.IsEnabled = false;
                StatusText.Text = "Canceling scan...";
                return;
            }

            if (!_isScanned)
            {
                if (FolderListBox.Items.Count == 0)
                {
                    MessageBox.Show("Please select at least one folder.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _isScanning = true;
                MainActionButton.Content = "Cancel";
                TotalSizeText.Visibility = Visibility.Collapsed;
                if (SearchCountText != null) SearchCountText.Text = "";

                await RunScanAsync();

                _isScanning = false;
                MainActionButton.IsEnabled = true;

                if (_cts != null && _cts.IsCancellationRequested)
                {
                    StatusText.Text = $"Scan Canceled. Found {allDuplicates.Count} duplicate files.";
                    if (allDuplicates.Count == 0) ResetToScanState();
                }
                else if (allDuplicates.Count > 0)
                {
                    _isScanned = true;
                    MainActionButton.Content = "CLEAN";
                    MainActionButton.Background = new SolidColorBrush(Colors.Crimson);
                }
                else
                {
                    // HİÇBİR ŞEY BULUNAMADIĞI DURUM
                    StatusText.Text = "Scan Complete. No duplicate files found.";
                    MessageBox.Show("Scan complete. No duplicate files were found in the selected directories.", "Scan Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    ResetToScanState();
                }
            }
            else
            {
                CleanDuplicates();
            }
        }

        private async Task RunScanAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            await Dispatcher.InvokeAsync(() =>
            {
                allDuplicates.Clear();
                ExplorerTree.Clear();
                if (FilesScannedText != null) FilesScannedText.Text = "";
                StatusText.Text = "Scanning directories...";
            });

            try
            {
                foreach (var item in FolderListBox.Items)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    string folder = item.ToString();
                    await Scan(folder);
                }

                await SaveDuplicatesDataAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    BuildFolderTree(allDuplicates);
                    CalculateTotalDuplicateSizeAsync();

                    if (allDuplicates.Count > 0)
                    {
                        StatusText.Text = _cts.IsCancellationRequested
                            ? $"Scan Canceled. Found {allDuplicates.Count} duplicate files."
                            : $"Scan Complete. Found {allDuplicates.Count} duplicate files.";
                    }
                });
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() => StatusText.Text = "Scan Canceled.");
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => StatusText.Text = $"Scan Error: {ex.Message}");
            }
        }

        private async Task Scan(string rootFolderPath)
        {
            var token = _cts.Token;
            var dupResults = new ConcurrentBag<DuplicateFileModel>();

            int scannedCount = 0;
            int hashedCount = 0;
            long lastUpdateTicks = 0;
            const int updateIntervalMs = 200;

            long minSizeBytes = 0;
            if (MinSizeTextBox != null && long.TryParse(MinSizeTextBox.Text, out long minKb))
                minSizeBytes = minKb * 1024;

            // 1. ExtensionTextBox.Text içeriğini dinamik olarak ayrıştır
            var selectedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool allowAllExtensions = false;

            string filterText = ExtensionTextBox?.Text?.Trim() ?? "*.*";

            if (string.IsNullOrEmpty(filterText) || filterText == "*.*" || filterText == "*")
            {
                allowAllExtensions = true;
            }
            else
            { 
                var parts = filterText.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    string cleanExt = part.Trim();
                    if (cleanExt == "*.*" || cleanExt == "*")
                    {
                        allowAllExtensions = true;
                        break;
                    } 
                    cleanExt = cleanExt.Replace("*", "");
                    if (!cleanExt.StartsWith("."))
                    {
                        cleanExt = "." + cleanExt;
                    }

                    if (cleanExt.Length > 1)
                    {
                        selectedExtensions.Add(cleanExt);
                    }
                }
                 
                if (selectedExtensions.Count == 0)
                {
                    allowAllExtensions = true;
                }
            }

            bool checkQualities = CheckQualityCheckBox?.IsChecked == true;

            void MaybeReportStatus(string currentFile, string phase)
            {
                var now = Environment.TickCount64;
                if (now - Interlocked.Read(ref lastUpdateTicks) < updateIntervalMs) return;
                Interlocked.Exchange(ref lastUpdateTicks, now);

                Dispatcher.BeginInvoke(() =>
                {
                    StatusText.Text = $"{phase}... {scannedCount} files scanned, {allDuplicates.Count + dupResults.Count} duplicate files found.";
                    if (FilesScannedText != null) FilesScannedText.Text = currentFile;
                });
            }

            try
            {
                await Task.Run(() =>
                {
                    IEnumerable<string> EnumerateAllFiles(string root)
                    {
                        var stack = new Stack<string>();
                        if (Directory.Exists(root)) stack.Push(root);

                        while (stack.Count > 0)
                        {
                            token.ThrowIfCancellationRequested();
                            var dir = stack.Pop();

                            string[] subDirs = Array.Empty<string>();
                            string[] filesHere = Array.Empty<string>();
                            try
                            {
                                filesHere = Directory.GetFiles(dir, "*.*");
                                subDirs = Directory.GetDirectories(dir);
                            }
                            catch { }

                            foreach (var d in subDirs) stack.Push(d);
                            foreach (var f in filesHere) yield return f;
                        }
                    }

                    var sizeGroups = new ConcurrentDictionary<long, ConcurrentBag<string>>();
                    var mediaNameGroups = new ConcurrentDictionary<string, ConcurrentBag<string>>();

                    Parallel.ForEach(
                        EnumerateAllFiles(rootFolderPath),
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token },
                        file =>
                        {
                            string ext = Path.GetExtension(file);
                             
                            if (!allowAllExtensions && (string.IsNullOrEmpty(ext) || !selectedExtensions.Contains(ext)))
                            {
                                return;
                            }

                            long size;
                            try { size = new FileInfo(file).Length; }
                            catch { return; }

                            if (size >= minSizeBytes)
                            {
                                string lowerExt = ext.ToLower();
                                bool isMedia = lowerExt == ".mp4" || lowerExt == ".mkv" || lowerExt == ".avi" ||
                                               lowerExt == ".mov" || lowerExt == ".jpg" || lowerExt == ".png" ||
                                               lowerExt == ".jpeg" || lowerExt == ".webp";

                                if (checkQualities && isMedia)
                                {
                                    string baseName = Path.GetFileNameWithoutExtension(file).ToLower();
                                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\b(1080p|720p|1440p|4k|8k|480p|360p|copy)\b", "");
                                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\(\d+\)", "");
                                    baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"\s+", " ").Trim(' ', '-', '_');

                                    mediaNameGroups.GetOrAdd(baseName, _ => new ConcurrentBag<string>()).Add(file);
                                }
                                else
                                {
                                    sizeGroups.GetOrAdd(size, _ => new ConcurrentBag<string>()).Add(file);
                                }
                            }
                            Interlocked.Increment(ref scannedCount);
                            MaybeReportStatus(file, "Scanning files");
                        });

                    var candidateGroups = sizeGroups.Where(g => g.Value.Count > 1).ToList();

                    Parallel.ForEach(
                        candidateGroups,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = token },
                        group =>
                        {
                            var filesInGroup = group.Value.ToList();
                            long exactSize = group.Key;
                            var hashDict = new Dictionary<string, List<string>>();

                            foreach (var file in filesInGroup)
                            {
                                if (token.IsCancellationRequested) return;

                                string hash;
                                try
                                {
                                    using var stream = new FileStream(
                                        file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                                        bufferSize: 1024 * 1024, FileOptions.SequentialScan);

                                    hash = Convert.ToHexString(SHA256.HashData(stream));
                                }
                                catch { continue; }

                                if (!hashDict.ContainsKey(hash))
                                {
                                    hashDict[hash] = new List<string>();
                                }
                                hashDict[hash].Add(file);

                                Interlocked.Increment(ref hashedCount);
                                MaybeReportStatus(file, "Hashing & Verifying");
                            }

                            foreach (var hashGroup in hashDict.Where(hg => hg.Value.Count > 1))
                            {
                                foreach (var exactDuplicate in hashGroup.Value)
                                {
                                    dupResults.Add(new DuplicateFileModel
                                    {
                                        FileName = Path.GetFileName(exactDuplicate),
                                        FilePath = exactDuplicate,
                                        FileSize = exactSize,
                                        Hash = hashGroup.Key
                                    });
                                }
                            }
                        });

                    if (checkQualities && !token.IsCancellationRequested)
                    {
                        var fuzzyCandidates = mediaNameGroups.Where(g => g.Value.Count > 1).ToList();
                        foreach (var group in fuzzyCandidates)
                        {
                            string displayHash = "Similar Media: " + group.Key.ToUpper();

                            foreach (var file in group.Value)
                            {
                                long fSize = 0;
                                try { fSize = new FileInfo(file).Length; } catch { }

                                dupResults.Add(new DuplicateFileModel
                                {
                                    FileName = Path.GetFileName(file),
                                    FilePath = file,
                                    FileSize = fSize,
                                    Hash = displayHash
                                });
                            }
                        }
                    }

                }, token);

                var finalList = dupResults.ToList();
                await Dispatcher.InvokeAsync(() =>
                {
                    foreach (var item in finalList)
                    {
                        if (!allDuplicates.Any(d => d.FilePath.Equals(item.FilePath, StringComparison.OrdinalIgnoreCase)))
                        {
                            allDuplicates.Add(item);
                        }
                    }
                    if (FilesScannedText != null) FilesScannedText.Text = "";
                });
            }
            catch (OperationCanceledException) { }
        }
        private void ExtensionCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
       
            if (ExtensionTextBox == null) return;

            List<string> selectedExtensions = new List<string>();

            if (PngCheckBox?.IsChecked == true) selectedExtensions.Add("*.png");
            if (JpegCheckBox?.IsChecked == true) selectedExtensions.Add("*.jpeg");
            if (JpgCheckBox?.IsChecked == true) selectedExtensions.Add("*.jpg");
            if (WebpCheckBox?.IsChecked == true) selectedExtensions.Add("*.webp");
            if (Mp4CheckBox?.IsChecked == true) selectedExtensions.Add("*.mp4");
            if (AviCheckBox?.IsChecked == true) selectedExtensions.Add("*.avi");
            if (MkvCheckBox?.IsChecked == true) selectedExtensions.Add("*.mkv");
            if (MovCheckBox?.IsChecked == true) selectedExtensions.Add("*.mov");

            if (selectedExtensions.Count > 0)
            {
                ExtensionTextBox.Text = string.Join(", ", selectedExtensions);
            }
            else
            {
                ExtensionTextBox.Text = "*.*";
            }
        }

        private void ExtensionPresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ExtensionTextBox == null || ExtensionPresetComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)ExtensionPresetComboBox.SelectedItem;
            string content = selectedItem.Content.ToString();

            if (content.Contains("All Files"))
            {
                ExtensionTextBox.Text = "*.*";
            }
            else if (content.Contains("Images Only"))
            {
                ExtensionTextBox.Text = "*.png, *.jpg, *.jpeg, *.webp, *.bmp";
            }
            else if (content.Contains("Videos Only"))
            {
                ExtensionTextBox.Text = "*.mp4, *.avi, *.mkv, *.mov, *.wmv";
            }
            else if (content.Contains("Custom"))
            {
                ExtensionCheckBox_CheckedChanged(null, null);
            }
        }
        private void BuildFolderTree(List<DuplicateFileModel> duplicates)
        {
            var (nodes, groupIndex) = BuildFolderTreeNodes(duplicates, ShowHashCheckBox?.IsChecked == true);
            _duplicateGroupIndex = groupIndex;

            ExplorerTree.Clear();
            foreach (var root in nodes)
            {
                ExplorerTree.Add(root);
            }

            DuplicatesTreeView.ItemsSource = null;
            DuplicatesTreeView.ItemsSource = ExplorerTree;
        }

        private (List<FolderNodeModel> Roots, Dictionary<string, List<FileNodeModel>> GroupIndex) BuildFolderTreeNodes(
            List<DuplicateFileModel> duplicates, bool isHashVisible)
        {
            var rootNodes = new List<FolderNodeModel>();
            var folderLookup = new Dictionary<string, FolderNodeModel>(StringComparer.OrdinalIgnoreCase);
            var groupIndex = new Dictionary<string, List<FileNodeModel>>(StringComparer.OrdinalIgnoreCase);

            var groupedByHash = duplicates.GroupBy(d => d.Hash);
            var fileCheckStates = new Dictionary<string, bool>();
            var siblingPathsByHash = new Dictionary<string, List<string>>();

            foreach (var group in groupedByHash)
            {
                var orderedFiles = group.OrderBy(f => f.FilePath).ToList();
                siblingPathsByHash[group.Key] = orderedFiles.Select(f => f.FilePath).ToList();

                bool isFirst = true;
                foreach (var file in orderedFiles)
                {
                    fileCheckStates[file.FilePath] = !isFirst;
                    isFirst = false;
                }
            }

            foreach (var file in duplicates)
            {
                string directory = Path.GetDirectoryName(file.FilePath);
                if (string.IsNullOrEmpty(directory)) continue;

                string[] pathParts = directory.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

                FolderNodeModel currentNode = null;
                string currentPath = "";

                for (int i = 0; i < pathParts.Length; i++)
                {
                    string part = pathParts[i];
                    currentPath = (i == 0) ? part + Path.DirectorySeparatorChar.ToString() : Path.Combine(currentPath, part);

                    if (!folderLookup.TryGetValue(currentPath, out FolderNodeModel node))
                    {
                        node = new FolderNodeModel { Name = part, FullPath = currentPath };
                        folderLookup[currentPath] = node;

                        if (currentNode == null)
                        {
                            rootNodes.Add(node);
                        }
                        else
                        {
                            currentNode.Children.Add(node);
                        }
                    }
                    currentNode = node;
                }

                if (currentNode != null)
                {
                    var fileNode = new FileNodeModel
                    {
                        FileName = file.FileName,
                        FilePath = file.FilePath,
                        FileSize = file.FileSize,
                        Hash = file.Hash,
                        FileIcon = GetFileIcon(file.FileName),
                        FormattedSize = FormatSize(file.FileSize),
                        IsSelectedForDeletion = fileCheckStates.GetValueOrDefault(file.FilePath, true),
                        IsHashVisible = isHashVisible
                    };

                    if (!string.IsNullOrEmpty(file.Hash) && siblingPathsByHash.TryGetValue(file.Hash, out var siblingPaths))
                    {
                        foreach (var p in siblingPaths)
                        {
                            if (!string.Equals(p, file.FilePath, StringComparison.OrdinalIgnoreCase))
                                fileNode.MatchedFilePaths.Add(p);
                        }
                    }

                    currentNode.Children.Add(fileNode);

                    if (!string.IsNullOrEmpty(file.Hash))
                    {
                        if (!groupIndex.TryGetValue(file.Hash, out var list))
                        {
                            list = new List<FileNodeModel>();
                            groupIndex[file.Hash] = list;
                        }
                        list.Add(fileNode);
                    }
                }
            }

            return (rootNodes, groupIndex);
        }

        private string GetFileIcon(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "🖼️",
                ".mp4" or ".avi" or ".mkv" or ".mov" => "🎥",
                ".mp3" or ".wav" or ".flac" => "🎵",
                ".zip" or ".rar" or ".7z" => "📦",
                ".exe" or ".msi" => "⚙️",
                ".txt" or ".doc" or ".docx" or ".pdf" => "📄",
                _ => "📄"
            };
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }

        private async Task CalculateTotalDuplicateSizeAsync()
        {
            long totalRecoverableBytes = await Task.Run(() =>
            {
                long bytes = 0;
                var groups = allDuplicates.GroupBy(d => d.Hash);

                foreach (var group in groups)
                {
                    var items = group.ToList();
                    if (items.Count > 1)
                    {
                        bytes += items.Skip(1).Sum(f => f.FileSize);
                    }
                }

                return bytes;
            });

            if (totalRecoverableBytes > 0)
            {
                TotalSizeText.Text = $"Recoverable Space: {FormatSize(totalRecoverableBytes)}";
                TotalSizeText.Visibility = Visibility.Visible;
            }
            else
            {
                TotalSizeText.Visibility = Visibility.Collapsed;
            }
        }

        private async void CleanDuplicates()
        {
            MessageBoxResult result = MessageBox.Show(
                "All CHECKED files in the list will be permanently deleted. Are you sure?",
                "Confirm Cleanup", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var filesToDelete = GetCheckedFiles(ExplorerTree).ToList();

                int totalFiles = filesToDelete.Count;

                if (totalFiles == 0)
                {
                    MessageBox.Show("No files were selected for deletion.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                int deletedCount = 0;
                long freedBytes = 0;

                var failedFilesList = new List<FileNodeModel>();

                MainActionButton.IsEnabled = false;

                var progress = new Progress<string>(statusMessage =>
                {
                    StatusText.Text = statusMessage;
                });

                var progressReporter = (IProgress<string>)progress;

                await Task.Run(() =>
                {
                    int currentProcessed = 0;

                    foreach (var fileNode in filesToDelete)
                    {
                        currentProcessed++;
                        string filePath = fileNode.FilePath;

                        try
                        {
                            if (File.Exists(filePath))
                            {
                                long fileSize = new FileInfo(filePath).Length;

                                File.Delete(filePath);
                                deletedCount++;

                                freedBytes += fileSize;
                            }
                            else
                            {
                                failedFilesList.Add(CreateErrorNode(fileNode, "File missing on disk"));
                            }
                        }
                        catch (Exception ex)
                        {
                            failedFilesList.Add(CreateErrorNode(fileNode, ex.Message));
                        }

                        progressReporter.Report($"Deleting... ({currentProcessed}/{totalFiles}) - Freed: {FormatSize(freedBytes)}");
                    }
                });

                MainActionButton.IsEnabled = true;

                ClearListButton_Click(null, null);

                StatusText.Text = $"Cleanup Complete! Successfully deleted {deletedCount} of {totalFiles} files. Total freed space: {FormatSize(freedBytes)}";

                if (failedFilesList.Count > 0)
                {
                    var errorFolder = new FolderNodeModel
                    {
                        Name = $"⚠️ Failed to Delete ({failedFilesList.Count} files)",
                        FullPath = "ErrorList",
                        Children = new ObservableCollection<object>(failedFilesList)
                    };

                    DuplicatesTreeView.ItemsSource = new List<object> { errorFolder };
                }
            }
        }

        private FileNodeModel CreateErrorNode(FileNodeModel originalNode, string errorMessage)
        {
            return new FileNodeModel
            {
                FileName = $"{originalNode.FileName}  [ERROR: {errorMessage}]",
                FilePath = originalNode.FilePath,
                FileIcon = "❌",
                FormattedSize = originalNode.FormattedSize,
                IsSelectedForDeletion = false,
            };
        }

        private List<FileNodeModel> GetCheckedFiles(IEnumerable<FolderNodeModel> folders)
        {
            var result = new List<FileNodeModel>();
            if (folders == null) return result;

            foreach (var folder in folders)
            {
                result.AddRange(folder.Files.Where(f => f.IsSelectedForDeletion));
                result.AddRange(GetCheckedFiles(folder.SubFolders));
            }
            return result;
        }

        private void ResetToScanState()
        {
            _isScanning = false;
            _isScanned = false;
            MainActionButton.IsEnabled = true;
            MainActionButton.Content = "SCAN";
            MainActionButton.Background = (Brush)FindResource("AccentColor");
            StatusText.Text = "Ready to scan target directories.";
            TotalSizeText.Visibility = Visibility.Collapsed;
            if (SearchCountText != null) SearchCountText.Text = "";
            if (FilesScannedText != null) FilesScannedText.Text = "";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(query) || query == "Search by File Name or SHA256 Hash...")
            {
                DuplicatesTreeView.ItemsSource = ExplorerTree;
                if (SearchCountText != null) SearchCountText.Text = "";
                return;
            }

            var filteredTree = new ObservableCollection<FolderNodeModel>();
            int matchCount = 0;

            foreach (var rootNode in ExplorerTree)
            {
                var filteredNode = FilterFolderTree(rootNode, query, ref matchCount);
                if (filteredNode != null)
                {
                    filteredTree.Add(filteredNode);
                }
            }

            DuplicatesTreeView.ItemsSource = filteredTree;
            if (SearchCountText != null) SearchCountText.Text = $"{matchCount} matches found";
        }

        private FolderNodeModel FilterFolderTree(FolderNodeModel node, string query, ref int matchCount)
        {
            if (node == null) return null;

            var filteredNode = new FolderNodeModel
            {
                Name = node.Name,
                FullPath = node.FullPath
            };

            foreach (var subFolder in node.SubFolders)
            {
                var matchedSub = FilterFolderTree(subFolder, query, ref matchCount);
                if (matchedSub != null && matchedSub.Children.Count > 0)
                {
                    filteredNode.Children.Add(matchedSub);
                }
            }

            foreach (var file in node.Files)
            {
                if (file.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    file.FilePath.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    (file.Hash != null && file.Hash.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    filteredNode.Children.Add(file);
                    matchCount++;
                }
            }

            if (filteredNode.Children.Count > 0) return filteredNode;

            return null;
        }

        private void ShowHashCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ToggleHashVisibility(ExplorerTree, true);
        }

        private void ShowHashCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleHashVisibility(ExplorerTree, false);
        }

        private void ToggleHashVisibility(IEnumerable<FolderNodeModel> folders, bool isVisible)
        {
            if (folders == null) return;

            foreach (var folder in folders)
            {
                foreach (var file in folder.Files)
                {
                    file.IsHashVisible = isVisible;
                }
                ToggleHashVisibility(folder.SubFolders, isVisible);
            }
        }

        #region Show Matching Files

        private void ContextMenu_ShowMatchingFiles_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is FileNodeModel node)
                ShowMatchingFiles(node);
        }

        private void MatchSummary_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is FileNodeModel node)
                ShowMatchingFiles(node);
        }

        private void ShowMatchingFiles(FileNodeModel currentNode)
        {
            if (currentNode == null || string.IsNullOrEmpty(currentNode.Hash)) return;
            if (!_duplicateGroupIndex.TryGetValue(currentNode.Hash, out var siblings) || siblings.Count < 2) return;

            var others = siblings.Where(n => !ReferenceEquals(n, currentNode)).ToList();
            if (others.Count == 0) return;

            var matchesWindow = new MatchingFilesWindow(currentNode, others) { Owner = this };
            matchesWindow.ShowDialog();
        }

        #endregion

        #region Folder Duplicates Popup

        private void FolderBadge_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not FolderNodeModel folder) return;

            e.Handled = true;

            var files = GetAllFilesRecursive(folder);
            if (files.Count == 0) return;

            var window = new FolderDuplicatesWindow(folder.Name, files) { Owner = this };
            window.ShowDialog();
        }

        private List<FileNodeModel> GetAllFilesRecursive(FolderNodeModel folder)
        {
            var result = new List<FileNodeModel>();
            result.AddRange(folder.Files);

            foreach (var subFolder in folder.SubFolders)
                result.AddRange(GetAllFilesRecursive(subFolder));

            return result;
        }

        #endregion

        private void ContextMenu_OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string folderPath && Directory.Exists(folderPath))
            {
                Process.Start(new ProcessStartInfo { FileName = folderPath, UseShellExecute = true });
            }
        }

        private void ContextMenu_OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string filePath && File.Exists(filePath))
            {
                Process.Start("explorer.exe", $"/select,\"{filePath}\"");
            }
        }

        private void ContextMenu_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is string filePath && File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
            }
        }

        private void BtnOpenJson_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(duplicatesFilePath))
            {
                try { Process.Start(new ProcessStartInfo(duplicatesFilePath) { UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show($"Could not open the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else MessageBox.Show("The duplicates data file does not exist yet.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDeleteJson_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(duplicatesFilePath))
            {
                MessageBoxResult result = MessageBox.Show(
                    "Are you sure you want to delete the saved duplicates data file?",
                    "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        File.Delete(duplicatesFilePath);
                        ClearListButton_Click(null, null);
                        MessageBox.Show("Data file successfully deleted.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex) { MessageBox.Show($"Could not delete the file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
            else MessageBox.Show("The duplicates data file does not exist.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            allDuplicates.Clear();
            ExplorerTree.Clear();
            StatusText.Text = "List cleared.";
            if (File.Exists(duplicatesFilePath)) File.Delete(duplicatesFilePath);
            ResetToScanState();
        }

        private void SetSearchPlaceholder()
        {
            SearchBox.Text = "Search by File Name or SHA256 Hash...";
            SearchBox.Foreground = new SolidColorBrush(Colors.Gray);

            SearchBox.GotFocus += (s, ev) =>
            {
                if (SearchBox.Text == "Search by File Name or SHA256 Hash...")
                {
                    SearchBox.Text = "";
                    SearchBox.Foreground = (Brush)FindResource("TextPrimary");
                }
            };

            SearchBox.LostFocus += (s, ev) =>
            {
                if (string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchBox.Text = "Search by File Name or SHA256 Hash...";
                    SearchBox.Foreground = new SolidColorBrush(Colors.Gray);
                }
            };
        }

        private void AdvancedToggle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (AdvancedPanel.Visibility == Visibility.Collapsed)
            {
                AdvancedPanel.Visibility = Visibility.Visible;
                AdvancedToggleText.Text = "▲ Hide Properties";
            }
            else
            {
                AdvancedPanel.Visibility = Visibility.Collapsed;
                AdvancedToggleText.Text = "▼ Properties";
            }
        }
        
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new VistaFolderBrowserDialog { Description = "Select folder" };
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
                FolderListBox.Items.Add(dlg.SelectedPath);
        }

        private void BtnAddDefault_Click(object sender, RoutedEventArgs e)
        {
            FolderListBox.Items.Clear();
            string osDrive = Path.GetPathRoot(Environment.SystemDirectory);
            if (!string.IsNullOrEmpty(osDrive)) FolderListBox.Items.Add(osDrive);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListBox.SelectedItem != null)
                FolderListBox.Items.Remove(FolderListBox.SelectedItem);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Visibility = Visibility.Collapsed;
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private bool isMax = false;
        private double prevW, prevH, prevX, prevY;

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

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
        }
    }
}
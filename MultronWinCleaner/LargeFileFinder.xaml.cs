using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Ookii.Dialogs.Wpf;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for LargeFileFinder.xaml
    /// </summary>
    /// 
   
    public partial class LargeFileFinder : Window
    {
        byte cancel = 0;
        public LargeFileFinder()
        {
            InitializeComponent();
            FolderListBox.Items.Add("C:\\");
            LargeFilesDataGrid.Items.Clear();
            SizeUnitComboBox.SelectedIndex = 0;
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
            this.Hide();
            CancelScan.IsEnabled = false;
            StartScan.IsEnabled = true;
             
         
        }
        public class LargeFileInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string SizeMB { get; set; }
        }
     

        private async void StartScan_Click(object sender, RoutedEventArgs e)
        {
            if (!double.TryParse(SizeThresholdTextBox.Text, out double minSize))
            {
                ScanResultLabel.Text = $"Please enter a valid size threshold.";
            
                return;
            }
            ScanResultLabel.Text = "Calculating...";
            StartScan.IsEnabled = false;
            string selectedUnit = (SizeUnitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            long multiplier = selectedUnit switch
            {
                "KB" => 1024L,
                "MB" => 1024L * 1024L,
                "GB" => 1024L * 1024L * 1024L,
                _ => 1L  
            };

            long minBytes = (long)(minSize * multiplier);




            CancelScan.IsEnabled = true;
            ScanProgressBar.Value = 0;
            cancel = 0;
            LargeFilesDataGrid.ItemsSource = null;
           
            try
            {
                var foundFiles = new List<LargeFileInfo>();
                var sw = Stopwatch.StartNew();

               
                var allFiles = await Task.Run(() =>
                {
                    var files = new List<string>();
                    foreach (string folder in FolderListBox.Items)
                    {
                       if(cancel == 1)
                        {
                            break;
                        }

                        if (Directory.Exists(folder))
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
                
                    ScanResultLabel.Text = $"No files found in the selected folders.";
                    return;
                }

                int processed = 0;

               
                await Task.Run(() =>
                {
                    foreach (var file in allFiles)
                    {
                        if (cancel == 1)
                        {
                            break;
                        }

                        try
                        {
                            var info = new FileInfo(file);
                            if (info.Length >= minBytes)
                            {
                                lock (foundFiles)  
                                {
                                    foundFiles.Add(new LargeFileInfo
                                    {
                                        Name = info.Name,
                                        Path = info.FullName,
                                        SizeMB = (info.Length / (1024d * 1024d)).ToString("F2") + " MB"
                                    });
                               
                                       
                                }
                            }
                        }
                        catch { }

                        processed++;

                       
                        if (processed % 20 == 0 || processed == totalFiles)
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
                    LargeFilesDataGrid.ItemsSource = foundFiles;
                    if(cancel == 1)
                    {
                        ScanResultLabel.Text= $"{foundFiles.Count} large files found. Scan Cancelled!";
                    } else
                    {
                        ScanResultLabel.Text = $"{foundFiles.Count} large files found.";
                    }
                       
                    StartScan.IsEnabled = true;
                    CancelScan.IsEnabled = false;
                });
            }
            catch (Exception ex)
            {
                ScanResultLabel.Text = $"An error occurred: {ex.Message}";
         
            }
            finally
            {
             
            }
        }

 
        private IEnumerable<string> SafeFileEnumerator(string root)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                if (cancel == 1)
                {
                    break;
                }
                string currentDir = pending.Pop();
                string[] subDirs = Array.Empty<string>();

                try
                {
                    subDirs = Directory.GetDirectories(currentDir);
                }
                catch { continue; }

                foreach (var dir in subDirs)
                {
                    if (cancel == 1)
                    {
                        break;
                    }
                    pending.Push(dir);
                }

                string[] files = Array.Empty<string>();
                try
                {
                    files = Directory.GetFiles(currentDir);
                }
                catch { continue; }

                foreach (var file in files)
                {
                    if (cancel == 1)
                    {
                        break;
                    }
                    yield return file;
                }
            }
        }
        private bool sortDescending = true;

        private void LargeFilesDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.Header.ToString() == "Size")
            {
                e.Handled = true; 

                var list = LargeFilesDataGrid.ItemsSource as List<LargeFileInfo>;
                if (list == null) return;

                if (sortDescending)
                {
                    list = list.OrderByDescending(f => ParseSizeMB(f.SizeMB)).ToList();
                }
                else
                {
                    list = list.OrderBy(f => ParseSizeMB(f.SizeMB)).ToList();
                }

                sortDescending = !sortDescending;

                LargeFilesDataGrid.ItemsSource = null;  
                LargeFilesDataGrid.ItemsSource = list;
            }
        }
        
        private void OpenFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (LargeFilesDataGrid.SelectedItem is LargeFileInfo selectedFile)
            {
                string argument = "/select, \"" + selectedFile.Path + "\"";
                System.Diagnostics.Process.Start("explorer.exe", argument);
            }
        }

        
    
        private double ParseSizeMB(string sizeString)
        {
         
            if (string.IsNullOrEmpty(sizeString)) return 0;

            var parts = sizeString.Split(' ');
            if (parts.Length != 2) return 0;

            if (!double.TryParse(parts[0], out double value)) return 0;

            string unit = parts[1].ToUpper();

            if (unit == "GB")
                return value * 1024;  

            return value; 
        }
        private void RemoveSelectedFolder_Click(object sender, RoutedEventArgs e)
        {
             if(FolderListBox.SelectedItem != null)
            {
                FolderListBox.Items.Remove(FolderListBox.SelectedItem);
            }
        }

        private void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog();
            dialog.Description = "Select a folder";
            dialog.UseDescriptionForTitle = true;

            bool? result = dialog.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                if (!FolderListBox.Items.Contains(dialog.SelectedPath))
                {
                    FolderListBox.Items.Add(dialog.SelectedPath);
                }
            }
        }

        private void CancelScan_Click(object sender, RoutedEventArgs e)
        {
            cancel = 1;
            CancelScan.IsEnabled = false;
            StartScan.IsEnabled = true;
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private double previousWidth, previousHeight, previousLeft, previousTop;

        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            LargeFilesDataGrid.ItemsSource = null;
            LargeFilesDataGrid.Items.Clear();
            ScanResultLabel.Text = "List Cleared!";
        }

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

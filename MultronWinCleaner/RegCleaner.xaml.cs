using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MultronWinCleaner
{
    public partial class RegCleaner : Window
    {
        private bool _isLoaded = false;

        public RegCleaner()
        {
            InitializeComponent();
            this.Loaded += RegCleaner_Loaded;
        }

        private void RegCleaner_Loaded(object sender, RoutedEventArgs e)
        {
            ResultsListView.ItemsSource = _issues;
            CleanSelectedButton.IsEnabled = false;
            ExportResultsButton.IsEnabled = false;
            CancelScanButton.Visibility = Visibility.Collapsed;
            UpdateIssuesCountLabel();
        }

        public class RegistryIssue
        {
            public string Path { get; set; }
            public string IssueType { get; set; }
        }

        private ObservableCollection<RegistryIssue> _issues = new();
        private CancellationTokenSource? _scanCts;

        private void UpdateIssuesCountLabel()
        {
            Dispatcher.Invoke(() =>
            {
                if (IssuesCountLabel != null)
                    IssuesCountLabel.Content = $"Issues found: {_issues.Count}";
            });
        }

        private void FullRegistryScan(CancellationToken token)
        {
            string[] baseRoots = new string[]
            {
                @"HKEY_CURRENT_USER\Software",
                @"HKEY_LOCAL_MACHINE\Software",
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services"
            };

            int totalSteps = baseRoots.Length;
            int currentStep = 0;

            foreach (var root in baseRoots)
            {
                token.ThrowIfCancellationRequested();

                if (root.StartsWith("HKEY_CURRENT_USER"))
                    ScanRegistrySubTree(Registry.CurrentUser, root["HKEY_CURRENT_USER\\".Length..], token);
                else if (root.StartsWith("HKEY_LOCAL_MACHINE"))
                    ScanRegistrySubTree(Registry.LocalMachine, root["HKEY_LOCAL_MACHINE\\".Length..], token);

                currentStep++;
                UpdateProgressBarSafe(currentStep * 100 / totalSteps);
            }
        }

        private void ScanRegistrySubTree(RegistryKey baseKey, string subPath, CancellationToken token)
        {
            using RegistryKey? key = baseKey.OpenSubKey(subPath);
            if (key == null) return;

            ScanRegistryKeyRecursive(key, $"{GetRootName(baseKey)}\\{subPath}", token);
        }

        private void ScanRegistryKeyRecursive(RegistryKey key, string fullPath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            foreach (var valName in key.GetValueNames())
            {
                if (valName.Equals("InstallPath", StringComparison.OrdinalIgnoreCase) ||
                    valName.Equals("Path", StringComparison.OrdinalIgnoreCase))
                {
                    var val = key.GetValue(valName)?.ToString();
                    if (!string.IsNullOrEmpty(val) && !Directory.Exists(val) && !File.Exists(val))
                    {
                        AddIssueSafe(new RegistryIssue()
                        {
                            Path = fullPath,
                            IssueType = "Broken Path"
                        });
                    }
                }
            }

            foreach (var subKeyName in key.GetSubKeyNames())
            {
                try
                {
                    using RegistryKey? subKey = key.OpenSubKey(subKeyName);
                    if (subKey == null) continue;

                    ScanRegistryKeyRecursive(subKey, $"{fullPath}\\{subKeyName}", token);
                }
                catch { }
            }
        }

        private string GetRootName(RegistryKey baseKey)
        {
            if (baseKey == Registry.CurrentUser) return "HKEY_CURRENT_USER";
            if (baseKey == Registry.LocalMachine) return "HKEY_LOCAL_MACHINE";
            return "UNKNOWN_ROOT";
        }

        private void AddIssueSafe(RegistryIssue issue)
        {
            Dispatcher.Invoke(() =>
            {
                _issues.Add(issue);
                UpdateIssuesCountLabel();
            });
        }

        private void UpdateProgressBarSafe(int value)
        {
            Dispatcher.Invoke(() => ScanProgressBar.Value = value);
        }

        private void CleanSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResultsListView?.SelectedItems == null) return;

            foreach (RegistryIssue issue in ResultsListView.SelectedItems)
            {
                try
                {
                    DeleteRegistryKey(issue.Path);
                    _issues.Remove(issue);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting key {issue.Path}: {ex.Message}");
                }
            }

            CleanSelectedButton.IsEnabled = _issues.Count > 0;
            ExportResultsButton.IsEnabled = _issues.Count > 0;
            UpdateIssuesCountLabel();
        }

        private void DeleteRegistryKey(string fullPath)
        {
            if (fullPath.StartsWith("HKEY_CURRENT_USER\\"))
            {
                string subPath = fullPath["HKEY_CURRENT_USER\\".Length..];
                Registry.CurrentUser.DeleteSubKeyTree(subPath, false);
            }
            else if (fullPath.StartsWith("HKEY_LOCAL_MACHINE\\"))
            {
                string subPath = fullPath["HKEY_LOCAL_MACHINE\\".Length..];
                Registry.LocalMachine.DeleteSubKeyTree(subPath, false);
            }
            else
            {
                throw new Exception("Unsupported root key in path: " + fullPath);
            }
        }

        private void ExportResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_issues.Count == 0) return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "RegistryIssues.csv",
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (dialog.ShowDialog() == true)
            {
                using var writer = new StreamWriter(dialog.FileName);
                writer.WriteLine("Registry Path,Issue Type");
                foreach (var issue in _issues)
                {
                    writer.WriteLine($"\"{issue.Path}\",\"{issue.IssueType}\"");
                }
                MessageBox.Show("Export completed.");
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = SearchBox.Text.Trim().ToLower();

            var view = CollectionViewSource.GetDefaultView(ResultsListView.ItemsSource);
            if (view == null)
                return;

            if (string.IsNullOrEmpty(filter))
            {
                view.Filter = null;   
            }
            else
            {
                view.Filter = item =>
                {
                    if (item is RegistryIssue issue)
                    {
                       
                        return issue.Path?.ToLower().Contains(filter) == true
                               || issue.IssueType?.ToLower().Contains(filter) == true;
                    }
                    return false;
                };
            }
        }




        private async void ScanButton_Click_1(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            CancelScanButton.Visibility = Visibility.Visible;
            ScanProgressBar.Visibility = Visibility.Visible;
            ScanProgressBar.Value = 0;

            _issues.Clear();
            UpdateIssuesCountLabel();

            _scanCts = new CancellationTokenSource();

            try
            {
                await Task.Run(() => FullRegistryScan(_scanCts.Token));
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Scan cancelled.");
            }
            finally
            {
                ScanProgressBar.Visibility = Visibility.Collapsed;
                CancelScanButton.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
                CancelScanButton.IsEnabled = false;
                CleanButton.IsEnabled = true;
                CleanSelectedButton.IsEnabled = _issues.Count > 0;
                ExportResultsButton.IsEnabled = _issues.Count > 0;
            }
        }

        private void CleanButton_Click(object sender, RoutedEventArgs e)
        {
            if (_issues.Count == 0)
            {
                MessageBox.Show("No issues to clean.");
                return;
            }

            if (MessageBox.Show($"Clean all {_issues.Count} registry issues?", "Confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            if (MessageBox.Show("Create System Restore Point?", "Restore Point", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    CreateSystemRestorePoint("Multron Win Cleaner - Before Registry Clean");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Restore point failed: {ex.Message}");
                }
            }

            foreach (var issue in _issues)
            {
                try { DeleteRegistryKey(issue.Path); } catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            _issues.Clear();
            CleanSelectedButton.IsEnabled = false;
            ExportResultsButton.IsEnabled = false;
            UpdateIssuesCountLabel();
            MessageBox.Show("Cleanup completed.");
        }

        private void CancelScanButton_Click_1(object sender, RoutedEventArgs e)
        {
            _scanCts?.Cancel();
        }

        private void CreateSystemRestorePoint(string description)
        {
            ManagementScope scope = new("\\\\.\\root\\default");
            scope.Connect();

            ManagementClass classInstance = new(scope, new ManagementPath("SystemRestore"), null);
            ManagementBaseObject inParams = classInstance.GetMethodParameters("CreateRestorePoint");
            inParams["Description"] = description;

            inParams["RestorePointType"] = 0;  
            inParams["EventType"] = 102;      

            ManagementBaseObject outParams = classInstance.InvokeMethod("CreateRestorePoint", inParams, null);
            uint retVal = (uint)outParams.Properties["ReturnValue"].Value;
            if (retVal != 0)
                throw new Exception("Restore point creation failed. Error code: " + retVal);
        }

        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;
        private void MaximizeButton_Click(object sender, RoutedEventArgs e) => this.WindowState = (this.WindowState == WindowState.Maximized) ? WindowState.Normal : WindowState.Maximized;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => this.Hide();
    }
}
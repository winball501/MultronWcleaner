using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MultronWinCleaner.Processes;

namespace MultronWinCleaner
{
    public partial class ScanLogsWindow : Window
    {
        public ScanLogsWindow()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            string? selected = (LogsDataGrid.SelectedItem as MalwareScanLog.Entry)?.FilePath;
            var entries = MalwareScanLog.LoadEntries();
            LogsDataGrid.ItemsSource = entries;
            CountText.Text = entries.Count > 0 ? Loc.F("({0})", entries.Count) : "";
            EmptyPanel.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LogsDataGrid.SelectedItem = entries.FirstOrDefault(entry => entry.FilePath == selected) ?? entries.FirstOrDefault();
            if (LogsDataGrid.SelectedItem is MalwareScanLog.Entry current)
                ShowLog(current);
            else
                LogViewerTextBox.Text = "";
        }

        private void ShowLog(MalwareScanLog.Entry entry)
        {
            LogViewerTextBox.Text = MalwareScanLog.Read(entry.FilePath);
            LogViewerTextBox.ScrollToHome();
        }

        private void LogsDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (LogsDataGrid.SelectedItem is MalwareScanLog.Entry entry)
                ShowLog(entry);
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Refresh_Click(object sender, RoutedEventArgs e) => Refresh();

        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            if (LogsDataGrid.SelectedItem is not MalwareScanLog.Entry entry || !File.Exists(entry.FilePath)) return;
            try
            {
                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + entry.FilePath + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, ex.Message, Loc.T("Scan Logs"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CopyLog_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LogViewerTextBox.Text)) return;
            try { Clipboard.SetText(LogViewerTextBox.Text); }
            catch (Exception) { }
        }

        private void ExportLog_Click(object sender, RoutedEventArgs e)
        {
            if (LogsDataGrid.SelectedItem is not MalwareScanLog.Entry entry || !File.Exists(entry.FilePath)) return;
            var dialog = new SaveFileDialog
            {
                Title = Loc.T("Export Log"),
                Filter = Loc.T("Log file") + " (*.log)|*.log|" + Loc.T("Text file") + " (*.txt)|*.txt",
                FileName = entry.FileName,
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true) return;
            try
            {
                File.Copy(entry.FilePath, dialog.FileName, true);
                ShowExported(dialog.FileName);
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not export the logs:\n{0}", ex.Message), Loc.T("Scan Logs"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            var entries = MalwareScanLog.LoadEntries();
            if (entries.Count == 0) return;
            var dialog = new SaveFileDialog
            {
                Title = Loc.T("Export Logs"),
                Filter = Loc.T("ZIP archive") + " (*.zip)|*.zip",
                FileName = "Multron_ScanLogs_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".zip",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true) return;
            string zipPath = dialog.FileName;
            Cursor = Cursors.Wait;
            try
            {
                await Task.Run(() => MalwareScanLog.ExportZip(entries.Select(entry => entry.FilePath), zipPath));
                ShowExported(zipPath);
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not export the logs:\n{0}", ex.Message), Loc.T("Scan Logs"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = null;
            }
        }

        private void ShowExported(string path)
        {
            var open = AppDialog.Show(this, Loc.F("Exported to:\n{0}\n\nShow the file in File Explorer?", path), Loc.T("Scan Logs"),
                MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (open != MessageBoxResult.Yes) return;
            try { Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + path + "\"") { UseShellExecute = true }); }
            catch (Exception) { }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(MalwareScanLog.LogFolder);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + MalwareScanLog.LogFolder + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, ex.Message, Loc.T("Scan Logs"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (LogsDataGrid.SelectedItem is not MalwareScanLog.Entry entry) return;
            if (AppDialog.Show(this, Loc.F("Delete this log?\n\n{0}", entry.FileName), Loc.T("Scan Logs"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            try
            {
                MalwareScanLog.Delete(entry.FilePath);
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not delete the file:\n{0}", ex.Message), Loc.T("Scan Logs"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            LogsDataGrid.SelectedItem = null;
            Refresh();
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            int count = MalwareScanLog.LoadEntries().Count;
            if (count == 0) return;
            if (AppDialog.Show(this, Loc.F("Delete all {0} scan log(s)?", count), Loc.T("Scan Logs"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            MalwareScanLog.DeleteAll();
            Refresh();
        }
    }
}

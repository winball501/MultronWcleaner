using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MultronWinCleaner.Processes;

namespace MultronWinCleaner
{
    public partial class QuarantineWindow : Window
    {
        public event EventHandler? Changed;

        public QuarantineWindow()
        {
            InitializeComponent();
            Refresh();
        }

        public void Refresh()
        {
            var entries = QuarantineManager.LoadEntries();
            QuarantineDataGrid.ItemsSource = entries;
            CountText.Text = entries.Count > 0 ? Loc.F("({0})", entries.Count) : "";
            EmptyPanel.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NotifyChanged()
        {
            Refresh();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void TopBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Restore_Click(object sender, RoutedEventArgs e)
        {
            var entry = QuarantineDataGrid.SelectedItem as QuarantineManager.Entry;
            if (entry == null) return;

            var answer = AppDialog.Show(this, Loc.F("Restore this file to its original location?\n\n{0}", entry.OriginalPath),
                Loc.T("Quarantine"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                if (QuarantineManager.Restore(entry))
                {
                    MalwareScanLog.ForCurrentResults("removal", Loc.T("Threat removal")).Write(Loc.F("Restored from quarantine: {0}", entry.OriginalPath));
                    NotifyChanged();
                    AppDialog.Show(this, Loc.T("The file has been restored."), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    NotifyChanged();
                    AppDialog.Show(this, Loc.T("The quarantined file was not found."), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not restore the file:\n{0}", ex.Message), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var entry = QuarantineDataGrid.SelectedItem as QuarantineManager.Entry;
            if (entry == null) return;

            var answer = AppDialog.Show(this, Loc.F("Permanently delete this quarantined file?\n\n{0}\n\nThis action cannot be undone.", entry.OriginalPath),
                Loc.T("Quarantine"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                QuarantineManager.Delete(entry);
                MalwareScanLog.ForCurrentResults("removal", Loc.T("Threat removal")).Write(Loc.F("Deleted from quarantine: {0}", entry.OriginalPath));
                NotifyChanged();
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not delete the file:\n{0}", ex.Message), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            int count = QuarantineManager.Count;
            if (count == 0) return;

            var answer = AppDialog.Show(this, Loc.F("Permanently delete all {0} quarantined file(s)?\n\nThis action cannot be undone.", count),
                Loc.T("Quarantine"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                QuarantineManager.DeleteAll();
                MalwareScanLog.ForCurrentResults("removal", Loc.T("Threat removal")).Write(Loc.F("Deleted all {0} quarantined file(s)", count));
                NotifyChanged();
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not delete quarantined files:\n{0}", ex.Message), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (QuarantineDataGrid.SelectedItem is QuarantineManager.Entry entry)
                Export(new List<QuarantineManager.Entry> { entry });
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e) => Export(QuarantineManager.LoadEntries());

        private async void Export(List<QuarantineManager.Entry> entries)
        {
            if (entries.Count == 0) return;

            var answer = AppDialog.Show(this, Loc.F("Export {0} quarantined file(s) to a ZIP file?\n\nThe ZIP contains the original malware. It is protected with the password \"{1}\" so it cannot be opened or run by accident. Only share it with antivirus vendors or security researchers, and never run the files.", entries.Count, QuarantineManager.ExportPassword),
                Loc.T("Quarantine"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes) return;

            var dialog = new SaveFileDialog
            {
                Title = Loc.T("Export Quarantine"),
                Filter = Loc.T("ZIP archive") + " (*.zip)|*.zip",
                FileName = entries.Count == 1
                    ? Path.GetFileNameWithoutExtension(entries[0].OriginalFileName) + "_quarantine.zip"
                    : "Multron_Quarantine_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + ".zip",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true) return;

            string zipPath = dialog.FileName;
            ExportSelectedButton.IsEnabled = false;
            ExportAllButton.IsEnabled = false;
            Cursor = Cursors.Wait;
            try
            {
                await Task.Run(() => QuarantineManager.ExportZip(entries, zipPath));
                MalwareScanLog.ForCurrentResults("removal", Loc.T("Threat removal")).Write(Loc.F("Exported {0} quarantined file(s) to {1}", entries.Count, zipPath));
                var open = AppDialog.Show(this, Loc.F("{0} file(s) exported to:\n{1}\n\nZIP password: {2}\n\nShow the file in File Explorer?", entries.Count, zipPath, QuarantineManager.ExportPassword),
                    Loc.T("Quarantine"), MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (open == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + zipPath + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, Loc.F("Could not export the quarantined files:\n{0}", ex.Message), Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = null;
                ExportSelectedButton.IsEnabled = true;
                ExportAllButton.IsEnabled = true;
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(QuarantineManager.QuarantineFolder);
                Process.Start(new ProcessStartInfo("explorer.exe", "\"" + QuarantineManager.QuarantineFolder + "\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppDialog.Show(this, ex.Message, Loc.T("Quarantine"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}

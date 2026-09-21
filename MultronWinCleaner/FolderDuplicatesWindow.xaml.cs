using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace MultronWinCleaner
{
    /// <summary>
    /// Popup that lists every duplicate file found inside a folder (including its
    /// subfolders), visually grouped by identical-content set so it's obvious at a
    /// glance which files are copies of each other. Opened by clicking a folder's
    /// duplicate-count badge in the main tree.
    /// </summary>
    public partial class FolderDuplicatesWindow : Window
    {
        public FolderDuplicatesWindow(string folderName, List<FileNodeModel> files)
        {
            InitializeComponent();

            Title = $"Duplicates in {folderName}";

            int groupCount = files.Select(f => f.Hash).Distinct().Count();
            HeaderText.Text = $"{files.Count} duplicate files in \"{folderName}\" ({groupCount} duplicate sets)";

            var view = CollectionViewSource.GetDefaultView(files);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FileNodeModel.Hash)));
            view.SortDescriptions.Add(new SortDescription(nameof(FileNodeModel.Hash), ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription(nameof(FileNodeModel.FilePath), ListSortDirection.Ascending));

            FilesListBox.ItemsSource = view;
        }

        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileNodeModel node && File.Exists(node.FilePath))
            {
                Process.Start(new ProcessStartInfo { FileName = node.FilePath, UseShellExecute = true });
            }
        }

        private void OpenLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is FileNodeModel node && File.Exists(node.FilePath))
            {
                Process.Start("explorer.exe", $"/select,\"{node.FilePath}\"");
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }
    }
}
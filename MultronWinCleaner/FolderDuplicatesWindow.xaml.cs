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
    public partial class FolderDuplicatesWindow : Window
    {
        public FolderDuplicatesWindow(string folderName, List<FileNodeModel> files)
        {
            InitializeComponent();

            Title = Loc.F("Duplicates in {0}", folderName);

            int groupCount = files.Select(f => f.Hash).Distinct().Count();
            HeaderText.Text = Loc.F("{0} duplicate files in \"{1}\" ({2} duplicate sets)", files.Count, folderName, groupCount);

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

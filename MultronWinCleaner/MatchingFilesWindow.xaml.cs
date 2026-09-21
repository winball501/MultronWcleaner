using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultronWinCleaner
{
    /// <summary>
    /// Small popup that lists every file sharing the same hash as the file the user
    /// clicked "Show Matching Files" on. Each row shows the file name and full path,
    /// with buttons to open the file itself or reveal it in File Explorer.
    /// </summary>
    public partial class MatchingFilesWindow : Window
    {
        public MatchingFilesWindow(FileNodeModel sourceFile, List<FileNodeModel> matches)
        {
            InitializeComponent();

            Title = $"Matching Files - {sourceFile.FileName}";
            HeaderText.Text = matches.Count == 1
                ? $"1 file matches \"{sourceFile.FileName}\":"
                : $"{matches.Count} files match \"{sourceFile.FileName}\":";

            MatchesListBox.ItemsSource = matches;
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
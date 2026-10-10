using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultronWinCleaner
{
    public partial class MatchingFilesWindow : Window
    {
        public MatchingFilesWindow(FileNodeModel sourceFile, List<FileNodeModel> matches)
        {
            InitializeComponent();

            Title = Loc.F("Matching Files - {0}", sourceFile.FileName);
            HeaderText.Text = matches.Count == 1
                ? Loc.F("1 file matches \"{0}\":", sourceFile.FileName)
                : Loc.F("{0} files match \"{1}\":", matches.Count, sourceFile.FileName);

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

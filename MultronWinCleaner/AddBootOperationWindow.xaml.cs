using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MultronWinCleaner
{
    public partial class AddBootOperationWindow : Window
    {
        public enum OperationType
        {
            Delete,
            Move,
            Rename,
            ManualCommand
        }

        public class BootOperation
        {
            public OperationType Type { get; set; }
            public string SourcePath { get; set; } = string.Empty;
            public string DestinationPath { get; set; } = string.Empty;
            public string ManualCommand { get; set; } = string.Empty;

            public override string ToString()
            {
                return Type switch
                {
                    OperationType.Delete => $"Delete: {SourcePath}",
                    OperationType.Move => $"Move: {SourcePath} -> {DestinationPath}",
                    OperationType.Rename => $"Rename: {SourcePath} -> {DestinationPath}",
                    OperationType.ManualCommand => $"ManualCommand: {ManualCommand}",
                    _ => base.ToString(),
                };
            }
        }

        private ObservableCollection<BootOperation> bootOperations = new();

        public BootOperation? ResultOperation { get; private set; }

        public AddBootOperationWindow()
        {
            InitializeComponent();

            cmbOperationType.SelectionChanged += CmbOperationType_SelectionChanged;
            cmbOperationType.SelectedIndex = 0;
 
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CmbOperationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbOperationType.SelectedItem is ComboBoxItem selected)
            {
                var selectedStr = selected.Content?.ToString() ?? "";

                txtDestPath.IsEnabled = selectedStr == "Move" || selectedStr == "Rename";
                txtSourcePath.IsEnabled = selectedStr != "ManualCommand";
                txtManualCommand.IsEnabled = selectedStr == "ManualCommand";

              
                txtSourcePath.Clear();
                txtDestPath.Clear();
                txtManualCommand.Clear();
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (cmbOperationType.SelectedItem is not ComboBoxItem selectedItem)
            {
                MessageBox.Show("Please select an operation type.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string typeStr = selectedItem.Content?.ToString()?.Trim() ?? "";

            if (!Enum.TryParse(typeStr, true, out OperationType opType))
            {
                MessageBox.Show("Invalid operation type.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BootOperation newOp;

            switch (opType)
            {
                case OperationType.ManualCommand:
                    string cmd = txtManualCommand.Text.Trim();
                    if (string.IsNullOrWhiteSpace(cmd))
                    {
                        MessageBox.Show("Please enter a manual command.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    newOp = new BootOperation
                    {
                        Type = opType,
                        ManualCommand = cmd
                    };
                    break;

                case OperationType.Move:
                case OperationType.Rename:
                    if (string.IsNullOrWhiteSpace(txtSourcePath.Text))
                    {
                        MessageBox.Show("Please enter a source path.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtDestPath.Text))
                    {
                        MessageBox.Show("Please enter a destination path.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    newOp = new BootOperation
                    {
                        Type = opType,
                        SourcePath = txtSourcePath.Text.Trim(),
                        DestinationPath = txtDestPath.Text.Trim()
                    };
                    break;

                case OperationType.Delete:
                default:
                    if (string.IsNullOrWhiteSpace(txtSourcePath.Text))
                    {
                        MessageBox.Show("Please enter a source path.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    newOp = new BootOperation
                    {
                        Type = opType,
                        SourcePath = txtSourcePath.Text.Trim()
                    };
                    break;
            }

            bootOperations.Add(newOp);

         
            txtSourcePath.Clear();
            txtDestPath.Clear();
            txtManualCommand.Clear();
        }

        private void RunCommand_Click(object sender, RoutedEventArgs e)
        {
            if (lstBootOperations.SelectedItem is not BootOperation selected)
            {
                MessageBox.Show("Please select an operation from the list.", "Run Command", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                switch (selected.Type)
                {
                    case OperationType.ManualCommand:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c {selected.ManualCommand}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        MessageBox.Show($"Ran command:\n{selected.ManualCommand}");
                        break;

                     
                    default:
                        MessageBox.Show($"Operation {selected} çalıştırma kodu eklenmedi.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Komut çalıştırılırken hata: {ex.Message}");
            }
        }
    }
}

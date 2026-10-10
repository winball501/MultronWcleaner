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
                    OperationType.Delete => Loc.F("Delete: {0}", SourcePath),
                    OperationType.Move => Loc.F("Move: {0} -> {1}", SourcePath, DestinationPath),
                    OperationType.Rename => Loc.F("Rename: {0} -> {1}", SourcePath, DestinationPath),
                    OperationType.ManualCommand => Loc.F("ManualCommand: {0}", ManualCommand),
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
                var selectedStr = Loc.En(selected.Content);

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
                AppDialog.Show(Loc.T("Please select an operation type."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string typeStr = Loc.En(selectedItem.Content).Trim();

            if (!Enum.TryParse(typeStr, true, out OperationType opType))
            {
                AppDialog.Show(Loc.T("Invalid operation type."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BootOperation newOp;

            switch (opType)
            {
                case OperationType.ManualCommand:
                    string cmd = txtManualCommand.Text.Trim();
                    if (string.IsNullOrWhiteSpace(cmd))
                    {
                        AppDialog.Show(Loc.T("Please enter a manual command."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        AppDialog.Show(Loc.T("Please enter a source path."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(txtDestPath.Text))
                    {
                        AppDialog.Show(Loc.T("Please enter a destination path."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        AppDialog.Show(Loc.T("Please enter a source path."), Loc.T("Input Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                AppDialog.Show(Loc.T("Please select an operation from the list."), Loc.T("Run Command"), MessageBoxButton.OK, MessageBoxImage.Information);
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
                        AppDialog.Show(Loc.F("Ran command:\n{0}", selected.ManualCommand), Loc.T("Run Command"), MessageBoxButton.OK, MessageBoxImage.Information);
                        break;

                     
                    default:
                        AppDialog.Show(Loc.F("No run code has been added for operation {0}.", selected), Loc.T("Run Command"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppDialog.Show(Loc.F("Error while running the command: {0}", ex.Message), Loc.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

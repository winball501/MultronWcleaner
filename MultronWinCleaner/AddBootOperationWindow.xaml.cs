using System;
using System.Collections.Generic;
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
using static MultronWinCleaner.StartupManager;

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for AddBootOperationWindow.xaml
    /// </summary>
    public partial class AddBootOperationWindow : Window
    {
        public AddBootOperationWindow()
        {
            InitializeComponent();
            cmbOperationType.SelectionChanged += CmbOperationType_SelectionChanged;
            cmbOperationType.SelectedIndex = 0;
        }
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }
        public BootOperation? ResultOperation { get; private set; }

     
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void CmbOperationType_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var selected = ((ComboBoxItem)cmbOperationType.SelectedItem).Content.ToString();
            txtDestPath.IsEnabled = selected == "Move" || selected == "Rename";
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var typeStr = ((ComboBoxItem)cmbOperationType.SelectedItem).Content.ToString();
            if (string.IsNullOrEmpty(txtSourcePath.Text))
            {
                MessageBox.Show("Please enter source path.");
                return;
            }

            if ((typeStr == "Move" || typeStr == "Rename") && string.IsNullOrEmpty(txtDestPath.Text))
            {
                MessageBox.Show("Please enter destination path.");
                return;
            }

            var op = new BootOperation
            {
                Type = Enum.Parse<OperationType>(typeStr!),
                SourcePath = txtSourcePath.Text.Trim(),
                DestinationPath = txtDestPath.Text.Trim()
            };

            ResultOperation = op;
            DialogResult = true;
        }
    }
}

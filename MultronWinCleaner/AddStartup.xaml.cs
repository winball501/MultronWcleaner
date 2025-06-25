using Microsoft.Win32;
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

namespace MultronWinCleaner
{
    /// <summary>
    /// Interaction logic for AddStartup.xaml
    /// </summary>
    public partial class AddStartup : Window
    {
        public AddStartup()
        {
            InitializeComponent();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string path = txtPath.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a name for the startup application.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please enter the executable path.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
                if (runKey == null)
                {
                    MessageBox.Show("Unable to open the registry key for startup applications.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                 
                runKey.SetValue(name, path);

                MessageBox.Show("Startup application added successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true; 
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding startup application:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

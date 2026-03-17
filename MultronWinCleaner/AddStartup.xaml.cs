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
        public static void AddWinlogonEntry(string pathToAdd)
        {
            try
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey( @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", writable: true);

                if (key == null)
                {
                    MessageBox.Show("Key returned null.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                } else
                {
                    string raw = key.GetValue("Userinit") as string ?? "";


                    var parts = raw.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();

                    bool alreadyExists = parts.Any(p => p.Equals(pathToAdd, StringComparison.OrdinalIgnoreCase));
                    if (alreadyExists)
                    {
                        MessageBox.Show(pathToAdd + " Already Exists", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        parts.Add(pathToAdd);

                        string newValue = string.Join(",", parts) + ",";
                        key.SetValue("Userinit", newValue);
                        MessageBox.Show("Operation Sucessfully! " + newValue + " > added!", "Error", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

           

            

            }
            catch (Exception ex) { 
                 MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            
            }
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            string path = txtPath.Text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a name.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Please enter the executable path.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (rbRegistry.IsChecked == true)
                {
                  
                    using RegistryKey runKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);

                    if (runKey == null)
                    {
                        MessageBox.Show("Unable to open registry key.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    runKey.SetValue(name, $"\"{path}\"");
                    MessageBox.Show("Startup application added successfully. ", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (rbTaskScheduler.IsChecked == true)
                { 
                    string args = $"/create /tn \"{name}\" /tr \"\\\"{path}\\\"\" /sc onlogon /rl highest /f";

                    var proc = new System.Diagnostics.Process();
                    proc.StartInfo.FileName = "schtasks.exe";
                    proc.StartInfo.Arguments = args;
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();
                    proc.WaitForExit();

                    if (proc.ExitCode != 0)
                    {
                        MessageBox.Show("Task Scheduler entry could not be created.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    } else
                    {
                        MessageBox.Show("Added to task scheduler successfully. ", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }

                } else
                {
                    AddWinlogonEntry(txtPath.Text);
                }

            
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                InitialDirectory = @"C:\",
                Filter = "All files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            bool? result = openFileDialog1.ShowDialog();

            if (result == true)
            {
                txtPath.Text = openFileDialog1.FileName;

                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    txtName.Text = System.IO.Path.GetFileNameWithoutExtension(openFileDialog1.FileName);
                }
            }
        }

        private void rbWinLogon_Checked(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = false;

        }

        private void rbTaskScheduler_Checked(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = true;
        }

        private void rbRegistry_Checked(object sender, RoutedEventArgs e)
        {
            txtName.IsEnabled = true;
        }
    }
}

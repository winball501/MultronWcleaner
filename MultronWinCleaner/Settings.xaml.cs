using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
     
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
           
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt";

        
            if (System.IO.File.Exists(path))
            {
          
                string fileContent = System.IO.File.ReadAllText(path);
 
                fileContent = fileContent.Trim().ToLower();  
 

           
                if (fileContent.Contains("trayicon:1"))
                {
                    this.chkTrayIcon.IsChecked = true;
                    
                }
                else
                {
                    this.chkTrayIcon.IsChecked = false;
                }

                if (fileContent.Contains("autoclean:1"))
                {
                    this.chkAutoClean.IsChecked = true;
                }
                else
                {
                    this.chkAutoClean.IsChecked = false;
                }
              
 
                Regex minutesRegex = new Regex(@"minutes:(\d+)");

                
                Match match = minutesRegex.Match(fileContent);

                if (match.Success)  
                {
                    string minutesValue = match.Groups[1].Value;  
                     txtCleaningInterval.Text = minutesValue;
                }
               


            }
            else
            {

                System.IO.File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt");
            }

        }

        private void txtCleaningInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
       
        }
        private void txtCleaningInterval_TextChanged_1(object sender, TextChangedEventArgs e)
        {
 
        }
        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            string fileContent = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt");
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Settings.txt";

            string[] lines = fileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
             
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("minutes:"))
                {
                    lines[i] = "minutes:" + txtCleaningInterval.Text;  
                }
                if (chkAutoClean.IsChecked == true && lines[i].Contains("autoclean:0"))
                {
                    lines[i] = "autoclean:1";  
                }
                else if (chkAutoClean.IsChecked == false && lines[i].Contains("autoclean:1"))
                {
                    lines[i] = "autoclean:0";  
                }
                if (chkTrayIcon.IsChecked == true && lines[i].Contains("trayicon:0"))
                {
                    lines[i] = "trayicon:1";
                }
                else if (chkTrayIcon.IsChecked == false && lines[i].Contains("trayicon:1"))
                {
                    lines[i] = "trayicon:0";
                }
            }

          
            fileContent = string.Join(Environment.NewLine, lines);

          
            System.IO.File.WriteAllText(path, fileContent);
            MessageBox.Show("Settings Saved!");
        }
  

        private void txtCleaningInterval_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
    
            e.Handled = !IsNumeric(e.Text);
        }
 
        private bool IsNumeric(string text)
        {
            int result;
            return int.TryParse(text, out result);
        }
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
          
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void chkAutoClean_Checked(object sender, RoutedEventArgs e)
        {
        
        }

        private void chkTrayIcon_Checked(object sender, RoutedEventArgs e)
        {
          
        }

   
    }
}

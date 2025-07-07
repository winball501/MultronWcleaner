using Octokit;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Input;

namespace MultronWinCleaner
{
    public partial class Configure : Window
    {
      
        Utilities utilities;

        string settingsPath = AppDomain.CurrentDomain.BaseDirectory + "Settings.txt";
        public Configure(Utilities utilities)
        {
            InitializeComponent();
 

       
            var lines = System.IO.File.ReadAllLines(settingsPath);
         
            foreach(string line in lines)
            {
                if(line.StartsWith("selectedservice:"))
                {
                    string service = utilities.window.stringtokenizer(line, ":", 1);
                    string isenabled = utilities.window.stringtokenizer(line, ":", 2);
                    foreach(var svc in utilities.turboBoostServices)
                    {
                        if(svc.ServiceName == service)
                        {
                            if(isenabled == "false")
                            {
                                svc.IsSelected = false;
                            } else
                            {
                                svc.IsSelected = true;
                            }
                        }
                    }
                }
            }
            ServicesList.ItemsSource = utilities.turboBoostServices;
            this.utilities = utilities;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

            int selected = 0;
            foreach (var svc in utilities.turboBoostServices.ToList())
            {
                if (svc.IsSelected == true)
                {
                    selected = 1;
                    svc.IsSelected = true;
                    utilities.savesettings("selectedservice:" + svc.ServiceName  + ":" + "true");
                } else
                {
                    svc.IsSelected = false;
                    utilities.savesettings("selectedservice:" + svc.ServiceName + ":" + "false");
                }
                  
            }  
            if(selected == 0)
            {
                MessageBox.Show("All services disabled! this means services going to enabled and in turbo boost going to disable all.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                foreach (var svc in utilities.turboBoostServices.ToList())
                {
                   
                        svc.IsSelected = true;
                        utilities.savesettings("selectedservice:" + svc.ServiceName + ":" + "true");
                }
            

            }
          
            this.Hide();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
    public class ServiceItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string ServiceName { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}

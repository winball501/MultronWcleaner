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
        ObservableCollection<ServiceItem> items;
        string settingPrefix = "selectedservice:";

        public void ShowItems(string title, string header, ObservableCollection<ServiceItem> list, string prefix)
        {
            TitleText.Text = title;
            HeaderText.Text = header;
            ServicesList.ItemsSource = list;
            items = list;
            settingPrefix = prefix;
            Show();
            Activate();
        }

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
            items = utilities.turboBoostServices;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!items.Any(i => i.IsSelected))
            {
                MessageBox.Show("Select at least one option.", "Optimization", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (var item in items)
                utilities.savesettings(settingPrefix + item.ServiceName + ":" + (item.IsSelected ? "true" : "false"));
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
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Title => string.IsNullOrEmpty(DisplayName) ? ServiceName : DisplayName;
        public Visibility DescriptionVisibility => string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;

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

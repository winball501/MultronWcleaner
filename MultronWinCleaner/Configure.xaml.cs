using HidSharp.Reports;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MultronWinCleaner
{
    public partial class Configure : Window
    {
        public ObservableCollection<ServiceItem> Services { get; set; }
        Utilities utilities;
        public Configure(Utilities utilities)
        {
            InitializeComponent();

            Services = new ObservableCollection<ServiceItem>
            {
                new ServiceItem{ ServiceName = "SysMain" },
                new ServiceItem{ ServiceName = "Fax" },
                new ServiceItem{ ServiceName = "BluetoothSupport" },
                new ServiceItem{ ServiceName = "Spooler" },
                new ServiceItem{ ServiceName = "MapsBroker" },
                new ServiceItem{ ServiceName = "PrintNotify" },
                new ServiceItem{ ServiceName = "XblGameSave" },
                new ServiceItem{ ServiceName = "WMPNetworkSvc" },
                new ServiceItem{ ServiceName = "TouchKeyboardAndHandwritingPanelService" },
                new ServiceItem{ ServiceName = "RemoteRegistry" },
                new ServiceItem{ ServiceName = "DiagTrack" },
                new ServiceItem{ ServiceName = "RetailDemo" },
                new ServiceItem{ ServiceName = "WSearch" },
                new ServiceItem{ ServiceName = "dmwappushservice" },
                new ServiceItem{ ServiceName = "WerSvc" },
                new ServiceItem{ ServiceName = "DeviceInstall" },
                new ServiceItem{ ServiceName = "iphlpsvc" },
                new ServiceItem{ ServiceName = "RemoteAccess" },
                new ServiceItem{ ServiceName = "TabletInputService" },
                new ServiceItem{ ServiceName = "WpnService" },
                new ServiceItem{ ServiceName = "Themes" },
                new ServiceItem{ ServiceName = "XblAuthManager" }
            };

            ServicesList.ItemsSource = Services;
            this.utilities = utilities;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {

            utilities.turboBoostServices.Clear();
            foreach (var svc in Services)
            {
                if (svc.IsSelected)
                    utilities.turboBoostServices.Add(svc.ServiceName);
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

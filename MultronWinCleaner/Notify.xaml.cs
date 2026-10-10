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
    public partial class Notify : Window
    {
        
        private readonly Action onClick;

        public Notify(string status, string proc, string cleaned, Action onClick) : this(status, proc, cleaned)
        {
            this.onClick = onClick;
            Cursor = Cursors.Hand;
            if (onClick != null)
                DetailsButton.Visibility = Visibility.Visible;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenDetails();
        }

        private void DetailsButton_Click(object sender, RoutedEventArgs e)
        {
            OpenDetails();
        }

        private void OpenDetails()
        {
            if (onClick == null)
                return;
            Close();
            onClick();
        }

        public Notify(string status, string proc, string cleaned)
        {
            InitializeComponent();
            
            Status.Text = status;
            DetailsText.Text = proc;
            AmountText.Text = cleaned;
            if (string.IsNullOrWhiteSpace(cleaned))
                AmountText.Visibility = Visibility.Collapsed;
        }
        public Notify AsWarning()
        {
            IconStart.Color = (Color)ColorConverter.ConvertFromString("#FFEF5350");
            IconEnd.Color = (Color)ColorConverter.ConvertFromString("#FFC62828");
            IconText.Text = "!";
            AmountText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE53935"));
            return this;
        }

        private static readonly List<Notify> openNotifications = new List<Notify>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (openNotifications.Count == 0)
            {
                position = ReadPosition();
                layout = ReadLayout();
            }
            openNotifications.Add(this);
            Closed += (s, args) =>
            {
                openNotifications.Remove(this);
                ArrangeNotifications();
            };
            SizeChanged += (s, args) => ArrangeNotifications();
            ArrangeNotifications();
            Dispatcher.BeginInvoke(ArrangeNotifications, System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public const string PositionSettingKey = "notifyposition";

        public enum Corner { BottomRight, BottomLeft, TopRight, TopLeft }

        public const string LayoutSettingKey = "notifylayout";

        public enum Layout { Stacked, Overlapping }

        public static Corner ReadPosition() => (Corner)ReadSetting(PositionSettingKey, typeof(Corner));

        public static Layout ReadLayout() => (Layout)ReadSetting(LayoutSettingKey, typeof(Layout));

        private static int ReadSetting(string key, Type enumType)
        {
            try
            {
                string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
                if (System.IO.File.Exists(path))
                {
                    string? line = System.IO.File.ReadAllLines(path)
                        .FirstOrDefault(l => l.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase));
                    if (line != null && int.TryParse(line.Substring(key.Length + 1).Trim(), out int value) && Enum.IsDefined(enumType, value))
                        return value;
                }
            }
            catch
            {
            }
            return 0;
        }

        private static Corner position = Corner.BottomRight;
        private static Layout layout = Layout.Stacked;

        private static void ArrangeNotifications()
        {
            var workArea = SystemParameters.WorkArea;
            bool top = position == Corner.TopRight || position == Corner.TopLeft;
            bool left = position == Corner.BottomLeft || position == Corner.TopLeft;
            double next = top ? workArea.Top + 10 : workArea.Bottom - 10;
            foreach (Notify notify in openNotifications)
            {
                double height = Math.Max(notify.ActualHeight, notify.MinHeight);
                notify.Left = left ? workArea.Left + 10 : workArea.Right - notify.Width - 10;
                notify.Top = top ? next : next - height;
                if (layout == Layout.Overlapping)
                    continue;
                next = top ? next + height + 10 : notify.Top - 10;
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        } 
    }
}

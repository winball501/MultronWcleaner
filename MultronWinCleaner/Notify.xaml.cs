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
    /// Interaction logic for Notify.xaml
    /// </summary>
    public partial class Notify : Window
    {
        
        private readonly Action onClick;

        public Notify(string status, string proc, string cleaned, Action onClick) : this(status, proc, cleaned)
        {
            this.onClick = onClick;
            Cursor = Cursors.Hand;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
          
            var workArea = SystemParameters.WorkArea;

            
            this.Left = workArea.Right - this.Width - 10;   
            this.Top = workArea.Bottom - this.Height - 10; 
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        } 
    }
}

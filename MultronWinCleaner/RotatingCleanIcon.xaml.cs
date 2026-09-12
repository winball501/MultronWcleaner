using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Multron_Win_Cleaner
{
 
    public partial class RotatingCleanIcon : UserControl
    {
        public RotatingCleanIcon()
        {
            InitializeComponent();
        }

        public void StartSpinning()
        {
            var storyboard = (Storyboard)Resources["SpinStoryboard"];
            storyboard.Begin();
        }

        public void StopSpinning()
        {
            var storyboard = (Storyboard)Resources["SpinStoryboard"];
            storyboard.Stop();
        }
    }
}
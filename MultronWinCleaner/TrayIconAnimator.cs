using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Multron_Win_Cleaner
{
    public class TrayIconAnimator
    {
        private readonly TaskbarIcon _trayIcon;
        private readonly BitmapImage _idleIcon;
        private readonly BitmapImage[] _frames;
        private readonly DispatcherTimer _timer;
        private int _frameIndex;

        public TrayIconAnimator(TaskbarIcon trayIcon, string idleIconUri, string frameUriFormat, int frameCount, int fps = 12)
        {
            _trayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));

            _idleIcon = new BitmapImage(new Uri(idleIconUri, UriKind.Absolute));

            _frames = new BitmapImage[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                _frames[i] = new BitmapImage(new Uri(string.Format(frameUriFormat, i), UriKind.Absolute));
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000.0 / fps)
            };
            _timer.Tick += OnTick;
        }

        public bool IsSpinning => _timer.IsEnabled;

        public void Start()
        {
            if (_timer.IsEnabled) return;
            _frameIndex = 0;
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _trayIcon.IconSource = _idleIcon; // eski ikona geri dön
        }

        private void OnTick(object sender, EventArgs e)
        {
            _trayIcon.IconSource = _frames[_frameIndex];
            _frameIndex = (_frameIndex + 1) % _frames.Length;
        }
    }
}

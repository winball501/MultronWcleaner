using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Multron_Win_Cleaner
{
    /// <summary>
    /// Converts a ProgressBar's Value (0-100) into a PathGeometry describing
    /// a circular arc, so the "ModernProgressBarStyle" ring in MainWindow.xaml
    /// can render a determinate percentage without any extra code-behind.
    ///
    /// Geometry assumes a 120x120 host with StrokeThickness="12"
    /// (radius = (120 - 12) / 2 = 54, center = (60,60)).
    /// If you resize the ring, update Center/Radius below to match.
    /// </summary>
    public class PercentToArcConverter : IValueConverter
    {
        private const double Center = 60.0;
        private const double Radius = 54.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double percent = 0;
            if (value is double d) percent = d;
            else if (value != null) double.TryParse(value.ToString(), NumberStyles.Float, culture, out percent);

            percent = Math.Max(0, Math.Min(100, percent));
             
            double angle = Math.Max(0.01, percent / 100.0 * 359.99);

            var center = new Point(Center, Center);
            var startPoint = new Point(Center, Center - Radius);

            double radians = (Math.PI / 180.0) * (angle - 90);
            var endPoint = new Point(
                center.X + Radius * Math.Cos(radians),
                center.Y + Radius * Math.Sin(radians));

            bool isLargeArc = angle > 180.0;

            var figure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            figure.Segments.Add(new ArcSegment(
                endPoint,
                new Size(Radius, Radius),
                0,
                isLargeArc,
                SweepDirection.Clockwise,
                true));

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            geometry.Freeze();
            return geometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

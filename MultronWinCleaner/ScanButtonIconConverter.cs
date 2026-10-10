using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Effects;
using MultronWinCleaner;

namespace Multron_Win_Cleaner
{
    public class ScanButtonIconConverter : IValueConverter
    {
        private sealed record Look(string Icon, Brush Fill, Effect Glow);

        private static readonly Look ScanLook = Create("\uE721", "#FF42A5F5", "#FF1565C0", "#FF1E88E5");
        private static readonly Look CleanLook = Create("\uEA99", "#FF4CC56A", "#FF1E7E34", "#FF28A745");
        private static readonly Look CancelLook = Create("\uE711", "#FFF06272", "#FFB02A37", "#FFDC3545");
        private static readonly Look KillLook = Create("\uE71A", "#FFFFB74D", "#FFEF6C00", "#FFFF9800");

        private static Look Create(string icon, string from, string to, string glow)
        {
            var brush = new LinearGradientBrush(Parse(from), Parse(to), new Point(0, 0), new Point(1, 1));
            brush.Freeze();
            var shadow = new DropShadowEffect { BlurRadius = 18, ShadowDepth = 3, Opacity = 0.35, Color = Parse(glow) };
            shadow.Freeze();
            return new Look(icon, brush, shadow);
        }

        private static Color Parse(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private static Look Resolve(object value)
        {
            string text = value?.ToString() ?? "";
            if (text == Loc.T("Clean")) return CleanLook;
            if (text == Loc.T("Cancel")) return CancelLook;
            if (text == Loc.T("Kill")) return KillLook;
            return ScanLook;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Look look = Resolve(value);
            return (parameter as string) switch
            {
                "Fill" => look.Fill,
                "Glow" => look.Glow,
                _ => look.Icon
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}

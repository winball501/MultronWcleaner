using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace MultronWinCleaner
{
    public static class Appearance
    {
        public const string EnabledSettingKey = "transparency";
        public const string ColorSettingKey = "transparencycolor";
        public const string LevelSettingKey = "transparencylevel";

        public static readonly (string Name, Color Color)[] Colors =
        {
            ("Blue", Color.FromRgb(0x1E, 0x88, 0xE5)),
            ("Green", Color.FromRgb(0x2E, 0xB8, 0x5C)),
            ("Orange", Color.FromRgb(0xFF, 0x8C, 0x1A)),
            ("Purple", Color.FromRgb(0x8E, 0x5C, 0xE6)),
            ("Red", Color.FromRgb(0xE5, 0x39, 0x35)),
            ("Pink", Color.FromRgb(0xE8, 0x4A, 0x9B)),
            ("Teal", Color.FromRgb(0x14, 0xB8, 0xA6)),
            ("Gray", Color.FromRgb(0x80, 0x80, 0x80))
        };

        private static readonly (byte WindowAlpha, byte PanelAlpha, double Tint)[] Levels =
        {
            (0xF6, 0xFA, 0.06),
            (0xEA, 0xF3, 0.10),
            (0xD8, 0xE8, 0.16)
        };

        private static readonly string[] Keys = { "WindowBackground", "PanelBackground" };

        public static bool Enabled => ReadSetting(EnabledSettingKey) != "0";
        public static int ColorIndex => Clamp(ReadSetting(ColorSettingKey), Colors.Length);
        public static int LevelIndex => ReadSetting(LevelSettingKey) is string s ? Clamp(s, Levels.Length) : 1;

        public static void Apply()
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            foreach (string key in Keys)
                resources.Remove(key);
            if (!Enabled)
                return;

            var theme = resources.MergedDictionaries.LastOrDefault();
            if (theme == null)
                return;
            var level = Levels[LevelIndex];
            Color tint = Colors[ColorIndex].Color;
            if (theme["WindowBackground"] is SolidColorBrush window)
                resources["WindowBackground"] = Tinted(window.Color, tint, level.Tint, level.WindowAlpha);
            if (theme["PanelBackground"] is SolidColorBrush panel)
                resources["PanelBackground"] = Tinted(panel.Color, tint, level.Tint, level.PanelAlpha);
        }

        private static SolidColorBrush Tinted(Color baseColor, Color tint, double amount, byte alpha)
        {
            byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * amount);
            var brush = new SolidColorBrush(Color.FromArgb(alpha, Mix(baseColor.R, tint.R), Mix(baseColor.G, tint.G), Mix(baseColor.B, tint.B)));
            brush.Freeze();
            return brush;
        }

        private static int Clamp(string? value, int count) =>
            int.TryParse(value, out int i) && i >= 0 && i < count ? i : 0;

        private static string? ReadSetting(string key)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.txt");
                if (!File.Exists(path))
                    return null;
                return File.ReadAllLines(path)
                    .Where(l => l.StartsWith(key + ":", StringComparison.OrdinalIgnoreCase))
                    .Select(l => l.Substring(key.Length + 1).Trim())
                    .LastOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}

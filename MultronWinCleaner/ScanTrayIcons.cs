using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace MultronWinCleaner
{
    internal static class ScanTrayIcons
    {
        private const int Size = 32;

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon[] CreateScanningFrames(int count) =>
            Enumerable.Range(0, count).Select(i => Create(i * 360f / count)).ToArray();

        public static Icon CreateIdle() => Create(null);

        private static Icon Create(float? spinnerAngle)
        {
            using var bitmap = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                bool scanning = spinnerAngle.HasValue;
                if (scanning)
                {
                    var ring = new RectangleF(2f, 2f, Size - 4f, Size - 4f);
                    using var track = new Pen(Color.FromArgb(70, 66, 165, 245), 3f);
                    g.DrawEllipse(track, ring);
                    using var arc = new Pen(Color.FromArgb(255, 66, 165, 245), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                    g.DrawArc(arc, ring, spinnerAngle!.Value, 110f);
                }

                float height = scanning ? 20f : 28f;
                float width = height * 18f / 22f;
                float left = (Size - width) / 2f, top = (Size - height) / 2f;
                PointF P(float x, float y) => new PointF(left + (x - 3f) / 18f * width, top + (y - 1f) / 22f * height);

                using var shield = new GraphicsPath();
                shield.AddLine(P(12, 1), P(3, 5));
                shield.AddLine(P(3, 5), P(3, 11));
                shield.AddBezier(P(3, 11), P(3, 16.55f), P(6.84f, 21.74f), P(12, 23));
                shield.AddBezier(P(12, 23), P(17.16f, 21.74f), P(21, 16.55f), P(21, 11));
                shield.AddLine(P(21, 11), P(21, 5));
                shield.CloseFigure();

                using var fill = new LinearGradientBrush(new RectangleF(left, top, width, height),
                    Color.FromArgb(255, 66, 165, 245), Color.FromArgb(255, 21, 101, 192), LinearGradientMode.ForwardDiagonal);
                g.FillPath(fill, shield);

                if (!scanning)
                {
                    using var check = new Pen(Color.White, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
                    g.DrawLines(check, new[] { P(8, 12), P(11, 15), P(16.5f, 9) });
                }
            }

            IntPtr handle = bitmap.GetHicon();
            try
            {
                using var temporary = Icon.FromHandle(handle);
                return (Icon)temporary.Clone();
            }
            finally
            {
                DestroyIcon(handle);
            }
        }
    }
}

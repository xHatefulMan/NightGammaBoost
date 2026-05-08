using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NightGammaBoost
{
    public static class ScreenAnalyzer
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        public static double Threshold = 0.15;
        public static double MaxBoost = 1.0;

        private static bool _boostActive = false;
        private const double HysteresisMargin = 0.08;

        public static double GetWindowBrightness(IntPtr hwnd)
        {
            try
            {
                GetWindowRect(hwnd, out RECT rect);
                int w = rect.Right - rect.Left;
                int h = rect.Bottom - rect.Top;
                if (w <= 0 || h <= 0) return 0.5;
                int bw = Math.Max(1, w / 4);
                int bh = Math.Max(1, h / 4);
                using var bmp = new Bitmap(bw, bh);
                using var g = Graphics.FromImage(bmp);
                IntPtr hdc = g.GetHdc();
                PrintWindow(hwnd, hdc, 0x2);
                g.ReleaseHdc(hdc);
                return CalcBrightness(bmp);
            }
            catch { return 0.5; }
        }

        public static double GetScreenBrightness(Screen screen)
        {
            try
            {
                var bounds = screen.Bounds;
                int bw = Math.Max(1, bounds.Width / 4);
                int bh = Math.Max(1, bounds.Height / 4);
                using var bmp = new Bitmap(bw, bh);
                using var g = Graphics.FromImage(bmp);
                g.CopyFromScreen(bounds.Location, Point.Empty, bmp.Size);
                return CalcBrightness(bmp);
            }
            catch { return 0.5; }
        }

        public static double GetAverageBrightness() =>
            GetScreenBrightness(Screen.PrimaryScreen);

        private static double CalcBrightness(Bitmap bmp)
        {
            long total = 0; int count = 0;
            for (int x = 0; x < bmp.Width; x += 3)
                for (int y = 0; y < bmp.Height; y += 3)
                {
                    var c = bmp.GetPixel(x, y);
                    total += (int)(c.R * 0.299 + c.G * 0.587 + c.B * 0.114);
                    count++;
                }
            return count == 0 ? 0.5 : (total / (double)count) / 255.0;
        }

        public static double BrightnessToBoost(double brightness)
        {
            if (_boostActive)
            {
                if (brightness > Threshold + HysteresisMargin)
                    _boostActive = false;
            }
            else
            {
                if (brightness < Threshold)
                    _boostActive = true;
            }
            return _boostActive ? MaxBoost : 0.0;
        }

        public static bool IsWindowForeground(IntPtr hwnd) =>
            GetForegroundWindow() == hwnd;

        public static Screen GetScreenForWindow(IntPtr hwnd)
        {
            GetWindowRect(hwnd, out RECT rect);
            return Screen.FromPoint(new Point(
                (rect.Left + rect.Right) / 2,
                (rect.Top + rect.Bottom) / 2));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NightGammaBoost
{
    public static class GammaController
    {
        [DllImport("gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [StructLayout(LayoutKind.Sequential)]
        private struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        private static Dictionary<string, RAMP> _originalRamps = new Dictionary<string, RAMP>();

        public static void SaveOriginalGamma()
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                IntPtr hdc = CreateDC(null, screen.DeviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero) continue;
                RAMP ramp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };
                GetDeviceGammaRamp(hdc, ref ramp);
                _originalRamps[screen.DeviceName] = ramp;
                DeleteDC(hdc);
            }
        }

        public static void RestoreGamma(Screen screen)
        {
            if (!_originalRamps.ContainsKey(screen.DeviceName)) return;
            IntPtr hdc = CreateDC(null, screen.DeviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero) return;
            RAMP ramp = _originalRamps[screen.DeviceName];
            SetDeviceGammaRamp(hdc, ref ramp);
            DeleteDC(hdc);
        }

        public static void RestoreOriginalGamma()
        {
            foreach (Screen screen in Screen.AllScreens)
                RestoreGamma(screen);
        }

        public static void ApplyBoost(Screen screen, double boost)
        {
            boost = Math.Max(0.0, Math.Min(1.0, boost));
            IntPtr hdc = CreateDC(null, screen.DeviceName, null, IntPtr.Zero);
            if (hdc == IntPtr.Zero) return;

            RAMP ramp = new RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            double gamma = 1.0 + boost * 1.2;
            for (int i = 0; i < 256; i++)
            {
                double normalized = i / 255.0;
                double corrected = Math.Pow(normalized, 1.0 / gamma);
                ushort value = (ushort)(Math.Min(corrected * 65535, 65535));
                ramp.Red[i] = value;
                ramp.Green[i] = value;
                ramp.Blue[i] = value;
            }

            SetDeviceGammaRamp(hdc, ref ramp);
            DeleteDC(hdc);
        }
    }
}
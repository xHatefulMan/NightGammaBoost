using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using Application = System.Windows.Application;
using WinForms = System.Windows.Forms;

namespace NightGammaBoost
{
    public partial class App : Application
    {
        private WinForms.NotifyIcon _tray = null!;
        private MainWindow _win = null!;
        private DispatcherTimer _timer = null!;

        public static bool IsAnyGameActive = false;
        public static string? ActiveGameName = null;

        private WinForms.Screen? _activeScreen = null;
        private IntPtr _activeHwnd = IntPtr.Zero;
        private double _currentBoost = 0;
        private double _targetBoost = 0;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            GammaController.SaveOriginalGamma();

            _win = new MainWindow();
            _win.Show();

            _tray = new WinForms.NotifyIcon
            {
                Text = "NightGammaBoost",
                Visible = true
            };

            try
            {
                var stream = Application.GetResourceStream(
                    new Uri("pack://application:,,,/brightness.ico"))?.Stream;
                if (stream != null)
                    _tray.Icon = new System.Drawing.Icon(stream);
                else
                    _tray.Icon = System.Drawing.SystemIcons.Application;
            }
            catch
            {
                _tray.Icon = System.Drawing.SystemIcons.Application;
            }

            _tray.MouseClick += (s, ev) =>
            {
                if (ev.Button == WinForms.MouseButtons.Left)
                {
                    if (_win.IsVisible) _win.Hide();
                    else { _win.Show(); _win.Activate(); }
                }
            };

            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("Ouvrir", null, (s, ev) => { _win.Show(); _win.Activate(); });
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add("Quitter", null, (s, ev) => Quit());
            _tray.ContextMenuStrip = menu;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
            _timer.Tick += Tick;
            _timer.Start();
        }

        private void Tick(object? sender, EventArgs e)
        {
            double brightness = GetCurrentBrightness();
            if (brightness < 0)
            {
                SetTarget(0);
                return;
            }
            double target = ScreenAnalyzer.BrightnessToBoost(brightness);
            SetTarget(target);
        }

        private double GetCurrentBrightness()
        {
            if (_win.GlobalEnabled)
            {
                IsAnyGameActive = true;
                ActiveGameName = null;
                _tray.Text = "NightGammaBoost - Global";
                _activeScreen = WinForms.Screen.PrimaryScreen;
                return ScreenAnalyzer.GetScreenBrightness(_activeScreen!);
            }

            foreach (var kvp in _win.GameAdaptiveEnabled)
            {
                if (!kvp.Value) continue;
                if (!GameDetector.GameList.TryGetValue(kvp.Key, out var proc)) continue;

                var procs = Process.GetProcessesByName(proc);
                if (procs.Length == 0) continue;

                try
                {
                    var hwnd = procs[0].MainWindowHandle;
                    if (hwnd == IntPtr.Zero) continue;

                    _activeHwnd = hwnd;
                    _activeScreen = ScreenAnalyzer.GetScreenForWindow(hwnd);
                    IsAnyGameActive = true;
                    ActiveGameName = kvp.Key;
                    _tray.Text = "NightGammaBoost - " + kvp.Key;

                    bool fg = ScreenAnalyzer.IsWindowForeground(hwnd);
                    if (!fg) return -1;

                    return ScreenAnalyzer.GetWindowBrightness(hwnd);
                }
                catch { continue; }
            }

            if (IsAnyGameActive)
            {
                IsAnyGameActive = false;
                ActiveGameName = null;
                _activeScreen = null;
                _activeHwnd = IntPtr.Zero;
                _tray.Text = "NightGammaBoost";
            }

            return -1;
        }

        private void SetTarget(double target)
        {
            _targetBoost = target;
            _currentBoost = target;

            if (_currentBoost > 0.01 && _activeScreen != null && _win.IsScreenEnabled(_activeScreen))
                GammaController.ApplyBoost(_activeScreen, _currentBoost);
            else
                GammaController.RestoreOriginalGamma();
        }

        public void RestoreAndReset()
        {
            GammaController.RestoreOriginalGamma();
            _currentBoost = 0;
            _targetBoost = 0;
            IsAnyGameActive = false;
            ActiveGameName = null;
        }

        public void Quit()
        {
            GammaController.RestoreOriginalGamma();
            _tray.Visible = false;
            _tray.Dispose();
            Shutdown();
        }

        public static bool IsStartupEnabled()
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
            return k?.GetValue("NightGammaBoost") != null;
        }

        public static void SetStartup(bool enable)
        {
            using var k = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (k == null) return;
            if (enable)
                k.SetValue("NightGammaBoost",
                    $"\"{WinForms.Application.ExecutablePath}\"");
            else
                k.DeleteValue("NightGammaBoost", false);
        }
    }
}
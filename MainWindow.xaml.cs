using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Application = System.Windows.Application;
using WinForms = System.Windows.Forms;
using WpfColor = System.Windows.Media.Color;
using WpfButton = System.Windows.Controls.Button;

namespace NightGammaBoost
{
    public partial class MainWindow : Window
    {
        public Dictionary<string, bool> GameAdaptiveEnabled = new Dictionary<string, bool>();
        private Dictionary<string, bool> _screenEnabled = new Dictionary<string, bool>();
        private DispatcherTimer _statusTimer;
        private SettingsManager.Settings _settings = new();
        private bool _loading = true;

        public bool GlobalEnabled => ChkGlobal.IsChecked == true;

        public bool IsScreenEnabled(WinForms.Screen screen)
        {
            if (_screenEnabled.TryGetValue(screen.DeviceName, out bool val)) return val;
            return true;
        }

        public MainWindow()
        {
            InitializeComponent();

            _loading = true;
            _settings = SettingsManager.Load();

            SliderThreshold.Value = _settings.Threshold * 100;
            SliderBoost.Value = _settings.MaxBoost * 100;
            ScreenAnalyzer.Threshold = _settings.Threshold;
            ScreenAnalyzer.MaxBoost = _settings.MaxBoost;
            ChkGlobal.IsChecked = _settings.Global;
            ChkStartup.IsChecked = _settings.Startup;

            LoadScreens();
            LoadGames();

            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _statusTimer.Tick += RefreshStatus;
            _statusTimer.Start();

            UpdatePreview();
            _loading = false;
        }

        private void SaveSettings()
        {
            if (_loading) return;

            _settings.Threshold = ScreenAnalyzer.Threshold;
            _settings.MaxBoost = ScreenAnalyzer.MaxBoost;
            _settings.Global = ChkGlobal.IsChecked == true;
            _settings.Startup = ChkStartup.IsChecked == true;

            _settings.EnabledGames = new List<string>();
            foreach (var kvp in GameAdaptiveEnabled)
                if (kvp.Value) _settings.EnabledGames.Add(kvp.Key);

            _settings.DisabledScreens = new List<string>();
            foreach (var kvp in _screenEnabled)
                if (!kvp.Value) _settings.DisabledScreens.Add(kvp.Key);

            SettingsManager.Save(_settings);
        }

        private void RefreshStatus(object? sender, EventArgs e)
        {
            bool active = GlobalEnabled || App.IsAnyGameActive;
            if (active)
            {
                StatusDot.Fill = new SolidColorBrush(WpfColor.FromRgb(34, 197, 94));
                StatusText.Text = App.ActiveGameName != null
                    ? "Actif — " + App.ActiveGameName
                    : "Actif — Mode global";
            }
            else
            {
                StatusDot.Fill = new SolidColorBrush(WpfColor.FromRgb(239, 68, 68));
                StatusText.Text = "Inactif";
            }
        }

        private void LoadScreens()
        {
            ScreenPanel.Children.Clear();
            _screenEnabled.Clear();

            int i = 1;
            foreach (var screen in WinForms.Screen.AllScreens)
            {
                bool enabled = !_settings.DisabledScreens.Contains(screen.DeviceName);
                _screenEnabled[screen.DeviceName] = enabled;
                var dev = screen.DeviceName;

                var btn = new WpfButton
                {
                    Content = "Ecran " + i + (screen.Primary ? " (Principal)" : ""),
                    Style = (Style)FindResource("ScreenBtn"),
                    Background = enabled
                        ? new SolidColorBrush(WpfColor.FromRgb(99, 102, 241))
                        : new SolidColorBrush(WpfColor.FromRgb(42, 42, 74)),
                    Tag = enabled
                };
                btn.Click += (s, ev) =>
                {
                    bool v = !(bool)btn.Tag;
                    btn.Tag = v;
                    btn.Background = v
                        ? new SolidColorBrush(WpfColor.FromRgb(99, 102, 241))
                        : new SolidColorBrush(WpfColor.FromRgb(42, 42, 74));
                    _screenEnabled[dev] = v;
                    SaveSettings();
                };
                ScreenPanel.Children.Add(btn);
                i++;
            }
        }

        private void LoadGames()
        {
            GamePanel.Children.Clear();
            GameAdaptiveEnabled.Clear();

            var installed = GameDetector.GetInstalledGames();
            if (installed.Count == 0)
            {
                GamePanel.Children.Add(new TextBlock
                {
                    Text = "Aucun jeu compatible detecte.",
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(102, 102, 128)),
                    FontSize = 12,
                    Margin = new Thickness(4)
                });
                return;
            }

            foreach (var game in installed)
            {
                bool savedOn = _settings.EnabledGames.Contains(game);
                GameAdaptiveEnabled[game] = savedOn;
                var g = game;

                var row = new Border
                {
                    Background = new SolidColorBrush(WpfColor.FromRgb(22, 22, 42)),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(10, 7, 10, 7),
                    Margin = new Thickness(0, 0, 0, 4)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var lbl = new TextBlock
                {
                    Text = game,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(192, 192, 216)),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(lbl, 0);

                var btn = new WpfButton
                {
                    Content = savedOn ? "ON" : "OFF",
                    Style = (Style)FindResource("GameBtn"),
                    Background = savedOn
                        ? new SolidColorBrush(WpfColor.FromRgb(99, 102, 241))
                        : new SolidColorBrush(WpfColor.FromRgb(42, 42, 74)),
                    Foreground = savedOn
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(WpfColor.FromRgb(85, 85, 102)),
                    Tag = savedOn
                };
                Grid.SetColumn(btn, 1);

                btn.Click += (s, ev) =>
                {
                    bool v = !(bool)btn.Tag;
                    btn.Tag = v;
                    btn.Content = v ? "ON" : "OFF";
                    btn.Background = v
                        ? new SolidColorBrush(WpfColor.FromRgb(99, 102, 241))
                        : new SolidColorBrush(WpfColor.FromRgb(42, 42, 74));
                    btn.Foreground = v
                        ? new SolidColorBrush(Colors.White)
                        : new SolidColorBrush(WpfColor.FromRgb(85, 85, 102));
                    GameAdaptiveEnabled[g] = v;
                    SaveSettings();
                };

                grid.Children.Add(lbl);
                grid.Children.Add(btn);
                row.Child = grid;
                GamePanel.Children.Add(row);
            }
        }

        private void SliderThreshold_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            if (ThresholdVal == null) return;
            int val = (int)SliderThreshold.Value;
            ThresholdVal.Text = val + "%";
            ScreenAnalyzer.Threshold = val / 100.0;
            UpdatePreview();
            SaveSettings();
        }

        private void SliderBoost_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            if (BoostVal == null) return;
            int val = (int)SliderBoost.Value;
            BoostVal.Text = val + "%";
            ScreenAnalyzer.MaxBoost = val / 100.0;
            UpdatePreview();
            SaveSettings();
        }

        private void UpdatePreview()
        {
            if (NightSceneThreshold == null || NightSceneBoost == null) return;
            double threshold = SliderThreshold.Value / 100.0;
            double boost = SliderBoost.Value / 100.0;
            NightSceneThreshold.Brightness = (float)threshold;
            NightSceneBoost.Brightness = (float)Math.Min(threshold + boost * 0.8, 1.0);
        }

        private void ChkStartup_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            App.SetStartup(ChkStartup.IsChecked == true);
            SaveSettings();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
            ((App)Application.Current).RestoreAndReset();
        }
    }
}
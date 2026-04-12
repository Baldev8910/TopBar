using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TopBar.Helpers;

namespace TopBar
{
    public partial class SettingsPopup : Window
    {
        public event Action<double>? BarHeightChanged;
        public event Action<double>? BarOpacityChanged;
        public event Action<double>? AnimSpeedChanged;
        public event Action<string>? TempUnitChanged;
        public event Action<int>? WeatherRefreshChanged;

        private bool _loading = true;

        public SettingsPopup(double barHeight, double screenWidth)
        {
            InitializeComponent();
            ContentRendered += (s, e) => ApplyWindowsStyling();

            Loaded += (s, e) =>
            {
                // Position — below bar, right aligned
                Left = screenWidth - Width - 8;
                Top = barHeight + 8;

                LoadSettings();
                _loading = false;
            };

            // Close when loses focus
            Deactivated += (s, e) => Close();
        }

        private void LoadSettings()
        {
            var s = SettingsService.Current;

            BarHeightSlider.Value = s.BarHeight;
            BarHeightLabel.Text = $"{s.BarHeight:0}px";

            BarOpacitySlider.Value = s.BarOpacity;
            BarOpacityLabel.Text = $"{s.BarOpacity * 100:0}%";

            AnimSpeedSlider.Value = s.AnimationSpeed;
            AnimSpeedLabel.Text = $"{s.AnimationSpeed:0.0}x";

            CelsiusRadio.IsChecked = s.TemperatureUnit == "C";
            FahrenheitRadio.IsChecked = s.TemperatureUnit == "F";

            RefreshCombo.SelectedIndex = s.WeatherRefreshMinutes switch
            {
                5 => 0,
                15 => 1,
                30 => 2,
                60 => 3,
                _ => 1
            };

            StartupCheckbox.IsChecked = StartupHelper.IsStartupEnabled();
        }

        private void BarHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            var val = Math.Round(e.NewValue);
            BarHeightLabel.Text = $"{val:0}px";
            SettingsService.Current.BarHeight = val;
            SettingsService.Save();
            BarHeightChanged?.Invoke(val);
        }

        private void BarOpacitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            BarOpacityLabel.Text = $"{e.NewValue * 100:0}%";
            SettingsService.Current.BarOpacity = e.NewValue;
            SettingsService.Save();
            BarOpacityChanged?.Invoke(e.NewValue);
        }

        private void AnimSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_loading) return;
            AnimSpeedLabel.Text = $"{e.NewValue:0.0}x";
            SettingsService.Current.AnimationSpeed = e.NewValue;
            SettingsService.Save();
            AnimSpeedChanged?.Invoke(e.NewValue);
        }

        private void TempUnit_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var unit = CelsiusRadio.IsChecked == true ? "C" : "F";
            SettingsService.Current.TemperatureUnit = unit;
            SettingsService.Save();
            TempUnitChanged?.Invoke(unit);
        }

        private void RefreshCombo_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var minutes = RefreshCombo.SelectedIndex switch
            {
                0 => 5,
                1 => 15,
                2 => 30,
                3 => 60,
                _ => 15
            };
            SettingsService.Current.WeatherRefreshMinutes = minutes;
            SettingsService.Save();
            WeatherRefreshChanged?.Invoke(minutes);
        }

        private void Startup_Checked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            StartupHelper.EnableStartup();
            SettingsService.Current.LaunchOnStartup = true;
            SettingsService.Save();
        }

        private void Startup_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            StartupHelper.DisableStartup();
            SettingsService.Current.LaunchOnStartup = false;
            SettingsService.Save();
        }

        private void ApplyWindowsStyling()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
            int cornerPreference = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
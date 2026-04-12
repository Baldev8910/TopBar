using System;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TopBar.Helpers;
using UserControl = System.Windows.Controls.UserControl;

namespace TopBar.Widgets
{
    public partial class WeatherWidget : UserControl
    {
        private WeatherPopupWindow? _popup;

        private System.Windows.Threading.DispatcherTimer? _closeTimer;

        private void Weather_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_popup != null && _popup.IsVisible)
            {
                _popup.Close();
                _popup = null;
                return;
            }

            var barHeight = 40;
            var position = PointToScreen(new System.Windows.Point(0, 0));
            _popup = new WeatherPopupWindow(barHeight, position.X, ActualWidth);
            _popup.Closed += (s, e) => _popup = null;

            // Fade in
            _popup.Opacity = 0;
            _popup.Show();
            AnimatePopupOpacity(_popup, 0, 1, 120);

            MouseLeave += OnWidgetMouseLeave;
            _popup.MouseEnter += OnPopupMouseEnter;
            _popup.MouseLeave += OnPopupMouseLeave;
        }

        private void OnWidgetMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StartCloseTimer();
        }

        private void OnPopupMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Cancel close if mouse enters popup
            _closeTimer?.Stop();
        }

        private void OnPopupMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            StartCloseTimer();
        }

        private void StartCloseTimer()
        {
            _closeTimer?.Stop();
            _closeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1000)
            };
            _closeTimer.Tick += (s, e) =>
            {
                _closeTimer?.Stop();

                // Check if mouse is over widget or popup
                if (IsMouseOver || (_popup?.IsMouseOver ?? false))
                    return;

                if (_popup != null)
                {
                    AnimatePopupOpacity(_popup, 1, 0, 120, () =>
                    {
                        _popup?.Close();
                        _popup = null;
                    });
                }

                MouseLeave -= OnWidgetMouseLeave;
            };
            _closeTimer.Start();
        }

        private void AnimatePopupOpacity(System.Windows.Window window, double from, double to,
            double durationMs, Action? onComplete = null)
        {
            var animation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = from,
                To = to,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = new System.Windows.Media.Animation.CubicEase
                {
                    EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut
                }
            };

            if (onComplete != null)
                animation.Completed += (s, e) => onComplete();

            window.BeginAnimation(OpacityProperty, animation);
        }

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly DispatcherTimer _timer;

        // Location coordinates
        private double _latitude = 19.07;
        private double _longitude = 72.87;
        private string _city = "Loading...";

        public WeatherWidget()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                // Refresh every 15 minutes
                Interval = TimeSpan.FromMinutes(15)
            };
            _timer.Tick += async (s, e) => await FetchWeather();
            _timer.Start();

            // Detect location then fetch weather on startup
            Loaded += async (s, e) =>
            {
                var location = await TopBar.Helpers.LocationService.GetLocationAsync();
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                _city = location.City;
                System.Diagnostics.Debug.WriteLine($"Using location: {_city} ({_latitude}, {_longitude})");
                await FetchWeather();
            };
        }

        private async Task FetchWeather()
        {
            // Show cached data instantly if available
            var cache = WeatherCache.Load();
            if (cache != null)
            {
                ParseAndDisplayWidget(cache.RawJson);
                System.Diagnostics.Debug.WriteLine("Widget loaded from cache.");
            }

            // Always fetch fresh data in background
            try
            {
                var url = $"https://api.open-meteo.com/v1/forecast" +
                          $"?latitude={_latitude}&longitude={_longitude}" +
                          $"&current=temperature_2m,weathercode" +
                          $"&temperature_unit=celsius";

                var response = await _httpClient.GetStringAsync(url);

                // Save to cache
                WeatherCache.Save(response, _latitude, _longitude);

                ParseAndDisplayWidget(response);
                System.Diagnostics.Debug.WriteLine("Widget updated from API.");
            }
            catch
            {
                if (cache == null)
                    WeatherText.Text = "Weather unavailable";
            }
        }

        private void ParseAndDisplayWidget(string rawJson)
        {
            try
            {
                var json = JsonDocument.Parse(rawJson);
                var current = json.RootElement.GetProperty("current");

                double temp = current.GetProperty("temperature_2m").GetDouble();
                int code = current.GetProperty("weathercode").GetInt32();

                string condition = GetCondition(code);
                string iconFile = GetIconFile(code);
                WeatherText.Text = $"{temp:0}°C  {condition}";
                WeatherIcon.Source = new Uri(
                    $"pack://application:,,,/Assets/Weather/{iconFile}");
            }
            catch { }
        }

        private static string GetCondition(int code) => code switch
        {
            0 => "Clear Sky",
            1 => "Mostly Clear",
            2 => "Partly Cloudy",
            3 => "Overcast",
            45 or 48 => "Foggy",
            51 or 53 or 55 => "Drizzle",
            61 or 63 or 65 => "Rainy",
            71 or 73 or 75 => "Snowy",
            80 or 81 or 82 => "Showers",
            95 => "Thunderstorm",
            96 or 99 => "Hail Storm",
            _ => "Unknown"
        };

        private static string GetIconFile(int code) => code switch
        {
            0 => "clear-day.svg",
            1 => "mostly-clear-day.svg",
            2 => "partly-cloudy-day.svg",
            3 => "overcast-day.svg",
            45 or 48 => "fog-day.svg",
            51 or 53 or 55 => "drizzle.svg",
            61 or 63 or 65 => "rain.svg",
            71 or 73 or 75 => "snow.svg",
            80 or 81 or 82 => "overcast-day-rain.svg",
            95 => "thunderstorms-day.svg",
            96 or 99 => "hail.svg",
            _ => "clear-day.svg"
        };
    }
}
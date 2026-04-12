using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace TopBar
{
    public partial class WeatherPopupWindow : Window
    {
        private readonly HttpClient _httpClient = new();
        private double _latitude = 19.07;
        private double _longitude = 72.87;
        private string _city = "Unknown";

        public WeatherPopupWindow(double barHeight, double weatherWidgetLeft, double weatherWidgetWidth)
        {
            InitializeComponent();
            ContentRendered += (s, e) => ApplyWindowsStyling();

            // Hide until positioned
            Opacity = 0;

            // Position immediately in constructor before window shows
            Left = weatherWidgetLeft;
            Top = barHeight + 8;
            if (Left + Width > SystemParameters.PrimaryScreenWidth)
                Left = SystemParameters.PrimaryScreenWidth - Width - 8;

            Loaded += async (s, e) =>
            {
                var location = await TopBar.Helpers.LocationService.GetLocationAsync();
                _latitude = location.Latitude;
                _longitude = location.Longitude;
                _city = location.City;
                await FetchWeather();
            };

            // Close when mouse leaves the popup
            MouseLeave += (s, e) =>
            {
                Close();
            };
        }

        private async Task FetchWeather()
        {
            // Show cached data instantly
            var cache = TopBar.Helpers.WeatherCache.Load();
            if (cache != null)
            {
                ParseAndDisplayPopup(cache.RawJson);
                System.Diagnostics.Debug.WriteLine("Popup loaded from cache.");
            }

            try
            {
                var url = "https://api.open-meteo.com/v1/forecast" +
                          $"?latitude={_latitude}&longitude={_longitude}" +
                          "&current=temperature_2m,relative_humidity_2m,apparent_temperature," +
                          "precipitation,weathercode,cloudcover,windspeed_10m,winddirection_10m," +
                          "uv_index,visibility" +
                          "&daily=weathercode,temperature_2m_max,temperature_2m_min," +
                          "precipitation_probability_max,sunrise,sunset" +
                          "&forecast_days=6&timezone=Asia%2FKolkata";

                var response = await _httpClient.GetStringAsync(url);

                // Save to cache
                TopBar.Helpers.WeatherCache.Save(response, _latitude, _longitude);
                var json = JsonDocument.Parse(response);

                var current = json.RootElement.GetProperty("current");
                var daily = json.RootElement.GetProperty("daily");

                // Current conditions
                double temp = current.GetProperty("temperature_2m").GetDouble();
                double feelsLike = current.GetProperty("apparent_temperature").GetDouble();
                double humidity = current.GetProperty("relative_humidity_2m").GetDouble();
                double windSpeed = current.GetProperty("windspeed_10m").GetDouble();
                double windDir = current.GetProperty("winddirection_10m").GetDouble();
                double uvIndex = current.GetProperty("uv_index").GetDouble();
                double precip = current.GetProperty("precipitation").GetDouble();
                double visibility = current.GetProperty("visibility").GetDouble();
                double cloudCover = current.GetProperty("cloudcover").GetDouble();
                int code = current.GetProperty("weathercode").GetInt32();

                CityText.Text = _city;
                CurrentTemp.Text = $"{temp:0}°C";
                CurrentCondition.Text = GetCondition(code);
                FeelsLike.Text = $"Feels like {feelsLike:0}°C";
                Humidity.Text = $"{humidity:0}%";
                Wind.Text = $"{windSpeed:0} km/h {GetWindDirection(windDir)}";
                UvIndex.Text = $"{uvIndex:0} — {GetUvLevel(uvIndex)}";
                Precipitation.Text = $"{precip:0.0} mm";
                Visibility.Text = $"{visibility / 1000:0.0} km";
                CloudCover.Text = $"{cloudCover:0}%";
                CurrentIcon.Source = new Uri($"pack://application:,,,/Assets/Weather/{GetIconFile(code)}");

                // Sunrise / Sunset (today)
                var sunriseArr = daily.GetProperty("sunrise");
                var sunsetArr = daily.GetProperty("sunset");
                var sunriseTime = DateTime.Parse(sunriseArr[0].GetString()!);
                var sunsetTime = DateTime.Parse(sunsetArr[0].GetString()!);
                Sunrise.Text = sunriseTime.ToString("HH:mm");
                Sunset.Text = sunsetTime.ToString("HH:mm");

                // 6-day forecast
                var dates = daily.GetProperty("time");
                var codes = daily.GetProperty("weathercode");
                var maxTemps = daily.GetProperty("temperature_2m_max");
                var minTemps = daily.GetProperty("temperature_2m_min");
                var precipProb = daily.GetProperty("precipitation_probability_max");

                var forecasts = new List<ForecastDay>();
                for (int i = 0; i < 6; i++)
                {
                    var date = DateTime.Parse(dates[i].GetString()!);
                    int dayCode = codes[i].GetInt32();
                    forecasts.Add(new ForecastDay
                    {
                        Day = i == 0 ? "Today" : date.ToString("ddd"),
                        MaxTemp = $"{maxTemps[i].GetDouble():0}°",
                        MinTemp = $"{minTemps[i].GetDouble():0}°",
                        PrecipChance = $"💧 {precipProb[i].GetDouble():0}%",
                        IconPath = new Uri($"pack://application:,,,/Assets/Weather/{GetIconFile(dayCode)}")
                    });
                }

                ParseAndDisplayPopup(response);
                System.Diagnostics.Debug.WriteLine("Popup updated from API.");
            }
            catch (Exception ex)
            {
                if (cache == null)
                {
                    CurrentTemp.Text = "N/A";
                    CurrentCondition.Text = "Could not load weather";
                }
                System.Diagnostics.Debug.WriteLine($"Weather error: {ex.Message}");
            }
        }

        private void ParseAndDisplayPopup(string rawJson)
        {
            try
            {
                var json = JsonDocument.Parse(rawJson);
                var current = json.RootElement.GetProperty("current");
                var daily = json.RootElement.GetProperty("daily");

                double temp = current.GetProperty("temperature_2m").GetDouble();
                double feelsLike = current.GetProperty("apparent_temperature").GetDouble();
                double humidity = current.GetProperty("relative_humidity_2m").GetDouble();
                double windSpeed = current.GetProperty("windspeed_10m").GetDouble();
                double windDir = current.GetProperty("winddirection_10m").GetDouble();
                double uvIndex = current.GetProperty("uv_index").GetDouble();
                double precip = current.GetProperty("precipitation").GetDouble();
                double visibility = current.GetProperty("visibility").GetDouble();
                double cloudCover = current.GetProperty("cloudcover").GetDouble();
                int code = current.GetProperty("weathercode").GetInt32();

                CityText.Text = _city;
                CurrentTemp.Text = $"{temp:0}°C";
                CurrentCondition.Text = GetCondition(code);
                FeelsLike.Text = $"Feels like {feelsLike:0}°C";
                Humidity.Text = $"{humidity:0}%";
                Wind.Text = $"{windSpeed:0} km/h {GetWindDirection(windDir)}";
                UvIndex.Text = $"{uvIndex:0} — {GetUvLevel(uvIndex)}";
                Precipitation.Text = $"{precip:0.0} mm";
                Visibility.Text = $"{visibility / 1000:0.0} km";
                CloudCover.Text = $"{cloudCover:0}%";
                CurrentIcon.Source = new Uri(
                    $"pack://application:,,,/Assets/Weather/{GetIconFile(code)}");

                var sunriseArr = daily.GetProperty("sunrise");
                var sunsetArr = daily.GetProperty("sunset");
                var sunriseTime = DateTime.Parse(sunriseArr[0].GetString()!);
                var sunsetTime = DateTime.Parse(sunsetArr[0].GetString()!);
                Sunrise.Text = sunriseTime.ToString("HH:mm");
                Sunset.Text = sunsetTime.ToString("HH:mm");

                var dates = daily.GetProperty("time");
                var codes = daily.GetProperty("weathercode");
                var maxTemps = daily.GetProperty("temperature_2m_max");
                var minTemps = daily.GetProperty("temperature_2m_min");
                var precipProb = daily.GetProperty("precipitation_probability_max");

                var forecasts = new List<ForecastDay>();
                for (int i = 0; i < 6; i++)
                {
                    var date = DateTime.Parse(dates[i].GetString()!);
                    int dayCode = codes[i].GetInt32();
                    forecasts.Add(new ForecastDay
                    {
                        Day = i == 0 ? "Today" : date.ToString("ddd"),
                        MaxTemp = $"{maxTemps[i].GetDouble():0}°",
                        MinTemp = $"{minTemps[i].GetDouble():0}°",
                        PrecipChance = $"💧 {precipProb[i].GetDouble():0}%",
                        IconPath = new Uri(
                            $"pack://application:,,,/Assets/Weather/{GetIconFile(dayCode)}")
                    });
                }

                ForecastList.ItemsSource = forecasts;
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

        private static string GetWindDirection(double degrees) => degrees switch
        {
            < 22.5 => "N",
            < 67.5 => "NE",
            < 112.5 => "E",
            < 157.5 => "SE",
            < 202.5 => "S",
            < 247.5 => "SW",
            < 292.5 => "W",
            < 337.5 => "NW",
            _ => "N"
        };

        private static string GetUvLevel(double uv) => uv switch
        {
            < 3 => "Low",
            < 6 => "Moderate",
            < 8 => "High",
            < 11 => "Very High",
            _ => "Extreme"
        };

        private void ApplyWindowsStyling()
        {
            var handle = new WindowInteropHelper(this).Handle;
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
            int cornerPreference = 2;
            DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }

    public class ForecastDay
    {
        public string Day { get; set; } = "";
        public string MaxTemp { get; set; } = "";
        public string MinTemp { get; set; } = "";
        public string PrecipChance { get; set; } = "";
        public Uri? IconPath { get; set; }
    }
}
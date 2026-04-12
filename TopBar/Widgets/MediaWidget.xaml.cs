using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UserControl = System.Windows.Controls.UserControl;
using Windows.Media.Control;

namespace TopBar.Widgets
{
    public partial class MediaWidget : UserControl
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private GlobalSystemMediaTransportControlsSession? _session;
        private readonly DispatcherTimer _timer;

        public MediaWidget()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _timer.Tick += async (s, e) => await UpdateMedia();
            _timer.Start();

            Loaded += async (s, e) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager
                    .RequestAsync();
                _manager.CurrentSessionChanged += async (s, e) =>
                    await Dispatcher.InvokeAsync(UpdateMedia);

                await UpdateMedia();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Media init error: {ex.Message}");
            }
        }

        private async Task UpdateMedia()
        {
            try
            {
                _session = _manager?.GetCurrentSession();

                if (_session == null)
                {
                    Dispatcher.Invoke(() => Visibility = Visibility.Collapsed);
                    return;
                }

                var info = await _session.TryGetMediaPropertiesAsync();
                var playback = _session.GetPlaybackInfo();

                if (info == null || playback == null)
                {
                    Dispatcher.Invoke(() => Visibility = Visibility.Collapsed);
                    return;
                }

                var title = info.Title ?? "";
                var artist = info.Artist ?? "";
                var status = playback.PlaybackStatus;

                // Show if playing or paused, hide if stopped/closed
                bool shouldShow = status ==
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing ||
                    status ==
                    GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused;

                Dispatcher.Invoke(() =>
                {
                    Visibility = shouldShow ? Visibility.Visible : Visibility.Collapsed;

                    if (shouldShow)
                    {
                        TitleText.Text = string.IsNullOrEmpty(title) ? "Unknown" : title;
                        ArtistText.Text = string.IsNullOrEmpty(artist) ? "" : artist;

                        // Update play/pause icon
                        PlayPauseIcon.Source = status ==
                            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing
                            ? new Uri("pack://application:,,,/Assets/Media/pause.svg")
                            : new Uri("pack://application:,,,/Assets/Media/play.svg");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Media update error: {ex.Message}");
                Dispatcher.Invoke(() => Visibility = Visibility.Collapsed);
            }
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_session != null)
                    await _session.TryTogglePlayPauseAsync();
            }
            catch { }
        }

        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_session != null)
                    await _session.TrySkipPreviousAsync();
            }
            catch { }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_session != null)
                    await _session.TrySkipNextAsync();
            }
            catch { }
        }
    }
}
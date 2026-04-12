using System.ComponentModel;
using System.Windows;
using TopBar.Helpers;

namespace TopBar
{
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int nIndex);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int nIndex, int dwNewLong);

        private const double BarHeight = 40;
        private AutoHideHelper? _autoHide;
        private ReminderService? _reminderService;
        private SettingsPopup? _settingsPopup;

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsPopup != null && _settingsPopup.IsVisible)
            {
                _settingsPopup.Close();
                return;
            }

            _settingsPopup = new SettingsPopup(BarHeight, SystemParameters.PrimaryScreenWidth);

            _settingsPopup.BarHeightChanged += h =>
            {
                Height = h;
                _autoHide?.UpdateBarHeight(h);
            };

            _settingsPopup.BarOpacityChanged += o =>
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        (byte)(o * 255), 0x1C, 0x1C, 0x1C));
            };

            _settingsPopup.Closed += (s, e) => _settingsPopup = null;
            _settingsPopup.Show();
        }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Hide from Alt+Tab switcher
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(handle, -20);
            SetWindowLong(handle, -20, exStyle | 0x00000080);

            Dispatcher.BeginInvoke(() =>
            {
                AppBarHelper.RegisterBar(this, BarHeight);
                _autoHide = new AutoHideHelper(this, BarHeight);

                // Start reminder service
                _reminderService = new ReminderService();
                _reminderService.ReminderFired += OnReminderFired;

                // Date updater
                UpdateDate();
                var dateTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(1)
                };
                dateTimer.Tick += (s, e) => UpdateDate();
                dateTimer.Start();

                // Sync startup toggle with current state
                StartupMenuItem.IsChecked = StartupHelper.IsStartupEnabled();

            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void Startup_Checked(object sender, RoutedEventArgs e)
        {
            StartupHelper.EnableStartup();
        }

        private void Startup_Unchecked(object sender, RoutedEventArgs e)
        {
            StartupHelper.DisableStartup();
        }

        private void UpdateDate()
        {
            DateText.Text = DateTime.Now.ToString("dddd, MMMM d");
        }

        private void OnReminderFired(Models.Reminder reminder)
        {
            Dispatcher.Invoke(() =>
            {
                var popup = new ReminderPopupWindow(reminder, BarHeight);
                popup.Show();
            });
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            AppBarHelper.UnregisterBar(this);
        }

        private void AddReminder_Click(object sender, RoutedEventArgs e)
        {
            var form = new AddReminderWindow(_reminderService!);
            form.Show();
        }

        private void ViewReminders_Click(object sender, RoutedEventArgs e)
        {
            var window = new RemindersListWindow(_reminderService!);
            window.Show();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
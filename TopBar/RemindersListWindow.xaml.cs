using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TopBar.Helpers;
using TopBar.Models;
using MessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;

namespace TopBar
{
    public partial class RemindersListWindow : Window
    {
        private readonly ReminderService _service;

        public RemindersListWindow(ReminderService service)
        {
            InitializeComponent();
            _service = service;
            ContentRendered += (s, e) => ApplyWindowsStyling();
            LoadReminders();
        }

        private void LoadReminders()
        {
            var all = _service.GetAll()
                              .Where(r => r.IsActive)
                              .OrderBy(r => r.DateTime)
                              .Select(ReminderViewModel.FromReminder)
                              .ToList();

            SubtitleText.Text = $"{all.Count} active reminder{(all.Count == 1 ? "" : "s")}";

            if (all.Count == 0)
            {
                RemindersList.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                RemindersList.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                RemindersList.ItemsSource = all;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var result = MessageBox.Show(
                    "Delete this reminder?", "TopBar",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _service.Remove(id);
                    LoadReminders();
                }
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid id)
            {
                var reminder = _service.GetAll().FirstOrDefault(r => r.Id == id);
                if (reminder == null) return;

                var editWindow = new AddReminderWindow(_service, reminder);
                editWindow.Closed += (s, e) => LoadReminders();
                editWindow.Show();
            }
        }

        private void ApplyWindowsStyling()
        {
            var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));
            int cornerPreference = 3;
            DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));
            int backdropType = 2;
            DwmSetWindowAttribute(handle, 38, ref backdropType, sizeof(int));
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }
    }
}
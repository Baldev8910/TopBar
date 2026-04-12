using System;
using System.Windows;
using TopBar.Helpers;
using TopBar.Models;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;

namespace TopBar
{
    public partial class AddReminderWindow : Window
    {

        private int _hour = 9;
        private int _minute = 0;

        private readonly ReminderService _service;

        private Reminder? _editingReminder;

        public AddReminderWindow(ReminderService service, Reminder? editingReminder = null)
        {
            InitializeComponent();
            _service = service;
            _editingReminder = editingReminder;
            ContentRendered += (s, e) => ApplyWindowsStyling();

            if (editingReminder != null)
            {
                // Pre-fill fields for editing
                TitleBox.Text = editingReminder.Title;
                DatePick.SelectedDate = editingReminder.DateTime.Date;
                _hour = editingReminder.DateTime.Hour;
                _minute = editingReminder.DateTime.Minute;
                HourText.Text = _hour.ToString("D2");
                MinuteText.Text = _minute.ToString("D2");
                RepeatBox.SelectedIndex = editingReminder.Repeat switch
                {
                    RepeatType.Daily => 1,
                    RepeatType.Weekly => 2,
                    RepeatType.Monthly => 3,
                    _ => 0
                };
                Title = "Edit Reminder";
            }
            else
            {
                DatePick.SelectedDate = DateTime.Today;
            }
        }

        private void HourBorder_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int steps = Math.Abs(e.Delta) / 120;
            for (int i = 0; i < steps; i++)
            {
                if (e.Delta > 0) HourUp_Click(sender, e);
                else HourDown_Click(sender, e);
            }
        }

        private void MinuteBorder_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int steps = Math.Abs(e.Delta) / 120;
            for (int i = 0; i < steps; i++)
            {
                if (e.Delta > 0) MinuteUp_Click(sender, e);
                else MinuteDown_Click(sender, e);
            }
        }

        private void HourUp_Click(object sender, RoutedEventArgs e)
        {
            _hour = (_hour + 1) % 24;
            HourText.Text = _hour.ToString("D2");
        }

        private void HourDown_Click(object sender, RoutedEventArgs e)
        {
            _hour = (_hour - 1 + 24) % 24;
            HourText.Text = _hour.ToString("D2");
        }

        private void MinuteUp_Click(object sender, RoutedEventArgs e)
        {
            _minute = (_minute + 1) % 60;
            MinuteText.Text = _minute.ToString("D2");
        }

        private void MinuteDown_Click(object sender, RoutedEventArgs e)
        {
            _minute = (_minute - 1 + 60) % 60;
            MinuteText.Text = _minute.ToString("D2");
        }

        private void ApplyWindowsStyling()
        {
            var handle = new WindowInteropHelper(this).Handle;

            // Enable dark mode title bar
            int darkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref darkMode, sizeof(int));

            // Enable rounded corners
            int cornerPreference = 3; // DWMWCP_ROUND
            DwmSetWindowAttribute(handle, 33, ref cornerPreference, sizeof(int));

            // Apply Mica backdrop
            int backdropType = 2;
            DwmSetWindowAttribute(handle, 38, ref backdropType, sizeof(int));

            // Extend frame into client area so Mica covers whole window
            var margins = new MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
            DwmExtendFrameIntoClientArea(handle, ref margins);
        }
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [StructLayout(LayoutKind.Sequential)]
        
        private struct MARGINS
        {
            public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validate title
            if (string.IsNullOrWhiteSpace(TitleBox.Text))
            {
                MessageBox.Show("Please enter a title.",
                    "TopBar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate date
            if (DatePick.SelectedDate == null)
            {
                MessageBox.Show("Please select a date.",
                    "TopBar", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate time
            var time = new TimeSpan(_hour, _minute, 0);

            // Build reminder
            var repeat = RepeatBox.SelectedIndex switch
            {
                1 => RepeatType.Daily,
                2 => RepeatType.Weekly,
                3 => RepeatType.Monthly,
                _ => RepeatType.OneTime
            };

            var reminder = new Reminder
            {
                Title = TitleBox.Text.Trim(),
                DateTime = DatePick.SelectedDate.Value.Add(time),
                Repeat = repeat
            };

            if (_editingReminder != null)
            {
                _service.Remove(_editingReminder.Id);
            }
            _service.Add(reminder);

            MessageBox.Show("Reminder saved!",
                "TopBar", MessageBoxButton.OK, MessageBoxImage.Information);

            Close();
        }
    }
}
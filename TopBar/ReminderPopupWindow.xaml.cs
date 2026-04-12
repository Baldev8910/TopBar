using System;
using System.Windows;
using System.Windows.Threading;
using TopBar.Models;

namespace TopBar
{
    public partial class ReminderPopupWindow : Window
    {
        private readonly DispatcherTimer _timer;
        private int _secondsLeft = 60;

        public ReminderPopupWindow(Reminder reminder, double barHeight)
        {
            InitializeComponent();

            // Set content
            TitleText.Text = reminder.Title;
            TimeText.Text = reminder.DateTime.ToString("HH:mm  —  ddd, MMM d");

            // Position — centered, just below the bar
            Loaded += (s, e) =>
            {
                var screenWidth = SystemParameters.PrimaryScreenWidth;
                Left = (screenWidth - ActualWidth) / 2;
                Top = barHeight + 8;
            };

            // Auto-dismiss countdown
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
            DismissText.MouseLeftButtonUp += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _secondsLeft--;
            CountdownText.Text = $"Dismissing in {_secondsLeft}s";

            if (_secondsLeft <= 0)
            {
                _timer.Stop();
                Close();
            }
        }

        private void Dismiss_Click(object sender, RoutedEventArgs e)
        {
            _timer.Stop();
            Close();
        }
    }
}
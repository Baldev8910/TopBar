using System;
using System.Windows.Controls;
using UserControl = System.Windows.Controls.UserControl;
using System.Windows.Threading;

namespace TopBar.Widgets
{
    public partial class ClockWidget : UserControl
    {
        private readonly DispatcherTimer _timer;

        public ClockWidget()
        {
            InitializeComponent();

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // Set immediately so there's no blank on startup
            UpdateClock();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            var now = DateTime.Now;
            TimeText.Text = now.ToString("HH:mm");       // 12-hour format
        }
    }
}
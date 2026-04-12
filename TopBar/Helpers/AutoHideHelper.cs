using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace TopBar.Helpers
{
    public class AutoHideHelper
    {
        private readonly Window _window;
        private double _barHeight;
        private readonly DispatcherTimer _checkTimer;
        private readonly DispatcherTimer _hideDelayTimer;
        private bool _isVisible;
        private const double SliverHeight = 2;
        private const int HideDelayMs = 300;

        // Animation duration — tweak this to taste
        private const double ShowDurationMs = 150;
        private const double HideDurationMs = 200;

        public AutoHideHelper(Window window, double barHeight)
        {
            _window = window;
            _barHeight = barHeight;
            _isVisible = false;

            // Start hidden
            _window.Top = -(_barHeight - SliverHeight);

            // Mouse checker at ~60fps
            _checkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _checkTimer.Tick += CheckTimer_Tick;
            _checkTimer.Start();

            // Delay before hiding
            _hideDelayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(HideDelayMs)
            };
            _hideDelayTimer.Tick += HideDelayTimer_Tick;
        }
        public void UpdateBarHeight(double newHeight)
        {
            _barHeight = newHeight;
        }

        private void CheckTimer_Tick(object? sender, EventArgs e)
        {
            double mouseY = System.Windows.Forms.Cursor.Position.Y;
            double mouseX = System.Windows.Forms.Cursor.Position.X;

            bool mouseInXRange = mouseX >= _window.Left &&
                                 mouseX <= _window.Left + _window.Width;

            bool mouseAtTopEdge = mouseY <= SliverHeight && mouseInXRange;
            bool mouseInsideBar = mouseY <= _barHeight && mouseInXRange;

            if (mouseAtTopEdge && !_isVisible)
            {
                _hideDelayTimer.Stop();
                _isVisible = true;
                AnimateTo(0, ShowDurationMs, new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                });
            }
            else if (!mouseInsideBar && _isVisible)
            {
                if (!_hideDelayTimer.IsEnabled)
                    _hideDelayTimer.Start();
            }
            else if (mouseInsideBar && _isVisible)
            {
                _hideDelayTimer.Stop();
            }
        }

        private void HideDelayTimer_Tick(object? sender, EventArgs e)
        {
            _hideDelayTimer.Stop();
            _isVisible = false;
            AnimateTo(-(_barHeight - SliverHeight), HideDurationMs, new CubicEase
            {
                EasingMode = EasingMode.EaseIn
            });
        }

        private void AnimateTo(double targetTop, double durationMs, IEasingFunction easing)
        {
            var animation = new DoubleAnimation
            {
                To = targetTop,
                Duration = TimeSpan.FromMilliseconds(durationMs),
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            };

            animation.Completed += (s, e) =>
            {
                _window.Top = targetTop;
            };

            // WPF animates the Top property using GPU
            _window.BeginAnimation(Window.TopProperty, animation);
        }
    }
}
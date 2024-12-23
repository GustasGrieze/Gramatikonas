using Microsoft.AspNetCore.Components;
using System.Timers;

namespace lithuanian_language_learning_tool.Components
{
    public partial class Timer : ComponentBase, IDisposable
    {
        [Parameter]
        public int SecondsToRun { get; set; }
        private System.Timers.Timer _timer = null!;
        private int _secondsToRun;
        protected string Time { get; set; } = "00:00";

        [Parameter]
        public EventCallback TimerOut { get; set; }

        private bool _isResetting = true;
        private string _progressBarClass => _isResetting ? "progress-bar-reset" : "progress-bar";

        private async Task StartTimer()
        {
            _secondsToRun = SecondsToRun;
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += OnTimedEvent;
            _timer.AutoReset = true;

            Time = TimeSpan.FromSeconds(_secondsToRun).ToString(@"mm\:ss");
            _isResetting = true;
            StateHasChanged();

            await Task.Delay(50);

            // Start the animation
            _isResetting = false;
            StateHasChanged();

            _timer.Start();
        }

        private async void OnTimedEvent(object? sender, ElapsedEventArgs e)
        {
            _secondsToRun--;

            await InvokeAsync(() =>
            {
                Time = TimeSpan.FromSeconds(_secondsToRun).ToString(@"mm\:ss");
                StateHasChanged();
            });

            if (_secondsToRun <= 0)
            {
                _timer.Stop();
                await InvokeAsync(() =>
                    TimerOut.InvokeAsync()
                );
            }
        }

        public async Task ResetTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                await StartTimer();
            }
        }

        public async Task StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
        }

        public void Dispose()
        {
            if (_timer != null)
                _timer.Dispose();
        }
    }
}
namespace FileCraft.Shared.Helpers
{
    public class Debouncer : IDisposable
    {
        public const int DefaultDelay = 300;
        private readonly Timer _timer;
        private readonly Action _action;

        public Debouncer(Action action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _timer = new Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Debounce(int delay = DefaultDelay)
        {
            _timer.Change(delay, Timeout.Infinite);
        }

        private void OnTimerElapsed(object? state)
        {
            _action();
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
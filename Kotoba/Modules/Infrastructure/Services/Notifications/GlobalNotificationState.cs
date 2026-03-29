namespace Kotoba.Modules.Infrastructure.Services.Notifications
{
    public class GlobalNotificationState
    {
        private int _count;

        public int Count => _count;

        public event Action? OnChange;

        public void Set(int count)
        {
            _count = count;
            OnChange?.Invoke();
        }

        public void Increment()
        {
            _count++;
            OnChange?.Invoke();
        }

        public void Reset()
        {
            _count = 0;
            OnChange?.Invoke();
        }
    }
}

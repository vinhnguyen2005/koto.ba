namespace Kotoba.Modules.Infrastructure.Services.Notifications
{
    public class ChatNotificationState
    {
        private readonly HashSet<string> _unread = new();

        public event Action? OnChange;

        public void Add(string conversationId)
        {
            if (_unread.Add(conversationId))
                OnChange?.Invoke();
        }

        public void Remove(string conversationId)
        {
            if (_unread.Remove(conversationId))
                OnChange?.Invoke();
        }

        public bool Contains(string conversationId)
            => _unread.Contains(conversationId);
    }
}

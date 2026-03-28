namespace Kotoba.Modules.Infrastructure.Services.Notifications
{
    public class ChatNotificationState
    {
        private readonly HashSet<string> _unread = new();
        private readonly Dictionary<string, string> _lastSender = new();
        public string? ActiveConversationId { get; set; }

        public event Action? OnChange;

        public void Add(string conversationId, string senderId)
        {
            _lastSender[conversationId] = senderId;

            if (_unread.Add(conversationId))
                OnChange?.Invoke();
        }

        public bool Contains(string conversationId)
        => _unread.Contains(conversationId);

        public string? GetSender(string conversationId)
            => _lastSender.TryGetValue(conversationId, out var s) ? s : null;

        public void Remove(string conversationId)
        {
            if (_unread.Remove(conversationId))
                OnChange?.Invoke();
        }
    }
}

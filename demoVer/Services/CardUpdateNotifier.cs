namespace demoVer.Services
{
    public class CardUpdateNotifier
    {
        private readonly Dictionary<string, Func<Task>> _listeners = new();

        public void Register(string cardId, Func<Task> callback)
        {
            if (!_listeners.ContainsKey(cardId))
                _listeners[cardId] = callback;
        }

        public void Unregister(string cardId)
        {
            _listeners.Remove(cardId);
        }

        public void Notify(string cardId)
        {
            if (_listeners.TryGetValue(cardId, out var callback))
            {
                callback?.Invoke();
            }
        }
    }
    
}
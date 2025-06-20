using System.Collections.Concurrent;

namespace AuthECAPI.Services.ProgressStore
{

    public class InMemoryProgressStore : IProgressStore
    {
        private readonly ConcurrentDictionary<string, int> _store = new();

        public void SetProgress(string userId, int percent)
        {
            _store[userId] = percent;
        }

        public int GetProgress(string userId)
        {
            return _store.TryGetValue(userId, out var value) ? value : 0;
        }
    }
}

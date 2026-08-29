using System.Collections.Concurrent;

namespace Server.Keys
{
    internal class KeyManager
    {
        private readonly ConcurrentDictionary<string, KeyToken> keys = [];

        private Task cleanupTask = Task.CompletedTask;


        public void RegisterKey(KeyToken key)
        {
            keys.TryAdd(key.Hash, key);

            if (cleanupTask.IsCompleted)
                cleanupTask ??= Cleanup();
        }


        private async Task Cleanup()
        {
            while (!keys.IsEmpty)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                DateTime now = DateTime.UtcNow;

                foreach (var key in from KeyToken key in keys.Values
                                    where now >= key.ExpiresAt
                                    select key)
                {
                    keys.Remove(key.Hash, out _);
                }
            }
        }

        public bool TryUseKey(string key)
        {
            lock (this)
            {
                if (keys.Remove(key, out KeyToken? token))
                {
                    return token.ExpiresAt >= DateTime.UtcNow;
                }

                return false;
            }
        }
    }
}

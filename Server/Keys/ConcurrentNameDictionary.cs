using System.Collections.Concurrent;

namespace Server.Keys
{
    internal class ConcurrentNameDictionary<TValue>(Func<string>? generator = null, Predicate<string>? validator = null) : ConcurrentDictionary<string, TValue>
    {
        private readonly Func<string>? generator = generator;
        private readonly Predicate<string> validator = validator ?? ((_) => true);


        public bool TryAddGenerated(out string key, TValue value)
        {
            if (generator is null)
            {
                key = string.Empty;
                return false;
            }

            lock (this)
            {
                do
                {
                    key = generator();
                }
                while (!validator(key) || ContainsKey(key));

                return TryAdd(key, value);
            }
        }

        public bool TryRename(string from, string to)
        {
            lock (this)
            {
                if (ContainsKey(to) || !TryRemove(from, out TValue? value))
                    return false;

                if (!TryAdd(to, value))
                {
                    TryAdd(from, value);
                    return false;
                }

                return true;
            }
        }
    }
}

namespace Server.Keys
{
    internal class NameManager
    {
        private readonly HashSet<string> names = [];
        private readonly Func<string>? generator;

        public NameManager(Func<string>? generator)
        {
            this.generator = generator;
        }

        public bool TryAddName(string name)
        {
            lock (this)
            {
                return names.Add(name);
            }
        }

        public bool TryRemoveName(string name)
        {
            lock(this)
            {
                return names.Remove(name);
            }
        }

        public bool TryRename(string from, string to)
        {
            lock (this)
            {
                if (!names.Contains(from) || names.Contains(to))
                    return false;

                names.Remove(from);
                names.Add(to);
                return true;
            }
        }

        public bool TryGenerate(out string result)
        {
            if (generator is null)
            {
                result = string.Empty;
                return false;
            }

            lock (this)
            {
                do
                {
                    result = generator();
                }
                while (names.Contains(result));

                names.Add(result);
                return true;
            }
        }
    }
}

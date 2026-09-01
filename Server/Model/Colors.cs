namespace Server.Model
{
    internal static class Colors
    {
        public static readonly int FALLBACK = 0x808080;

        public static readonly int[] DEFAULT = 
        [
            0x4C8DFF,
            0xF59E42,
            0x4CAF7A,
            0xE56B6F,
            0x9B7EDE,
            0x45B8C8,
            0xD6B84C,
            0xD878A5,
        ];

        public static int GetNextDefault(IEnumerable<int> inUse)
        {
            HashSet<int> set = [.. inUse];

            for (int i = 0; i < DEFAULT.Length; i++)
            {
                if (!set.Contains(DEFAULT[i]))
                    return DEFAULT[i];
            }

            return FALLBACK;
        }
    }
}

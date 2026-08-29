namespace Server.Keys
{
    internal class KeyToken
    {
        public string Hash { get; init; }
        public DateTime ExpiresAt { get; init; }

        public KeyToken(string hash, TimeSpan lifetime)
        {
            Hash = hash;
            ExpiresAt = DateTime.UtcNow + lifetime;
        }
    }
}

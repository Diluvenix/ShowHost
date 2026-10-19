namespace Network.Packets.Games._57
{
    public class _57_GamePacket
    {
        public string[]? Entries = default;
        public Player[]? Players = default;

        public record class Player(string Username, int Points, string Value);
    }
}

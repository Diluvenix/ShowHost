namespace Network.Packets.Games._57
{
    public class _57_GamePacket
    {
        public string[]? Entries { get; set; }
        public Player[]? Players { get; set; }

        public record class Player(string Username, int Points, string Value);
    }
}

namespace Network.Packets.Games._57
{
    public class _57_PingPacket
    {
        public Player[] Players { get; set; } = [];

        public record class Player(string Username, int Ping);
    }
}

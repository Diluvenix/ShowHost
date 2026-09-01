namespace Network.Packets
{
    public class PingPacket
    {
        public Player[] Players { get; set; } = [];

        public record class Player(string Username, int Ping);
    }
}

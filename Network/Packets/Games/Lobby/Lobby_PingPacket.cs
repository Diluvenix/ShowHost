namespace Network.Packets.Games.Lobby
{
    public class Lobby_PingPacket
    {
        public Player[] Players { get; set; } = [];

        public record class Player(string Username, int Ping);
    }
}

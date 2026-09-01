namespace Network.Packets.Games.Lobby
{
    public class Lobby_PlayerListPacket
    {
        public Player[] Players { get; set; } = [];

        public record class Player(string Username, int Ping, PlayerRole PlayerRole);

        public enum PlayerRole
        {
            Player,
            Moderator
        }
    }
}

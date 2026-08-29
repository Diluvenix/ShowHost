namespace Network.Packets
{
    public class LobbyPacket
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

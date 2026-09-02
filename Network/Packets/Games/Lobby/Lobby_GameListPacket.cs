namespace Network.Packets.Games.Lobby
{
    public class Lobby_GameListPacket
    {
        public Game[] Games { get; set; } = [];

        public record class Game(string Type, string Name, int PlayersMax, int PlayersCurrent, GameStatus Status, bool CanJoin);

        public enum GameStatus
        {
            Preparing,
            Running,
        }
    }
}

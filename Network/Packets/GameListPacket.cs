namespace Network.Packets
{
    public class GameListPacket
    {
        public Game[] Games { get; set; } = [];

        public record class Game(string Type, string Name, int PlayersMax, int PlayersCurrent, GameStatus GameStatus);

        public enum GameStatus
        {
            Preparing,
            Running,
        }
    }
}

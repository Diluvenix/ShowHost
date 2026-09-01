namespace Network.Packets.Games.Lobby
{
    public class Lobby_GameCreatePacket
    {
        public GameType Type { get; set; }

        public enum GameType
        {
            NONE,
            _57,
        }
    }
}

namespace Network.Packets
{
    public class CreateGamePacket
    {
        public GameType Game { get; set; }


        public enum GameType
        {
            NONE,
            _57,
        }
    }
}

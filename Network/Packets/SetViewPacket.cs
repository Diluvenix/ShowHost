namespace Network.Packets
{
    public class SetViewPacket
    {
        public ViewType View { get; set; }

        public enum ViewType
        {
            Connect,
            Lobby,
            _57_Lobby
        }
    }
}

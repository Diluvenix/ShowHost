namespace Network.Packets
{
    public class ConnectPacket
    {
        public string Username { get; set; } = "";
        public ConnectMode Mode { get; set; }
        public string? Secret { get; set; }

        public enum ConnectMode
        {
            NONE,
            Player,
            Moderator,
            Recovery
        }
    }

}

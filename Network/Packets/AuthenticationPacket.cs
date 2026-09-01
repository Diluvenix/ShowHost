namespace Network.Packets
{
    public class AuthenticationPacket
    {
        public string Username { get; set; } = "";
        public AuthenticationType Type { get; set; }
        public string? Secret { get; set; }

        public enum AuthenticationType
        {
            NONE,
            Player,
            Moderator,
            Recovery
        }
    }
}

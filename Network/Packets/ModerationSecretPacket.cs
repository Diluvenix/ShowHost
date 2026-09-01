namespace Network.Packets
{
    public class ModerationSecretPacket
    {
        public string? Target { get; set; }

        public string? Secret { get; set; }
        public SecretType Type { get; set; }

        public enum SecretType
        {
            Recovery,
            Moderator
        }
    }
}

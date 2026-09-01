namespace Network.Packets
{
    public class ModerationPacket
    {
        public string Target { get; set; } = string.Empty;
        public ModerationAction Action { get; set; }

        public enum ModerationAction
        {
            Kick,
            Delete
        }
    }
}

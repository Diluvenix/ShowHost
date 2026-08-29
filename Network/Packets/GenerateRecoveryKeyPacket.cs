namespace Network.Packets
{
    public class GenerateRecoveryKeyPacket
    {
        public string Username { get; set; } = "";
        public string? Key { get; set; }
    }
}

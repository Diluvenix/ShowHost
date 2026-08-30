namespace Network.Packets
{
    public class GenerateRecoveryKeyPacket
    {
        public string Target { get; set; } = "";
        public string? Key { get; set; }
    }
}

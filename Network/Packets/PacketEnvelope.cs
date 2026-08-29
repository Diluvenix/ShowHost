using System.Text.Json;

namespace Network.Packets
{
    internal class PacketEnvelope
    {
        public string Type { get; set; } = "";
        public JsonElement Data { get; set; }
    }
}

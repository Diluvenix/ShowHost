namespace Network.Packets.Games._57
{
    public class _57_LobbyPacket
    {
        public string Name { get; set; } = string.Empty;
        public int PlayersMax { get; set; }
        public int PlayersCurrent { get; set; }
    }
}

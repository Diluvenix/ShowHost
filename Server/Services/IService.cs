using Network.Packets;
using Server.Model;

namespace Server.Services
{
    internal interface IService : IDisposable
    {
        public string Type { get; }
        public string Name { get; }
        public int PlayersMax { get; }
        public int PlayersCurrent { get; }
        public GameListPacket.GameStatus Status { get; }

        public Task AddPlayerAsync(Player player);
        public Task RemovePlayerAsync(Player player);
        public Task RecoverAsync(Player player);
        public Task HandleAsync<T>(T packet, Player sender);
    }
}

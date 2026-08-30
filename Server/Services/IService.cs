using Server.Model;

namespace Server.Services
{
    internal interface IService : IDisposable
    {
        public Task AddPlayerAsync(Player player);
        public Task RemovePlayerAsync(Player player);
        public Task HandleAsync<T>(T packet, Player sender);
        public Task RecoverAsync(Player player);
    }
}

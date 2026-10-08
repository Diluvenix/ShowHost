using Network.Packets.Games.Lobby;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal abstract class ServiceBase : IDisposable
    {
        public string Type { get; }
        public string Name { get; private protected set; }
        public int PlayersMax { get; private protected set; }
        public abstract int PlayersCount { get; }
        public Lobby_GameListPacket.GameStatus Status { get; private protected set; } = Lobby_GameListPacket.GameStatus.Preparing;
        private protected readonly Dictionary<string, Player> clients = [];

        private protected ILogger logger;

        public ServiceBase(string type)
        {
            Context.Services.TryAddGenerated(out string name, this);

            Type = type;
            Name = name;
            logger = Log.ForContext("SourceContext", Type).ForContext(nameof(Name), Name);
            logger.Information("New game created");
        }
        public ServiceBase(string type, string name)
        {
            Type = type;
            Name = name;
            logger = Log.ForContext("SourceContext", Type).ForContext(nameof(Name), Name);
            logger.Information("New game created");
        }

        public async Task<bool> TryAddPlayerAsync(Player player, CancellationToken ct)
        {
            lock(this)
            {
                if (!CanPlayerJoin(player))
                    return false;

                clients[player.Username] = player;
                logger.ForContext("Player", player.Username).Information("Player joined");

                try
                {
                    OnPlayerAddedAsync(player, ct).Wait(ct);
                }
                catch (Exception) 
                {
                    return false;
                }
            }

            return true;
        }
        public async Task RemovePlayerAsync(Player player, CancellationToken ct)
        {
            clients.Remove(player.Username);
            logger.ForContext("Player", player.Username).Information("Player left");

            await OnPlayerRemovedAsync(player, ct);
        }
        public Task RecoverPlayerAsync(Player player, CancellationToken ct)
            => OnPlayerRecoveredAsync(player, ct);

        public abstract bool CanPlayerJoin(Player player);
        private protected abstract Task OnPlayerAddedAsync(Player player, CancellationToken ct);
        private protected abstract Task OnPlayerRemovedAsync(Player player, CancellationToken ct);
        private protected abstract Task OnPlayerRecoveredAsync(Player player, CancellationToken ct);
        public abstract Task HandleAsync<T>(T packet, Player sender, CancellationToken ct);
        public abstract void Dispose();
    }
}

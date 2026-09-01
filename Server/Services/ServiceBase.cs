using Network.Packets;
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
        public int PlayersCurrent { get; private protected set; }
        public Lobby_GameListPacket.GameStatus Status { get; private protected set; } = Lobby_GameListPacket.GameStatus.Preparing;
        private protected readonly Dictionary<string, Player> clients = [];

        private Task pingTask = Task.CompletedTask;
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

        public async Task AddPlayerAsync(Player player, CancellationToken ct)
        {
            clients[player.Username] = player;
            logger.ForContext("Player", player.Username).Information("Player joined");
            if (pingTask.IsCompleted)
                pingTask = SendPingAsync(ct);

            await OnPlayerAddedAsync(player, ct);
        }
        public async Task RemovePlayerAsync(Player player, CancellationToken ct)
        {
            clients.Remove(player.Username);
            logger.ForContext("Player", player.Username).Information("Player left");

            await OnPlayerRemovedAsync(player, ct);
        }
        public async Task RecoverPlayerAsync(Player player, CancellationToken ct)
        {
            if (pingTask.IsCompleted)
                pingTask = SendPingAsync(ct);

            await OnPlayerRecoveredAsync(player, ct);
        }

        private protected abstract Task OnPlayerAddedAsync(Player player, CancellationToken ct);
        private protected abstract Task OnPlayerRemovedAsync(Player player, CancellationToken ct);
        private protected abstract Task OnPlayerRecoveredAsync(Player player, CancellationToken ct);
        public abstract Task HandleAsync<T>(T packet, Player sender, CancellationToken ct);
        public abstract void Dispose();


        private async Task SendPingAsync(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && clients.Values.Any(c => c.IsConnected))
            {
                PingPacket packet = new()
                {
                    Players = [.. clients.Values.Select(p => new PingPacket.Player(p.Username, p.PingMS))]
                };

                await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
                {
                    await p.SendPacketAsync(packet, ct);
                });
            }
        }
    }
}

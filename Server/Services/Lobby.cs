using Network.Packets;
using Network.Packets.Games.Lobby;
using Server.Model;

namespace Server.Services
{
    internal class Lobby : ServiceBase
    {
        private Task ScheduledGamesUpdateTask;

        public Lobby() : base("Lobby", "Lobby")
        {
            Status = Lobby_GameListPacket.GameStatus.Running;
            ScheduledGamesUpdateTask = Task.CompletedTask;
        }

        public override void Dispose() { }

        private protected override async Task OnPlayerAddedAsync(Player player, CancellationToken ct)
        {
            PlayersCurrent = clients.Count;

            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby }, ct);

            await SendPlayersUpdateAsync(ct);
            await SendGamesUpdateAsync(ct);

            if (ScheduledGamesUpdateTask.IsCompleted)
                ScheduledGamesUpdateTask = ScheduledGamesUpdateAsync(Context.Cts.Token);
        }
        private protected override async Task OnPlayerRemovedAsync(Player player, CancellationToken ct)
        {
            PlayersCurrent = clients.Count;

            await SendPlayersUpdateAsync(ct);
        }
        private protected override async Task OnPlayerRecoveredAsync(Player player, CancellationToken ct)
        {
            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby }, ct);
            await SendPlayersUpdateAsync(ct);
            await SendGamesUpdateAsync(ct);

            if (ScheduledGamesUpdateTask.IsCompleted)
                ScheduledGamesUpdateTask = ScheduledGamesUpdateAsync(Context.Cts.Token);
        }

        private async Task ScheduledGamesUpdateAsync(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(5));

            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && clients.Values.Any(p => p.IsConnected))
            {
                await SendGamesUpdateAsync(ct);
            }
        }

        public override async Task HandleAsync<T>(T packet, Player sender, CancellationToken ct)
        {
            switch (packet)
            {
                case Lobby_GameCreatePacket createGamePacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        logger.ForContext("Actor", sender.Username).Warning("Denied access to CreateGame Method");
                        return;
                    }
                    await CreateGame(createGamePacket, sender, ct);
                    break;
                case Lobby_GameJoinPacket joinGamePacket:
                    if (!Context.Services.TryGetValue(joinGamePacket.GameName, out ServiceBase? service)) 
                    {
                        logger.ForContext("Actor", sender.Username).ForContext("Game", joinGamePacket.GameName).Warning("Game is unknown"); ;
                        break;
                    }
                    await sender.SetServiceAsync(service, ct);
                    break;
            }
        }

        private static async Task CreateGame(Lobby_GameCreatePacket createGamePacket, Player sender, CancellationToken ct)
        {
            ServiceBase newGame;
            switch (createGamePacket.Type)
            {
                case Lobby_GameCreatePacket.GameType._57:
                    newGame = new _57();
                    break;
                default:
                    return;
            }

            await sender.SetServiceAsync(newGame, ct);
        }

        private async Task SendPlayersUpdateAsync(CancellationToken ct)
        {
            Lobby_PlayerListPacket packet = new()
            {
                Players = [.. clients.Values.Select(p => new Lobby_PlayerListPacket.Player(
                    p.Username,
                    p.PingMS,
                    p.Role switch { PlayerRole.Moderator => Lobby_PlayerListPacket.PlayerRole.Moderator, _ => Lobby_PlayerListPacket.PlayerRole.Player }
                ))]
            };

            await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
            {
                await p.SendPacketAsync(packet, ct);
            });
        }

        private async Task SendGamesUpdateAsync(CancellationToken ct)
        {
            Lobby_GameListPacket packet = new()
            {
                Games = [.. Context.Services.Values.Select(s => new Lobby_GameListPacket.Game(
                    s.Type,
                    s.Name,
                    s.PlayersMax,
                    s.PlayersCurrent,
                    s.Status
                ))]
            };

            await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
            {
                await p.SendPacketAsync(packet, ct);
            });
        }
    }
}

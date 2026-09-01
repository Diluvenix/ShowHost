using Network.Packets;
using Network.Packets.Games.Lobby;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal class Lobby : IService
    {
        public string Type => "Lobby";
        public string Name => "Lobby";
        public int PlayersMax => 0;
        public int PlayersCurrent => players.Count;
        public Lobby_GameListPacket.GameStatus Status => Lobby_GameListPacket.GameStatus.Running;

        private readonly Dictionary<string, Player> players = [];
        private Task scheduledTask;

        private readonly ILogger logger = Log.ForContext("SourceContext", "Lobby");

        public Lobby()
        {
            scheduledTask = Task.CompletedTask;
        }

        public void Dispose() { }

        public async Task AddPlayerAsync(Player player, CancellationToken ct)
        {
            players.Add(player.Username, player);
            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby }, ct);

            await player.SendPacketAsync(new Lobby_PlayerListPacket()
            {
                Players = [.. players.Values.Select(p => new Lobby_PlayerListPacket.Player(
                    p.Username,
                    p.PingMS,
                    p.Role switch { PlayerRole.Moderator => Lobby_PlayerListPacket.PlayerRole.Moderator, _ => Lobby_PlayerListPacket.PlayerRole.Player }
                ))]
            }, ct);
            await player.SendPacketAsync(new Lobby_GameListPacket()
            {
                Games = [.. ServerContext.Services.Values.Select(s => new Lobby_GameListPacket.Game(
                    s.Type,
                    s.Name,
                    s.PlayersMax,
                    s.PlayersCurrent,
                    s.Status
                ))]
            }, ct);
            logger.ForContext("Player", player.Username).Information("Player joined");

            if (scheduledTask.IsCompleted)
                scheduledTask = ScheduledTasks(ServerContext.Cts.Token);
            await SendPlayerUpdateAsync(ct);
        }
        public async Task RemovePlayerAsync(Player player, CancellationToken ct)
        {
            players.Remove(player.Username);
            logger.ForContext("Player", player.Username).Debug("Player left");
            await SendPlayerUpdateAsync(ct);
        }
        public async Task RecoverAsync(Player player, CancellationToken ct)
        {
            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby }, ct);
            await player.SendPacketAsync(new Lobby_PlayerListPacket()
            {
                Players = [.. players.Values.Select(p => new Lobby_PlayerListPacket.Player(
                    p.Username,
                    p.PingMS,
                    p.Role switch { PlayerRole.Moderator => Lobby_PlayerListPacket.PlayerRole.Moderator, _ => Lobby_PlayerListPacket.PlayerRole.Player }
                ))]
            }, ct);
            await player.SendPacketAsync(new Lobby_GameListPacket()
            {
                Games = [.. ServerContext.Services.Values.Select(s => new Lobby_GameListPacket.Game(
                    s.Type,
                    s.Name,
                    s.PlayersMax,
                    s.PlayersCurrent,
                    s.Status
                ))]
            }, ct);
        }

        private async Task ScheduledTasks(CancellationToken ct)
        {
            Task[] tasks = 
            [
                ScheduledPlayerUpdate(ct),
                ScheduledGameUpdate(ct)
            ];

            await Task.WhenAll(tasks).WaitAsync(ct);
        }

        private async Task ScheduledPlayerUpdate(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            try
            {
                while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && players.Count > 0)
                {
                    PingPacket packet = new()
                    {
                        Players = [.. players.Values.Select(p => new PingPacket.Player(p.Username, p.PingMS))]
                    };

                    await Parallel.ForEachAsync(players.Values, ct, async (p, ct) =>
                    {
                        await p.SendPacketAsync(packet, ct);
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task ScheduledGameUpdate(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(5));

            try
            {
                while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && players.Count > 0)
                {
                    Lobby_GameListPacket packet = new()
                    {
                        Games = [.. ServerContext.Services.Values.Select(s => new Lobby_GameListPacket.Game(
                                s.Type,
                                s.Name,
                                s.PlayersMax,
                                s.PlayersCurrent,
                                s.Status
                            ))
                        ]
                    };

                    await Parallel.ForEachAsync(players.Values, ct, async (p, ct) =>
                    {
                        await p.SendPacketAsync(packet, ct);
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task HandleAsync<T>(T packet, Player sender, CancellationToken ct)
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
                    if (!ServerContext.Services.TryGetValue(joinGamePacket.GameName, out IService? service)) 
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
            IService newGame;
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

        private async Task SendPlayerUpdateAsync(CancellationToken ct)
        {
            Lobby_PlayerListPacket packet = new()
            {
                Players = [.. players.Values.Select(p => new Lobby_PlayerListPacket.Player(
                                p.Username,
                                p.PingMS,
                                p.Role switch { PlayerRole.Moderator => Lobby_PlayerListPacket.PlayerRole.Moderator, _ => Lobby_PlayerListPacket.PlayerRole.Player }
                            ))
                ]
            };

            await Parallel.ForEachAsync(players.Values, ct, async (p, ct) =>
            {
                await p.SendPacketAsync(packet, ct);
            });
        }
    }
}

using Network.Packets;
using Serilog;
using Server.Model;
using System.ComponentModel.Design;

namespace Server.Services
{
    internal class Lobby : IService
    {
        private readonly ILogger logger = Log.ForContext("SourceContext", "Lobby");
        private readonly Dictionary<string, Player> players = [];

        private Task task;
        private readonly CancellationTokenSource cts = new();

        public Lobby()
        {
            task = Task.CompletedTask;
        }
        public void Dispose()
        {
            cts.Cancel();
            task.Wait();
        }


        public async Task AddPlayerAsync(Player player)
        {
            players.Add(player.Username, player);
            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby });

            LobbyPacket packet = new()
            {
                Players = [.. players.Values.Select(p => new LobbyPacket.Player(
                    p.Username,
                    p.PingMS,
                    p.Role switch { PlayerRole.Moderator => LobbyPacket.PlayerRole.Moderator, _ => LobbyPacket.PlayerRole.Player }
                ))]
            };
            await player.SendPacketAsync(packet);
            logger.ForContext("Player", player.Username).Information("Player joined");

            if (task.IsCompleted)
                task = ScheduledUpdate(cts.Token);
        }
        public async Task RemovePlayerAsync(Player player)
        {
            players.Remove(player.Username);
        }
        public async Task RecoverAsync(Player player)
        {
            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Lobby });
            LobbyPacket packet = new()
            {
                Players = [.. players.Values.Select(p => new LobbyPacket.Player(
                    p.Username,
                    0,
                    p.Role switch { PlayerRole.Moderator => LobbyPacket.PlayerRole.Moderator, PlayerRole.Player => LobbyPacket.PlayerRole.Player, _ => LobbyPacket.PlayerRole.Player }
                ))]
            };
            await player.SendPacketAsync(packet);
        }

        private async Task ScheduledUpdate(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            try
            {
                while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && players.Count > 0)
                {
                    LobbyPacket packet = new()
                    {
                        Players = [.. players.Values.Select(p => new LobbyPacket.Player(
                                p.Username,
                                p.PingMS,
                                p.Role switch { PlayerRole.Moderator => LobbyPacket.PlayerRole.Moderator, _ => LobbyPacket.PlayerRole.Player }
                            ))
                        ]
                    };

                    await Parallel.ForEachAsync(players.Values, ct, async (p, ct) =>
                    {
                        await p.SendPacketAsync(packet).WaitAsync(ct);
                    });
                }
            }
            catch (OperationCanceledException) { }
        }

        public async Task HandleAsync<T>(T packet, Player sender)
        {
            switch (packet)
            {
                case CreateGamePacket createGamePacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        logger.ForContext("Actor", sender.Username).Warning("Denied access to CreateGame Method", sender.Username);
                        return;
                    }
                    await CreateGame(createGamePacket, sender);
                    break;
            }
        }

        private async Task CreateGame(CreateGamePacket createGamePacket, Player sender)
        {
            IService newGame;
            switch (createGamePacket.Game)
            {
                case CreateGamePacket.GameType._57:
                    newGame = new _57();
                    break;
                default:
                    return;
            }
            Server.Instance!.Context.Services.Add(newGame);

            await RemovePlayerAsync(sender);
            await newGame.AddPlayerAsync(sender);
        }
    }
}

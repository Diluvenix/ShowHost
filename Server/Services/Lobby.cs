using Network;
using Network.Packets;
using Serilog;
using Server.Model;

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

            LobbyPacket packet = new()
            {
                Players = [.. players.Values.Select(p => new LobbyPacket.Player(
                    p.Username,
                    p.PingMS,
                    p.Role switch { PlayerRole.Moderator => LobbyPacket.PlayerRole.Moderator, _ => LobbyPacket.PlayerRole.Player }
                ))]
            };
            await player.SendPacketAsync(packet);
            logger.Information("Player joined Username={0}", player.Username);

            if (task.IsCompleted)
                task = ScheduledUpdate(cts.Token);
        }
        public async Task RemovePlayerAsync(Player player)
        {
            players.Remove(player.Username);
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

        public async Task HandleAsync<T>(T packet)
        {
            throw new NotImplementedException();
        }

        public async Task RecoverAsync(Player player)
        {
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
    }
}

using Network.Packets;
using Network.Packets.Games._57;
using Network.Packets.Games.Lobby;
using Serilog;
using Server.Model;
using System.Net.Sockets;

namespace Server.Services
{
    internal class _57 : ServiceBase
    {
        public override int PlayersCount => players.Count;

        private InternalStatus internalStatus = InternalStatus.Lobby;
        private readonly OrderedDictionary<string, InternalPlayer> players = [];
        private readonly List<string> moderators = [];

        private Task scheduledPingUpdateTask;

        public _57() : base("57") 
        {
            PlayersMax = 4;
            scheduledPingUpdateTask = Task.CompletedTask;
        }

        public override void Dispose() { }

        public override bool CanPlayerJoin(Player player) 
            => player.Role == PlayerRole.Moderator || (PlayersCount < PlayersMax && internalStatus == InternalStatus.Lobby);

        private protected override async Task OnPlayerAddedAsync(Player player, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    switch (player.Role)
                    {
                        case PlayerRole.Player:
                            players.Add(player.Username, new InternalPlayer(player.Username, Colors.GetNextDefault(players.Values.Select(p => p.Color))));
                            break;
                        case PlayerRole.Moderator:
                            moderators.Add(player.Username);
                            break;
                    }
                    await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby }, ct);
                    await Lobby_SendLobbyPacketAsync(ct);
                    break;
            }

            if (scheduledPingUpdateTask.IsCompleted)
                scheduledPingUpdateTask = ScheduledPingUpdateAsync(Context.Cts.Token);
        }
        private protected override async Task OnPlayerRemovedAsync(Player player, CancellationToken ct)
        {
            switch (player.Role)
            {
                case PlayerRole.Player:
                    players.Remove(player.Username);
                    break;
                case PlayerRole.Moderator:
                    moderators.Remove(player.Username);
                    break;
            }
        }
        private protected override async Task OnPlayerRecoveredAsync(Player player, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby }, ct);
                    await Lobby_SendLobbyPacketAsync(ct);
                    break;
            }

            if (scheduledPingUpdateTask.IsCompleted)
                scheduledPingUpdateTask = ScheduledPingUpdateAsync(Context.Cts.Token);
        }

        private async Task ScheduledPingUpdateAsync(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct) && clients.Values.Any(c => c.IsConnected))
            {
                _57_PingPacket packet = new()
                {
                    Players = [.. clients.Values.Select(p => new _57_PingPacket.Player(p.Username, p.PingMS))]
                };

                await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
                {
                    await p.SendPacketAsync(packet, ct);
                });
            }
        }


        public override async Task HandleAsync<T>(T packet, Player sender, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    await Lobby_HandleAsync(packet, sender, ct);
                    break;
            }
        }

        #region Lobby
        private async Task Lobby_HandleAsync<T>(T packet, Player sender, CancellationToken ct)
        {
            switch (packet)
            {
                case _57_LobbySettingsUpdatePacket _57_LobbySettingsUpdatePacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        logger.ForContext("Actor", sender.Username).Warning("Access to LobbySettingsUpdate denied");
                        return;
                    }
                    await Lobby_HandleAsync(_57_LobbySettingsUpdatePacket, sender, ct);
                    break;
                case Signal signal:
                    await Lobby_HandleAsync(signal, sender, ct);
                    break;
            }
        }

        private async Task Lobby_HandleAsync(_57_LobbySettingsUpdatePacket packet, Player sender, CancellationToken ct)
        {
            ILogger logger = this.logger.ForContext("Actor", sender.Username);

            if (!string.IsNullOrEmpty(packet.Name))
            {
                if (!Context.Services.TryRename(Name, packet.Name))
                {
                    logger.ForContext("NewName", packet.Name).Warning("Invalid or already taken Name");
                }
                else
                {
                    logger.ForContext("NewName", packet.Name).Information("Updated Name");
                    Name = packet.Name;
                    this.logger = Log.ForContext("SourceContext", Type).ForContext(nameof(Name), Name);
                    logger = this.logger.ForContext("Actor", sender.Username);
                }
            }

            if (packet.PlayersMax != null)
            {
                if (packet.PlayersMax < 1)
                {
                    logger.ForContext(nameof(PlayersMax), PlayersMax).ForContext("NewPlayersMax", packet.PlayersMax).Warning("Invalid PlayersMax");
                }
                else
                {
                    logger.ForContext(nameof(PlayersMax), PlayersMax).ForContext("NewPlayersMax", packet.PlayersMax).Information("Updated PlayersMax");
                    PlayersMax = packet.PlayersMax.Value;
                }
            }

            if (packet.ColorUsername != null && packet.Color != null)
            {
                logger = logger.ForContext("Target", packet.ColorUsername);

                if (!players.TryGetValue(packet.ColorUsername, out InternalPlayer? player))
                {
                    logger.Warning("Unknown Player");
                }
                else
                {
                    packet.Color &= 0xffffff;
                    logger.ForContext("Color", player.Color).ForContext("NewColor", packet.Color).Information("Updated Color");
                    player.Color = packet.Color.Value;
                }
            }

            await Lobby_SendLobbyPacketAsync(ct);
        }

        private async Task Lobby_HandleAsync(Signal signal, Player sender, CancellationToken ct)
        {
            ILogger logger = this.logger.ForContext("Actor", sender.Username);

            switch (signal)
            {
                case Signal.START:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        logger.Warning("Access to START denied");
                        return;
                    }

                    if (PlayersCount != PlayersMax)
                    {
                        logger.ForContext(nameof(PlayersCount), PlayersCount).ForContext(nameof(PlayersMax), PlayersMax).Warning("Incorrect number of players");
                        return;
                    }

                    logger.Information("Game started");
                    internalStatus = InternalStatus.Game;
                    Status = Lobby_GameListPacket.GameStatus.Running;

                    SetViewPacket packet = new() { View = SetViewPacket.ViewType._57_Game };
                    await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
                    {
                        await p.SendPacketAsync(packet, ct);
                    });

                    break;
            }
        }


        private async Task Lobby_SendLobbyPacketAsync(CancellationToken ct)
        {
            _57_LobbyPacket packet = new()
            {
                Name = Name,
                PlayersMax = PlayersMax,
                PlayersCurrent = PlayersCount,
                Players = [.. players.Values.Select(p => new _57_LobbyPacket.Player(p.Username, clients[p.Username].PingMS, p.Color))]
            };

            await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
            {
                await p.SendPacketAsync(packet, ct);
            });
        }
        #endregion


        private enum InternalStatus
        {
            Lobby,
            Game,
        }

        private class InternalPlayer(string username, UInt32 color)
        {
            public string Username { get; set; } = username;
            public UInt32 Color { get; set; } = color;
        }
    }
}

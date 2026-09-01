using Network.Packets;
using Network.Packets.Games._57;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal class _57 : ServiceBase
    {
        private InternalStatus internalStatus = InternalStatus.Lobby;
        private readonly OrderedDictionary<string, _57_Player> players = [];
        private readonly List<string> moderators = [];

        public _57() : base("57") 
        {
            PlayersMax = 4;
        }

        public override void Dispose() { }

        private protected override async Task OnPlayerAddedAsync(Player player, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    switch (player.Role)
                    {
                        case PlayerRole.Player:
                            players.Add(player.Username, new _57_Player(player.Username, player.PingMS, Colors.GetNextDefault(players.Values.Select(p => p.Color)), 0));
                            PlayersCurrent = players.Count;
                            break;
                        case PlayerRole.Moderator:
                            moderators.Add(player.Username);
                            break;
                    }
                    await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby }, ct);
                    await SendLobbyUpdateAsync(ct);
                    break;
            }
        }
        private protected override async Task OnPlayerRemovedAsync(Player player, CancellationToken ct)
        {
            switch (player.Role)
            {
                case PlayerRole.Player:
                    players.Remove(player.Username);
                    PlayersCurrent = players.Count;
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
                    await SendLobbyUpdateAsync(ct);
                    break;
            }
        }

        public override async Task HandleAsync<T>(T packet, Player sender, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    await HandleLobbyAsync(packet, sender, ct);
                    break;
            }
        }

        private async Task HandleLobbyAsync<T>(T packet, Player sender, CancellationToken ct)
        {
            switch (packet)
            {
                case _57_LobbySettingsUpdatePacket _57_LobbySettingsUpdatePacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        logger.ForContext("Actor", sender.Username).Warning("Denied access to LobbySettingsUpdate Method");
                        return;
                    }
                    await LobbySettingsUpdateAsync(_57_LobbySettingsUpdatePacket, sender, ct);
                    break;
            }
        }

        private async Task LobbySettingsUpdateAsync(_57_LobbySettingsUpdatePacket packet, Player sender, CancellationToken ct)
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

            await SendLobbyUpdateAsync(ct);
        }

        private async Task SendLobbyUpdateAsync(CancellationToken ct)
        {
            _57_LobbyPacket packet = new()
            {
                Name = Name,
                PlayersMax = PlayersMax,
                PlayersCurrent = PlayersCurrent,
                Players = [.. players.Values]
            };

            await Parallel.ForEachAsync(clients.Values, ct, async (p, ct) =>
            {
                await p.SendPacketAsync(packet, ct);
            });
        }

        private enum InternalStatus
        {
            Lobby,
        }
    }
}

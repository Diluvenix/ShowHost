using Network.Packets;
using Network.Packets.Games._57;
using Network.Packets.Games.Lobby;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal class _57 : IService
    {
        public string Type => "57";
        public string Name => name;
        public int PlayersMax => playersMax;
        public int PlayersCurrent => players.Count;
        public Lobby_GameListPacket.GameStatus Status => Lobby_GameListPacket.GameStatus.Preparing;


        private InternalStatus internalStatus = InternalStatus.Lobby;
        private readonly Dictionary<string, Player> clients = [];
        private readonly Dictionary<string, _57_Player> players = [];
        private readonly List<string> moderators = [];

        private string name;
        private int playersMax = 4;

        private ILogger logger;

        public _57()
        {
            Context.Services.TryAddGenerated(out name, this);

            logger = Log.ForContext("SourceContext", Type).ForContext(nameof(Name), Name);
            logger.Information("New game created");
        }
        public void Dispose() { }

        public async Task AddPlayerAsync(Player player, CancellationToken ct)
        {
            clients.Add(player.Username, player);
            logger.ForContext("Player", player.Username).Information("Player joined");

            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    switch (player.Role)
                    {
                        case PlayerRole.Player:
                            players.Add(player.Username, new _57_Player(player.Username, Colors.GetNextDefault(players.Values.Select(p => p.Color)), 0));
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
        public async Task RemovePlayerAsync(Player player, CancellationToken ct)
        {
            clients.Remove(player.Username);
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
        public async Task RecoverAsync(Player player, CancellationToken ct)
        {
            switch (internalStatus)
            {
                case InternalStatus.Lobby:
                    await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby }, ct);
                    await SendLobbyUpdateAsync(ct);
                    break;
            }
        }

        public async Task HandleAsync<T>(T packet, Player sender, CancellationToken ct)
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
                    name = packet.Name;
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
                    playersMax = packet.PlayersMax.Value;
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

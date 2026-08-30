using Network.Packets;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal class _57 : IService
    {
        public string Type => "57";
        public string Name => "unnamed";
        public int PlayersMax => 4;
        public int PlayersCurrent => 75;
        public GameListPacket.GameStatus Status => GameListPacket.GameStatus.Preparing;

        private readonly ILogger logger = Log.ForContext("SourceContext", "57");
        private readonly Dictionary<string, Player> players = [];

        private Task task;
        private readonly CancellationTokenSource cts = new();

        public _57()
        {
            task = Task.CompletedTask;
            logger.Information("New game created");
        }
        public void Dispose()
        {
            cts.Cancel();
            task.Wait();
        }

        public async Task AddPlayerAsync(Player player)
        {
            players.Add(player.Username, player);
            logger.ForContext("Player", player.Username).Information("Player joined");

            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby });
        }
        public async Task RemovePlayerAsync(Player player)
        {
            players.Remove(player.Username);
        }
        public async Task RecoverAsync(Player player)
        {
            throw new NotImplementedException();
        }

        public async Task HandleAsync<T>(T packet, Player sender)
        {
            throw new NotImplementedException();
        }
    }
}

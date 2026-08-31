using Network.Packets;
using Network.Packets.Games._57;
using Serilog;
using Server.Model;

namespace Server.Services
{
    internal class _57 : IService
    {
        public string Type => "57";
        private readonly string name;
        public string Name => name;
        public int PlayersMax => 4;
        public int PlayersCurrent => players.Count;
        public GameListPacket.GameStatus Status => GameListPacket.GameStatus.Preparing;


        private InternalStatus internalStatus = InternalStatus.Lobby;
        private readonly Dictionary<string, Player> clients = [];
        private readonly List<string> players = [];
        private readonly List<string> moderators = [];


        private Task task;
        private readonly CancellationTokenSource cts = new();
        private readonly ILogger logger;

        public _57()
        {
            task = Task.CompletedTask;
            Server.Instance!.Context.ServiceNameManager.TryGenerate(out name);

            logger = Log.ForContext("SourceContext", "57").ForContext("Game", name);
            logger.Information("New game created");
        }
        public void Dispose()
        {
            cts.Cancel();
            task.Wait();
        }

        public async Task AddPlayerAsync(Player player)
        {
            clients.Add(player.Username, player);
            switch (player.Role)
            {
                case PlayerRole.Player:
                    players.Add(player.Username);
                    break;
                case PlayerRole.Moderator:
                    moderators.Add(player.Username);
                    break;
            }
            logger.ForContext("Player", player.Username).Information("Player joined");

            await player.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType._57_Lobby });
            await player.SendPacketAsync(new _57_LobbyPacket()
            {
                Name = Name,
                PlayersMax = PlayersMax,
                PlayersCurrent = PlayersCurrent,
            });

            if (task.IsCompleted)
                task = ScheduledTasks(cts.Token);
        }
        public async Task RemovePlayerAsync(Player player)
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
        public async Task RecoverAsync(Player player)
        {
            throw new NotImplementedException();
        }

        private async Task ScheduledTasks(CancellationToken ct)
        {
            Task[] tasks = internalStatus switch
            {
                InternalStatus.Lobby => [
                    ScheduledLobbyUpdate()
                ],
                _ => [
                    Task.CompletedTask
                ],
            };


            try
            {
                await Task.WhenAll(tasks).WaitAsync(ct);
            }
            catch (OperationCanceledException) { }
        }

        private async Task ScheduledLobbyUpdate()
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            while (await timer.WaitForNextTickAsync() && clients.Count > 0)
            {
                _57_LobbyPacket packet = new()
                {
                    Name = Name,
                    PlayersMax = PlayersMax,
                    PlayersCurrent = PlayersCurrent,
                };

                await Parallel.ForEachAsync(clients.Values, async (p, _) =>
                {
                    await p.SendPacketAsync(packet);
                });
            }
        }

        public async Task HandleAsync<T>(T packet, Player sender)
        {
            //throw new NotImplementedException();
        }




        private enum InternalStatus
        {
            Lobby,
        }
    }
}

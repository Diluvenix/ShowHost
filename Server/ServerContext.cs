using Server.Keys;
using Server.Model;
using Server.Services;
using System.Collections.Concurrent;

namespace Server
{
    internal class ServerContext : IDisposable
    {
        public ConcurrentDictionary<string, Player> Players = [];

        public KeyManager ModeratorKeys = new();

        public Lobby Lobby = new();
        public List<IService> Services = [];

        public readonly CancellationTokenSource Cts = new();

        public void Dispose()
        {
            Cts.Cancel();

            Lobby.Dispose();
            foreach (IService service in Services)
                service.Dispose();

            foreach (Player player in Players.Values)
                player.Dispose();

            Cts.Dispose();
        }
    }
}

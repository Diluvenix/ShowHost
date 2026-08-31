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
        public readonly ConcurrentNameDictionary<IService> Services = new(
            NameGenerator.Generate,
            (s) => !string.IsNullOrEmpty(s) && s.Length <= 32 && s.AsSpan().IndexOfAnyExceptInRange(' ', '~') == -1
        );

        public readonly CancellationTokenSource Cts = new();

        public void Dispose()
        {
            Cts.Cancel();

            Lobby.Dispose();
            foreach (IService service in Services.Values)
                service.Dispose();

            foreach (Player player in Players.Values)
                player.Dispose();

            Cts.Dispose();
        }
    }
}

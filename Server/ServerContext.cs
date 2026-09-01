using Server.Keys;
using Server.Model;
using Server.Services;
using System.Collections.Concurrent;

namespace Server
{
    internal static class ServerContext
    {
        public static ConcurrentDictionary<string, Player> Players = [];

        public static KeyManager ModeratorKeys = new();

        public static Lobby Lobby = new();
        public static readonly ConcurrentNameDictionary<IService> Services = new(
            NameGenerator.Generate,
            (s) => !string.IsNullOrEmpty(s) && s.Length <= 32 && s.AsSpan().IndexOfAnyExceptInRange(' ', '~') == -1
        );

        public static readonly CancellationTokenSource Cts = new();

        public static void Dispose()
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

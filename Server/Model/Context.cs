using Server.Keys;
using Server.Services;
using System.Collections.Concurrent;

namespace Server.Model
{
    internal static class Context
    {
        public static readonly ConcurrentDictionary<string, Player> Players = [];

        public static readonly KeyManager ModeratorKeys = new();

        public static readonly Lobby Lobby = new();
        public static readonly ConcurrentNameDictionary<IService> Services = new(
            NameGenerator.Generate,
            (s) => !string.IsNullOrEmpty(s) && s.Length <= 32 && s.AsSpan().IndexOfAnyExceptInRange(' ', '~') == -1 && s != "Lobby"
        );

        public static readonly CancellationTokenSource Cts = new();

        public static void Dispose()
        {
            Lobby.Dispose();
            foreach (IService service in Services.Values)
                service.Dispose();

            foreach (Player player in Players.Values)
                player.Dispose();
        }
    }
}

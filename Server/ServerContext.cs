using Server.Keys;
using Server.Model;
using Server.Services;
using System.Collections.Concurrent;

namespace Server
{
    internal static class ServerContext
    {
        public static readonly ConcurrentDictionary<string, Player> Players = [];

        public static readonly KeyManager ModeratorKeys = new();

        public static readonly Lobby Lobby = new();
        public static readonly ConcurrentNameDictionary<IService> Services = new(
            NameGenerator.Generate,
            (s) => !string.IsNullOrEmpty(s) && s.Length <= 32 && s.AsSpan().IndexOfAnyExceptInRange(' ', '~') == -1
        );

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

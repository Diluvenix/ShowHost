using Server.Model;

namespace Server.Commands
{
    internal class DeleteCommand : ICommand
    {
        public string Name => "delete";

        public string Description => "Delets a player connection";

        public void Execute(string[] args, Server server)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: delete <username>");
                return;
            }

            string username = args[0];
            if (!server.Context.Players.TryGetValue(username, out Player? player))
            {
                Console.WriteLine("Couldn't find player {0}", username);
                return;
            }

            player.Service?.RemovePlayerAsync(player).Wait();
            server.Context.Players.Remove(player.Username, out _);
            player.Dispose();
            Console.WriteLine("Deleted player connection {0}", username);
        }
    }
}

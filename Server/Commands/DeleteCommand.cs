using Server.Model;

namespace Server.Commands
{
    internal class DeleteCommand : ICommand
    {
        public string Name => "delete";

        public string Description => "Delets a player connection";

        public void Execute(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: delete <username>");
                return;
            }

            string username = args[0];
            if (!Context.Players.TryGetValue(username, out Player? player))
            {
                Console.WriteLine("Couldn't find player {0}", username);
                return;
            }

            try
            {
                player.Service.RemovePlayerAsync(player, Context.Cts.Token).Wait();
                Context.Players.Remove(player.Username, out _);
                player.Dispose();
                Console.WriteLine("Deleted player connection {0}", username);
            }
            catch (AggregateException) { }
        }
    }
}

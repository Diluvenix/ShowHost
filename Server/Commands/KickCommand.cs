using Server.Model;

namespace Server.Commands
{
    internal class KickCommand : ICommand
    {
        public string Name => "kick";

        public string Description => "Kicks a player";

        public void Execute(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: kick <username>");
                return;
            }

            string username = args[0];
            if (!ServerContext.Players.TryGetValue(username, out Player? player))
            {
                Console.WriteLine("Couldn't find player {0}", username);
                return;
            }

            try
            {
                player.DisconnectAsync("Kicked", ServerContext.Cts.Token).Wait();
                Console.WriteLine("Kicked player {0}", username);
            }
            catch (AggregateException) { }
        }
    }
}

using Server.Keys;
using Server.Model;

namespace Server.Commands
{
    internal class RecoverCommand : ICommand
    {
        public string Name => "recover";

        public string Description => "Generates a recovery key";

        public void Execute(string[] args, Server server)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: recovery <username>");
                return;
            }

            string username = args[0];
            if (!server.Context.Players.TryGetValue(username, out Player? player))
            {
                Console.WriteLine("Couldn't find player {0}", username);
                return;
            }

            Base32Token base32Token = Base32Token.FromRandom();
            Console.WriteLine("Generated a new recovery key: \"{0}\" for player {1}", base32Token.Code, username);
            KeyToken keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));

            player.KeyManager.RegisterKey(keyToken);
        }
    }
}

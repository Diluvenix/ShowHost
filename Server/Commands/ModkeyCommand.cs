using Server.Keys;

namespace Server.Commands
{
    internal class ModkeyCommand : ICommand
    {
        public string Name => "modkey";

        public string Description => "Generates a new moderator key";

        public void Execute(string[] args)
        {
            Base32Token base32Token = Base32Token.FromRandom();
            Console.WriteLine("Generated a moderator key: \"{0}\"", base32Token.Code);
            KeyToken keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));

            ServerContext.ModeratorKeys.RegisterKey(keyToken);
        }
    }
}

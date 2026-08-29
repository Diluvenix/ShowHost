using Server.Model;

namespace Server.Commands
{
    internal class ListCommand : ICommand
    {
        public string Name => "list";

        public string Description => "Lists all players";

        public void Execute(string[] args, Server server)
        {
            Console.WriteLine("There are currently {0} players connected:", server.Context.Players.Count);

            foreach (Player player in server.Context.Players.Values)
            {
                Console.WriteLine(
                    $"\t{player.Username, -10}" +
                    $"\t{player.Role switch { PlayerRole.Moderator => "Moderator", _ => "Player" }, -10}" +
                    $"\t{(player.IsConnected ? player.PingMS : "Disconnected")}"
                );
            }
        }
    }
}

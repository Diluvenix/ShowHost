using Server.Model;

namespace Server.Commands
{
    internal class ListCommand : ICommand
    {
        public string Name => "list";

        public string Description => "Lists all players";

        public void Execute(string[] args)
        {
            Console.WriteLine("There are currently {0} players connected:", Context.Players.Count);

            foreach (Player player in Context.Players.Values)
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

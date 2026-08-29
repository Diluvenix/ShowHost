using System.Text;

namespace Server.Commands
{
    internal static class CommandManager
    {
        private static readonly Dictionary<string, ICommand> commands = new()
        {
            ["stop"] = new StopCommand(),
            ["list"] = new ListCommand(),
            ["modkey"] = new ModkeyCommand(),
            ["recover"] = new RecoverCommand(),
            ["kick"] = new KickCommand(),
            ["delete"] = new DeleteCommand(),
        };

        public static void Execute(string input, Server server)
        {
            if (string.IsNullOrEmpty(input)) return;

            string[] parts = input.Split(' ');

            string name = parts[0];
            string[] args = [.. parts.Skip(1)];

            if(commands.TryGetValue(name, out ICommand? command))
            {
                command?.Execute(args, server);
            }
            else if (name == "help")
            {
                Console.WriteLine("'help' - Displays this message");
                foreach (ICommand cmd in commands.Values)
                {
                    Console.WriteLine("'{0}' - {1}", cmd.Name, cmd.Description);
                }
            }
            else
            {
                Console.WriteLine("Unknown Command. Use 'help' to get a list of available commands");
            }
        }
    }
}

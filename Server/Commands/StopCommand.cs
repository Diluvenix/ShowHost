namespace Server.Commands
{
    internal class StopCommand : ICommand
    {
        public string Name => "stop";

        public string Description => "Stops the server";

        public void Execute(string[] args, Server server)
        {
            Program.Cts.Cancel();
        }
    }
}

using Server.Model;

namespace Server.Commands
{
    internal interface ICommand
    {
        string Name { get; }
        string Description { get; }

        void Execute(string[] args);
    }
}

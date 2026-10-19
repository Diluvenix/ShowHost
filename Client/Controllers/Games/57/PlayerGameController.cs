using Client.Views.Games._57;
using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games._57
{
    internal class PlayerGameController : IController
    {
        private readonly PlayerGameView view = new();
        public UserControl View => view;

        public Geometry? Path => null;

        public void Dispose()
        {
        }

        public void Handle<T>(T packet)
        {
            switch (packet)
            {
                case _57_GamePacket _57_GamePacket:
                    view.Dispatcher.Invoke(HandleGamePacket, _57_GamePacket);
                    break;
            }
        }

        private void HandleGamePacket(_57_GamePacket packet)
        {
            if (packet.Entries is not null)
                view.UpdateEntries(packet.Entries);
        }
    }
}

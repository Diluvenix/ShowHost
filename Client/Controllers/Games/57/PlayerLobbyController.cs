using Client.Views.Games._57;
using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games._57
{
    internal class PlayerLobbyController : IController
    {
        private readonly PlayerLobbyView view = new();
        public UserControl View => view;

        public Geometry? Path => null;

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet)
        {
            switch (packet)
            {
                case _57_LobbyPacket _57_LobbyPacket:
                    view.Dispatcher.Invoke(() =>
                    {
                        view.NameLabel.Content = _57_LobbyPacket.Name;
                        view.PlayerCountLabel.Content = $"{_57_LobbyPacket.PlayersCurrent}/{_57_LobbyPacket.PlayersMax}";
                    });
                    break;
            }
        }
    }
}

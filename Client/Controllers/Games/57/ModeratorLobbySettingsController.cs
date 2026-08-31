using Client.Views.Games._57;
using Network.Packets.Games._57;
using System.Configuration;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games._57
{
    class ModeratorLobbySettingsController : IController
    {
        private readonly ModeratorLobbySettingsView view = new();
        public UserControl View => view;

        private static readonly Geometry path = new GeometryGroup()
        {
            Children = [
                new LineGeometry() {
                    StartPoint = new(4, 21),
                    EndPoint = new(4, 14)
                },
                new LineGeometry() {
                    StartPoint = new(4, 10),
                    EndPoint = new(4, 3)
                },
                new LineGeometry() {
                    StartPoint = new(12, 21),
                    EndPoint = new(12, 12)
                },
                new LineGeometry() {
                    StartPoint = new(12, 8),
                    EndPoint = new(12, 3)
                },
                new LineGeometry() {
                    StartPoint = new(20, 21),
                    EndPoint = new(20, 16)
                },
                new LineGeometry() {
                    StartPoint = new(20, 12),
                    EndPoint = new(20, 3),
                },
                new LineGeometry() {
                    StartPoint = new(1, 14),
                    EndPoint = new(7, 14)
                },
                new LineGeometry() {
                    StartPoint = new(9, 8),
                    EndPoint = new(15, 8)
                },
                new LineGeometry() {
                    StartPoint = new(17, 16),
                    EndPoint = new(23, 16)
                }
            ]
        };
        public Geometry? Path => path;

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet)
        {
            switch (packet)
            {
                case _57_LobbyPacket _57_LobbyPacket:
                    view.Dispatcher.Invoke(() =>
                    {
                        view.SetLobbyName(_57_LobbyPacket.Name);
                        view.SetPlayersMax(_57_LobbyPacket.PlayersMax);
                    });
                    break;
            }
        }
    }
}

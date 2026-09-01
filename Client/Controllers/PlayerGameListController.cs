using Client.Views;
using Network.Packets.Games.Lobby;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers
{
    class PlayerGameListController : IController
    {
        private readonly PlayerGameListView view = new();
        public UserControl View => view;

        private static readonly Geometry path = new GeometryGroup()
        {
            Children = [
                new EllipseGeometry(){
                    RadiusX = 10,
                    RadiusY = 10,
                    Center = new(12,12)
                },
                new PathGeometry(){
                    Figures=PathFigureCollection.Parse("M 16.24 7.76 L 14.12 14.12 L 7.76 16.24 L 9.88 9.88 L 16.24 7.76 Z")
                }
            ]
        };
        public Geometry? Path => path;

        public void Dispose() {}

        public async Task HandleAsync<T>(T packet)
        {
            switch (packet)
            {
                case Lobby_GameListPacket gameListPacket:
                    view.Dispatcher.Invoke(() =>
                    {
                        view.SetGameBoxCount(gameListPacket.Games.Length);
                        for (int i = 0; i < gameListPacket.Games.Length; i++)
                            view.GameBoxes[i].SetFromData(gameListPacket.Games[i]);
                    });
                    break;
            }
        }
    }
}

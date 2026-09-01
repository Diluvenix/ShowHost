using Client.Views.Games.Lobby;
using Network.Packets;
using Network.Packets.Games.Lobby;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games.Lobby
{
    internal class PlayerPlayersController : IController
    {
        private readonly PlayerPlayersView view = new();
        public UserControl View => view;

        private static readonly Geometry path = new GeometryGroup()
        {
            Children = [
                new PathGeometry() 
                { 
                    Figures = PathFigureCollection.Parse("M 3 9 l 9 -7 l 9 7 v 11 a 2 2 0 0 1 -2 2 H 5 a 2 2 0 0 1 -2 -2 z"), 
                    FillRule = FillRule.Nonzero 
                },
                new PathGeometry() 
                {
                    Figures = PathFigureCollection.Parse("M 9 22 L 9 12 L 15 12 L 15 22")
                }
            ]
        };
        public Geometry Path => path;

        private readonly Dictionary<string, PlayerBox> playerBoxes = [];

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet, CancellationToken ct)
        {
            switch (packet)
            {
                case Lobby_PlayerListPacket lobbyPacket:
                    view.Dispatcher.Invoke(HandleLobbyPacket, lobbyPacket);
                    break;
                case PingPacket pingPacket:
                    view.Dispatcher.Invoke(HandlePingPacket, pingPacket);
                    break;
                default:
                    return;
            }
        }

        private void HandleLobbyPacket(Lobby_PlayerListPacket packet)
        {
            if (packet.Players.Length != playerBoxes.Count)
                view.SetPlayerBoxCount(packet.Players.Length);

            playerBoxes.Clear();
            for (int i = 0; i < packet.Players.Length; i++)
            {
                PlayerBox playerBox = view.PlayerBoxes[i];
                playerBoxes[packet.Players[i].Username] = playerBox;
                playerBox.Update(packet.Players[i]);
            }

            view.PlayerViewRefresh();
        }

        private void HandlePingPacket(PingPacket packet)
        {
            foreach (PingPacket.Player player in packet.Players)
            {
                if (playerBoxes.TryGetValue(player.Username, out PlayerBox? playerBox))
                    playerBox.Update(player);
            }
            view.PlayerViewRefresh();
        }
    }
}

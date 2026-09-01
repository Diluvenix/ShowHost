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

        public PlayerPlayersController() { }

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet)
        {
            switch (packet)
            {
                case Lobby_PlayerListPacket lobbyPacket:
                    view.Dispatcher.Invoke(() => HandleLobbyPacket(lobbyPacket));
                    break;
                case PingPacket pingPacket:
                    view.Dispatcher.Invoke(() => HandlePingPacket(pingPacket));
                    break;
                default:
                    return;
            }
        }

        private void HandleLobbyPacket(Lobby_PlayerListPacket packet)
        {
            bool updatePlayers = true;

            foreach (string username in playerBoxes.Keys)
            {
                if (!packet.Players.Any(p => p.Username == username))
                {
                    view.RemovePlayer(playerBoxes[username]);
                    playerBoxes.Remove(username);

                    if (updatePlayers)
                    {
                        MainController.Instance!.RelatedUsers = [.. packet.Players.Select(p => p.Username)];
                        updatePlayers = false;
                    }
                }
            }

            foreach (Lobby_PlayerListPacket.Player player in packet.Players)
            {
                if (!playerBoxes.TryGetValue(player.Username, out PlayerBox? playerBox))
                {
                    playerBox = new PlayerBox(player.Username, player.Ping);
                    playerBoxes[player.Username] = playerBox;
                    view.AddPlayer(playerBox);

                    if (updatePlayers)
                    {
                        MainController.Instance!.RelatedUsers = [.. packet.Players.Select(p => p.Username)];
                        updatePlayers = false;
                    }
                }

                playerBox.Ping = player.Ping;
                playerBox.Role = player.PlayerRole;
            }

            view.PlayerViewRefresh();
        }

        private void HandlePingPacket(PingPacket packet)
        {
            foreach (PingPacket.Player player in packet.Players)
            {
                if (playerBoxes.TryGetValue(player.Username, out PlayerBox? playerBox))
                {
                    playerBox.Ping = player.Ping;
                }
            }
        }
    }
}

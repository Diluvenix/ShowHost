using Client.Views.Games._57;
using Network.Packets;
using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games._57
{
    internal class PlayerLobbyController : IController
    {
        private readonly PlayerLobbyView view = new();
        public UserControl View => view;

        private static readonly Geometry path = new GeometryGroup()
        {
            Children = [
                new PathGeometry() {
                    Figures = PathFigureCollection.Parse("M 2 16.1 A 5 5 0 0 1 5.9 20 M 2 12.05 A 9 9 0 0 1 9.95 20 M 2 8 V 6 a 2 2 0 0 1 2 -2 h 16 a 2 2 0 0 1 2 2 v 12 a 2 2 0 0 1 -2 2 h -6"),
                    FillRule = FillRule.Nonzero
                },
                new LineGeometry() {
                    StartPoint = new(2, 20),
                    EndPoint = new(2.01, 20)
                }
            ]
        };
        public Geometry? Path => path;


        private readonly Dictionary<string, LobbyPlayerBox> playerBoxes = [];

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet, CancellationToken ct)
        {
            switch (packet)
            {
                case _57_LobbyPacket _57_LobbyPacket:
                    view.Dispatcher.Invoke(HandleLobbyPacket, _57_LobbyPacket);
                    break;
                case PingPacket pingPacket:
                    view.Dispatcher.Invoke(() =>
                    {
                        foreach (PingPacket.Player player in pingPacket.Players)
                        {
                            if (playerBoxes.TryGetValue(player.Username, out LobbyPlayerBox? playerBox))
                                playerBox.Update(player);
                        }
                    });
                    break;
            }
        }

        private void HandleLobbyPacket(_57_LobbyPacket packet)
        {
            view.NameLabel.Content = packet.Name;
            view.PlayerCountLabel.Content = $"{packet.PlayersCurrent}/{packet.PlayersMax}";

            if (packet.Players.Length != playerBoxes.Count)
                view.SetPlayerBoxCount(packet.Players.Length);

            playerBoxes.Clear();
            for (int i = 0; i < packet.Players.Length; i++)
            {
                LobbyPlayerBox playerBox = view.PlayerBoxes[i];
                playerBoxes[packet.Players[i].Username] = playerBox;
                playerBox.Update(packet.Players[i]);
            }
        }
    }
}

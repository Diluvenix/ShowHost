using Client.Views.Games.Lobby;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Network.Packets;
using Network.Packets.Games.Lobby;

namespace Client.Controllers.Games.Lobby
{
    class ModeratorSettingsController : IController
    {
        private readonly ModeratorSettingsView view = new();
        public UserControl View => view;

        private static readonly Geometry path = new GeometryGroup() 
        { 
            Children = [
                new PathGeometry() {
                    Figures = PathFigureCollection.Parse("M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1 0 2.83 2 2 0 0 1-2.83 0l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-2 2 2 2 0 0 1-2-2v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83 0 2 2 0 0 1 0-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1-2-2 2 2 0 0 1 2-2h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 0-2.83 2 2 0 0 1 2.83 0l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 2-2 2 2 0 0 1 2 2v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 0 2 2 0 0 1 0 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 2 2 2 2 0 0 1-2 2h-.09a1.65 1.65 0 0 0-1.51 1z"),
                    FillRule = FillRule.Nonzero
                },
                new EllipseGeometry() {
                    RadiusX = 4,
                    RadiusY = 4,
                    Center = new Point(12, 12),
                }
            ] 
        };
        public Geometry? Path => path;

        public void Dispose() {}

        public Task HandleAsync<T>(T packet, CancellationToken ct)
        {
            switch (packet)
            {
                case ModerationSecretPacket moderationSecretPacket:
                    switch (moderationSecretPacket.Type)
                    {
                        case ModerationSecretPacket.SecretType.Recovery:
                            view.Dispatcher.Invoke(() => view.RecoveryBox.Text = moderationSecretPacket.Secret);
                            break;
                        case ModerationSecretPacket.SecretType.Moderator:
                            view.Dispatcher.Invoke(() => view.ModkeyBox.Text = moderationSecretPacket.Secret);
                            break;
                    }
                    break;
                case Lobby_PlayerListPacket lobbyPacket:
                    view.Dispatcher.Invoke(view.SetPlayers, lobbyPacket.Players.Select(p => p.Username));
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

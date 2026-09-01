using Client.Controllers;
using Network;
using Network.Packets.Games.Lobby;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views.Games.Lobby
{
    /// <summary>
    /// Interaction logic for GameBox.xaml
    /// </summary>
    public partial class GameBox : UserControl
    {
        private string gameName = string.Empty;

        public GameBox()
        {
            InitializeComponent();

            JoinButton.Click += (_, _) =>
            {
                if (string.IsNullOrEmpty(gameName))
                    return;
                _ = MainController.Instance!.Client.SendPacketAsync(new Lobby_GameJoinPacket() { GameName = gameName });
            };
        }

        public void Update(Lobby_GameListPacket.Game gameData)
        {
            gameName = gameData.Name;

            TypeLabel.Content = gameData.Type;
            NameLabel.Content = gameData.Name;
            PlayerLabel.Content = $"{gameData.PlayersCurrent}/{gameData.PlayersMax}";

            PreparingBorder.Visibility = gameData.Status == Lobby_GameListPacket.GameStatus.Preparing ? Visibility.Visible : Visibility.Collapsed;
            RunningBorder.Visibility = gameData.Status == Lobby_GameListPacket.GameStatus.Running ? Visibility.Visible : Visibility.Collapsed;
            JoinButton.IsEnabled = gameData.Status == Lobby_GameListPacket.GameStatus.Preparing;
        }
    }
}

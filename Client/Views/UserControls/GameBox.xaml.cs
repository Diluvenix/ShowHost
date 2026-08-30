using Network.Packets;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views.UserControls
{
    /// <summary>
    /// Interaction logic for GameBox.xaml
    /// </summary>
    public partial class GameBox : UserControl
    {
        public GameBox()
        {
            InitializeComponent();
        }

        public void SetFromData(GameListPacket.Game gameData)
        {
            TypeLabel.Content = gameData.Type;
            NameLabel.Content = gameData.Name;
            PlayerLabel.Content = $"{gameData.PlayersCurrent}/{gameData.PlayersMax}";

            PreparingBorder.Visibility = gameData.GameStatus == GameListPacket.GameStatus.Preparing ? Visibility.Visible : Visibility.Collapsed;
            RunningBorder.Visibility = gameData.GameStatus == GameListPacket.GameStatus.Running ? Visibility.Visible : Visibility.Collapsed;
            JoinButton.IsEnabled = gameData.GameStatus == GameListPacket.GameStatus.Preparing;
        }
    }
}

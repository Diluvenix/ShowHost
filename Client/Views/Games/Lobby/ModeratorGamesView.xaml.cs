using Client.Controllers;
using Network;
using Network.Packets.Games.Lobby;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Views.Games.Lobby
{
    /// <summary>
    /// Interaction logic for ModeratorGamesView.xaml
    /// </summary>
    public partial class ModeratorGamesView : UserControl
    {
        public readonly ObservableCollection<GameBox> GameBoxes = [];
        private readonly ICollectionView gameView;

        private readonly NetworkClient client;

        public ModeratorGamesView()
        {
            InitializeComponent();

            gameView = CollectionViewSource.GetDefaultView(GameBoxes);
            GameList.ItemsSource = GameBoxes;

            client = MainController.Instance!.Client;

            Create57Button.Click += (_, _) =>
            {
                _ = client.SendPacketAsync(new Lobby_GameCreatePacket() { Type = Lobby_GameCreatePacket.GameType._57 }, MainController.Instance!.Cts.Token);
            };
        }

        public void SetGameBoxCount(int count)
        {
            while (GameBoxes.Count > count)
                GameBoxes.RemoveAt(0);
            while (GameBoxes.Count < count)
                GameBoxes.Add(new GameBox());

            gameView.Refresh();
        }
    }
}

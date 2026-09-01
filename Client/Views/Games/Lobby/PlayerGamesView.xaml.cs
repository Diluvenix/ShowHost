using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Views.Games.Lobby
{
    /// <summary>
    /// Interaction logic for PlayerGamesView.xaml
    /// </summary>
    public partial class PlayerGamesView : UserControl
    {
        public readonly ObservableCollection<GameBox> GameBoxes = [];
        private readonly ICollectionView gameView;

        public PlayerGamesView()
        {
            InitializeComponent();

            gameView = CollectionViewSource.GetDefaultView(GameBoxes);
            GameList.ItemsSource = GameBoxes;
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

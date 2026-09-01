using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Views.Games.Lobby
{
    /// <summary>
    /// Interaction logic for PlayerPlayersView.xaml
    /// </summary>
    public partial class PlayerPlayersView : UserControl
    {
        public readonly ObservableCollection<PlayerBox> PlayerBoxes = [];
        private readonly ICollectionView playerView;

        public PlayerPlayersView()
        {
            InitializeComponent();

            playerView = CollectionViewSource.GetDefaultView(PlayerBoxes);
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.RoleRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.PingRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.Username), ListSortDirection.Ascending));

            PlayerList.ItemsSource = playerView;
        }

        public void SetPlayerBoxCount(int count)
        {
            while (PlayerBoxes.Count > count)
                PlayerBoxes.RemoveAt(0);
            while (PlayerBoxes.Count < count)
                PlayerBoxes.Add(new PlayerBox());
        }

        public void PlayerViewRefresh()
            => playerView.Refresh();
    }
}

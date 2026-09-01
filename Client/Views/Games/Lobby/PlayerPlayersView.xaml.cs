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
        private readonly ObservableCollection<PlayerBox> players = [];
        private readonly ICollectionView playerView;

        public PlayerPlayersView()
        {
            InitializeComponent();

            playerView = CollectionViewSource.GetDefaultView(players);
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.RoleRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.PingRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(PlayerBox.Username), ListSortDirection.Ascending));

            PlayerList.ItemsSource = playerView;
        }

        public void AddPlayer(PlayerBox playerBox)
            => players.Add(playerBox);

        public void RemovePlayer(PlayerBox playerBox)
            => players.Remove(playerBox);

        public void PlayerViewRefresh()
            => playerView.Refresh();
    }
}

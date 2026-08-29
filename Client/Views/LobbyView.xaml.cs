using Client.Views.UserControls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for LobbyView.xaml
    /// </summary>
    public partial class LobbyView : UserControl
    {
        private readonly ObservableCollection<LobbyPlayerBox> players = [];
        private readonly ICollectionView playerView;

        public LobbyView()
        {
            InitializeComponent();

            playerView = CollectionViewSource.GetDefaultView(players);
            playerView.SortDescriptions.Add(new SortDescription(nameof(LobbyPlayerBox.RoleRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(LobbyPlayerBox.PingRank), ListSortDirection.Descending));
            playerView.SortDescriptions.Add(new SortDescription(nameof(LobbyPlayerBox.Username), ListSortDirection.Ascending));

            PlayerList.ItemsSource = playerView;
        }

        public void AddPlayer(LobbyPlayerBox playerBox)
            => players.Add(playerBox);

        public void RemovePlayer(LobbyPlayerBox playerBox)
            => players.Remove(playerBox);

        public void PlayerViewRefresh()
            => playerView.Refresh();
    }
}

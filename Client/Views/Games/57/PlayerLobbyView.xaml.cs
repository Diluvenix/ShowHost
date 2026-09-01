using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for PlayerLobbyView.xaml
    /// </summary>
    public partial class PlayerLobbyView : UserControl
    {
        public readonly ObservableCollection<LobbyPlayerBox> PlayerBoxes = [];
        private readonly ICollectionView playerBoxesView;

        public PlayerLobbyView()
        {
            InitializeComponent();

            playerBoxesView = CollectionViewSource.GetDefaultView(PlayerBoxes);
            PlayerList.ItemsSource = PlayerBoxes;
        }

        public void SetPlayerBoxCount(int count)
        {
            while (PlayerBoxes.Count > count)
                PlayerBoxes.RemoveAt(0);
            while (PlayerBoxes.Count < count)
                PlayerBoxes.Add(new LobbyPlayerBox());

            playerBoxesView.Refresh();
        }
    }
}

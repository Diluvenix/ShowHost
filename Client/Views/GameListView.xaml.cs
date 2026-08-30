using Client.Controllers;
using Network;
using Network.Packets;
using System.Windows.Controls;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for GameListView.xaml
    /// </summary>
    public partial class GameListView : UserControl
    {
        private readonly NetworkClient client;

        public GameListView()
        {
            InitializeComponent();
            client = MainController.Instance!.Client;

            Create57Button.Click += (_, _) =>
            {
                _ = client.SendPacketAsync(new CreateGamePacket() { Game = CreateGamePacket.GameType._57 });
            };
        }
    }
}

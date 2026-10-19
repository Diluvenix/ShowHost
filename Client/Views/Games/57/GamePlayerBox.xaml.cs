using Client.Controllers;
using Network.Packets.Games._57;
using System.Windows.Controls;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for GamePlayerBox.xaml
    /// </summary>
    public partial class GamePlayerBox : UserControl
    {
        public GamePlayerBox()
        {
            InitializeComponent();
        }

        public void Update(_57_GamePacket.Player player)
        {
            UsernameLabel.Content = player.Username;
            ValueInputBox.Text = player.Value;
            ValueInputBox.IsEnabled = player.Username == MainController.Instance!.Username;
            PointLabel.Content = player.Points;
        }
    }
}

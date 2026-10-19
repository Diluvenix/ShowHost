using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for LobbyPlayerBox.xaml
    /// </summary>
    public partial class LobbyPlayerBox : UserControl
    {
        public LobbyPlayerBox()
        {
            InitializeComponent();
        }

        public void Update(_57_LobbyPacket.Player player)
        {
            UsernameLabel.Content = player.Username;

            if (player.Ping > 0)
            {
                PingLabel.Content = $"{player.Ping} ms";
                OfflineBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                PingLabel.Content = "Waiting for recovery";
                OfflineBorder.Visibility = Visibility.Visible;
            }
        }

        public void Update(_57_PingPacket.Player player)
        {
            UsernameLabel.Content = player.Username;

            if (player.Ping > 0)
            {
                PingLabel.Content = $"{player.Ping} ms";
                OfflineBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                PingLabel.Content = "Waiting for recovery";
                OfflineBorder.Visibility = Visibility.Visible;
            }
        }
    }
}

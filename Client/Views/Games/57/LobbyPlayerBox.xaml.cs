using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using Network.Packets;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for LobbyPlayerBox.xaml
    /// </summary>
    public partial class LobbyPlayerBox : UserControl
    {
        private int color;
        private SolidColorBrush colorBrush = new();

        public LobbyPlayerBox()
        {
            InitializeComponent();
        }

        public void Update(_57_Player player)
        {
            UsernameLabel.Content = player.Username;

            if (color != player.Color)
            {
                color = player.Color;
                colorBrush = new SolidColorBrush(Color.FromRgb(
                    (byte)((color >> 16) & 0xFF),
                    (byte)((color >> 8) & 0xFF),
                    (byte)((color >> 0) & 0xFF)
                ));
                ColorBorder.Background = colorBrush;
            }

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

        public void Update(PingPacket.Player player)
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

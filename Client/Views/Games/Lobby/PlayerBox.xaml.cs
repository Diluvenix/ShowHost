using Network.Packets;
using Network.Packets.Games.Lobby;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views.Games.Lobby
{
    /// <summary>
    /// Interaction logic for PlayerBox.xaml
    /// </summary>
    public partial class PlayerBox : UserControl
    {
        private string username = string.Empty;
        private int ping;
        private Lobby_PlayerListPacket.PlayerRole role;

        public string Username => username;
        public int PingRank => ping > 0 ? 1 : 0;
        public int RoleRank 
            => role switch 
            { 
                Lobby_PlayerListPacket.PlayerRole.Moderator => 1, 
                Lobby_PlayerListPacket.PlayerRole.Player => 0, 
                _ => 0
            };

        public PlayerBox()
        {
            InitializeComponent();
        }

        public void Update(Lobby_PlayerListPacket.Player player)
        {
            username = player.Username;
            UsernameLabel.Content = player.Username;

            ping = player.Ping;
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

            role = player.Role;
            switch (player.Role)
            {
                case Lobby_PlayerListPacket.PlayerRole.Player:
                    PlayerLogo.Visibility = Visibility.Visible;
                    ModeratorLogo.Visibility = Visibility.Collapsed;
                    ModeratorBorder.Visibility = Visibility.Collapsed;
                    break;
                case Lobby_PlayerListPacket.PlayerRole.Moderator:
                    PlayerLogo.Visibility = Visibility.Collapsed;
                    ModeratorLogo.Visibility = Visibility.Visible;
                    ModeratorBorder.Visibility = Visibility.Visible;
                    break;
            }
        }

        public void Update(PingPacket.Player player)
        {
            username = player.Username;
            UsernameLabel.Content = player.Username;

            ping = player.Ping;
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

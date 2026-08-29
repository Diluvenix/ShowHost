using Client.Controllers;
using Network.Packets;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views.UserControls
{
    /// <summary>
    /// Interaction logic for LobbyPlayerBox.xaml
    /// </summary>
    public partial class LobbyPlayerBox : UserControl
    {
        private string username = string.Empty;
        public string Username 
        {
            set
            {
                username = value;
                UsernameLabel.Content = value;
            }
            get => username;
        }

        private int ping;
        public int Ping
        {
            set
            {
                ping = value;
                if (value > 0)
                {
                    PingLabel.Content = $"{value} ms";
                    OfflineBorder.Visibility = Visibility.Collapsed;
                }
                else
                {
                    PingLabel.Content = "Waiting for recovery";
                    OfflineBorder.Visibility = Visibility.Visible;
                }
            }
            get => ping;
        }
        public int PingRank => ping > 0 ? 1 : 0;

        private LobbyPacket.PlayerRole role;
        public LobbyPacket.PlayerRole Role
        {
            set
            {
                role = value;
                switch (value)
                {
                    case LobbyPacket.PlayerRole.Player:
                        PlayerLogo.Visibility = Visibility.Visible;
                        ModeratorLogo.Visibility = Visibility.Collapsed;
                        ModeratorBorder.Visibility = Visibility.Collapsed;
                        break;
                    case LobbyPacket.PlayerRole.Moderator:
                        PlayerLogo.Visibility = Visibility.Collapsed;
                        ModeratorLogo.Visibility = Visibility.Visible;
                        ModeratorBorder.Visibility = Visibility.Visible;
                        break;
                }
            }
        }
        public int RoleRank 
            => role switch 
            { 
                LobbyPacket.PlayerRole.Moderator => 1, 
                LobbyPacket.PlayerRole.Player => 0, 
                _ => 0
            };

        public LobbyPlayerBox(string username, int ping)
        {
            InitializeComponent();

            Username = username;
            Ping = ping;
        }
    }
}

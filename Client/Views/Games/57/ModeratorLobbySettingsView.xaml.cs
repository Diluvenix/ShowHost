using Client.Controllers;
using Network;
using Network.Packets.Games._57;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for ModeratorLobbySettingsView.xaml
    /// </summary>
    public partial class ModeratorLobbySettingsView : UserControl
    {
        private string lobbyName = string.Empty;
        private int playersMax;

        private readonly NetworkClient client;

        public ModeratorLobbySettingsView()
        {
            InitializeComponent();

            client = MainController.Instance!.Client;

            LobbyNameBox.LostFocus += LobbyNameBox_LostFocus;
            LobbyNameBox.KeyDown += LobbyNameBox_KeyDown;

            PlayersMaxBox.LostFocus += PlayersMaxBox_LostFocus;
            PlayersMaxBox.KeyDown += PlayersMaxBox_KeyDown;
            PlayersMaxBox.PreviewTextInput += (_, e) =>
            {
                if (!e.Text.All(char.IsDigit))
                    e.Handled = true;
            };
        }

        private void PlayersMaxBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PlayersMaxBox_LostFocus(sender, e);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }

        private void PlayersMaxBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PlayersMaxBox.Text.Trim(), out int playersMax) || playersMax < 1)
            {
                PlayersMaxBox.Text = this.playersMax.ToString();
                return;
            }

            if (playersMax == this.playersMax)
                return;

            _ = client.SendPacketAsync(new _57_LobbySettingsUpdatePacket() { PlayersMax = playersMax }, MainController.Instance!.Cts.Token);
        }

        public void SetLobbyName(string lobbyName)
        {
            this.lobbyName = lobbyName;
            LobbyNameBox.Text = this.lobbyName;
        }

        public void SetPlayersMax(int playersMax)
        {
            this.playersMax = playersMax;
            PlayersMaxBox.Text = this.playersMax.ToString();
        }

        private void LobbyNameBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(LobbyNameBox.Text))
            {
                LobbyNameBox.Text = lobbyName;
                return;
            }

            if (LobbyNameBox.Text == lobbyName)
                return;

            _ = client.SendPacketAsync(new _57_LobbySettingsUpdatePacket() { Name = LobbyNameBox.Text }, MainController.Instance!.Cts.Token);
        }
        private void LobbyNameBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LobbyNameBox_LostFocus(sender, e);
                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }
    }
}

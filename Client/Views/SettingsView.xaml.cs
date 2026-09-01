using Client.Controllers;
using Network.Packets;
using System.Windows;
using System.Windows.Controls;

namespace Client.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();

            ModkeyButton.Click += (_, _) =>
            {
                _ = MainController.Instance!.Client.SendPacketAsync(new ModerationSecretPacket() { Type = ModerationSecretPacket.SecretType.Moderator });
            };
            ModkeyCopyButton.Click += (_, _) =>
            {
                Clipboard.SetText(ModkeyBox.Text);
            };

            RecoveryButton.Click += (_, _) =>
            {
                if (PlayersDropDown.SelectedItem is not string username) return;
                _ = MainController.Instance!.Client.SendPacketAsync(new ModerationSecretPacket() { Type = ModerationSecretPacket.SecretType.Recovery, Target = username });
            };
            KickButton.Click += (_, _) =>
            {
                if (PlayersDropDown.SelectedItem is not string username) return;
                _ = MainController.Instance!.Client.SendPacketAsync(new ModerationPacket() { Target = username, Action = ModerationPacket.ModerationAction.Kick });
            };
            DeleteButton.Click += (_, _) =>
            {
                if (PlayersDropDown.SelectedItem is not string username) return;
                MessageBoxResult result = MessageBox.Show($"Delete player connection for user {username}?", "Deletion request", MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
                if (result == MessageBoxResult.OK)
                    _ = MainController.Instance!.Client.SendPacketAsync(new ModerationPacket() { Target = username, Action = ModerationPacket.ModerationAction.Delete });
            };
            RecoveryCopyButton.Click += (_, _) =>
            {
                Clipboard.SetText(RecoveryBox.Text);
            };
        }

        internal void SetPlayers(IEnumerable<string> players)
        {
            string selected = (string)PlayersDropDown.SelectedValue;

            if (!PlayersDropDown.IsDropDownOpen)
            {
                PlayersDropDown.Items.Clear();
                foreach (string player in players)
                {
                    PlayersDropDown.Items.Add(player);
                }
            }

            PlayersDropDown.SelectedValue = selected;
        }
    }
}

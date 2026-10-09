using Client.Controllers;
using Network;
using Network.Packets.Games._57;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for SettingsPlayerBox.xaml
    /// </summary>
    public partial class SettingsPlayerBox : UserControl
    {
        private string username = string.Empty;
        private UInt32 color;
        private SolidColorBrush colorBrush = new();
        private readonly NetworkClient client;

        public SettingsPlayerBox()
        {
            InitializeComponent();

            client = MainController.Instance!.Client;

            ColorInputBox.LostFocus += ColorInputBox_LostFocus;
        }

        private void ColorInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ColorInputBox.Text))
            {
                ColorInputBox.Text = color.ToString("X6");
                return;
            }

            string hex = ColorInputBox.Text.Trim().TrimStart('#');

            if (!UInt32.TryParse(hex, NumberStyles.HexNumber, null, out UInt32 newColor))
            {
                ColorInputBox.Text = color.ToString("X6");
                return;
            }

            if (newColor == color)
            {
                return;
            }

            _ = client.SendPacketAsync(new _57_LobbySettingsUpdatePacket() { ColorUsername = username, Color = newColor }, MainController.Instance!.Cts.Token);
        }

        public void Update(_57_LobbyPacket.Player player)
        {
            username = player.Username;
            UsernameLabel.Content = username;

            if (color != player.Color)
            {
                color = player.Color;
                colorBrush = new SolidColorBrush(Color.FromRgb(
                    (byte)((color >> 16) & 0xFF),
                    (byte)((color >> 8) & 0xFF),
                    (byte)((color >> 0) & 0xFF)
                ));
                ColorBorder.Background = colorBrush;

                ColorInputBox.Text = color.ToString("X6");
            }
        }
    }
}

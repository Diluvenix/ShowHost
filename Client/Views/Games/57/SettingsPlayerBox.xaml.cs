using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for SettingsPlayerBox.xaml
    /// </summary>
    public partial class SettingsPlayerBox : UserControl
    {
        private UInt32 color;
        private SolidColorBrush colorBrush = new();

        public SettingsPlayerBox()
        {
            InitializeComponent();
        }

        public void Update(_57_LobbyPacket.Player player)
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
        }
    }
}

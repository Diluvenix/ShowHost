using Client.Controllers;
using Network;
using Network.Packets.Games._57;
using System.Windows.Controls;
using System.Windows.Input;

namespace Client.Views.Games._57
{
    /// <summary>
    /// Interaction logic for GamePlayerBox.xaml
    /// </summary>
    public partial class GamePlayerBox : UserControl
    {
        private string username;

        private readonly NetworkClient client;

        public GamePlayerBox()
        {
            InitializeComponent();

            client = MainController.Instance!.Client;
            username = string.Empty;

            ValueInputBox.TextChanged += ValueInputBox_TextChanged;
        }

        private void ValueInputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = client.SendPacketAsync(
                new _57_GameValueUpdatePacket() 
                { 
                    Value = ValueInputBox.Text, 
                    Target = username
                }, 
                MainController.Instance!.Cts.Token
            );
        }

        public void Update(_57_GamePacket.Player player)
        {
            username = player.Username;

            UsernameLabel.Content = player.Username;
            ValueInputBox.Text = player.Value;
            ValueInputBox.IsEnabled = player.Username == MainController.Instance!.Username || MainController.Instance!.IsModerator;
            PointLabel.Content = player.Points;
        }
    }
}

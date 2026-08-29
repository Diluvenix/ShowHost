using Client.Views;
using Network;
using Network.Packets;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers
{
    internal class ConnectController : IController
    {
        private readonly ConnectView view;
        public UserControl View => view;
        public Geometry? Path => null;

        private readonly NetworkClient client;

        public ConnectController()
        {
            view = new ConnectView();
            client = MainController.Instance!.Client;

            view.AddConnectButtonClickEventHandler((_, _) => _ = TryConnect());
        }

        private async Task TryConnect()
        {
            view.SetControllsEnabled(false);

            string serverAddress = view.ServerAddress;
            if (string.IsNullOrWhiteSpace(serverAddress))
            {
                view.ErrorMessage = "Error: Server address is unset.";
                view.SetControllsEnabled(true);
                return;
            }
            int port = view.Port;
            if (0 >= port || port > 65535)
            {
                view.ErrorMessage = "Error: Port is invalid.";
                view.SetControllsEnabled(true);
                return;
            }
            string username = view.Username;
            if (username.Length == 0)
            {
                view.ErrorMessage = "Error: Username is missing.";
                view.SetControllsEnabled(true);
                return;
            }

            int idx = username.AsSpan().IndexOfAnyExceptInRange('!', '~');
            if (idx != -1)
            {
                view.ErrorMessage = $"Error: Username has invalid character '{username[idx]}'.";
                view.SetControllsEnabled(true);
                return;
            }
            if (username.Length > 10)
            {
                view.ErrorMessage = "Error: Username may only be 10 characters long.";
                view.SetControllsEnabled(true);
                return;
            }

            view.HintMessage = "Connecting...";
            if (!(await client.TryConnect(view.ServerAddress, view.Port)).Success)
            {
                view.ErrorMessage = "Error: Server is unreachable.";
                view.SetControllsEnabled(true);
                return;
            }

            view.HintMessage = "Encrypting channel...";

            if (!(await client.DoHandshake()).Success)
            {
                view.ErrorMessage = "Error: Server connection aborted.";
                view.SetControllsEnabled(true);
                return;
            }

            view.HintMessage = "Register user...";

            string secret = "";
            ConnectMode connectMode = ConnectMode.Player;

            if (view.IsRecoveryChecked)
            {
                connectMode = ConnectMode.Recovery;
                secret = view.RecoveryKey;
            }
            else if (view.IsModChecked)
            {
                connectMode = ConnectMode.Moderator;
                secret = view.ModKey;
            }

            if (!(await client.SendPacketAsync(new ConnectPacket() { Username = username, Mode = connectMode, Secret = secret })).Success)
            {
                view.ErrorMessage = "Error: Server connection aborted.";
                view.SetControllsEnabled(true);
                return;
            }
        }

        public void Dispose() {}

        public async Task HandleAsync<T>(T packet)
        {
            switch (packet)
            {
                case ConnectPacket connectPacket:
                    view.Dispatcher.Invoke(() => HandleConnectPacket(connectPacket));
                    break;
                default:
                    return;
            }
        }

        private void HandleConnectPacket(ConnectPacket packet)
        {
            if (packet.Mode == ConnectMode.NONE)
            {
                view.ErrorMessage = string.Format("Error: {0}", packet.Secret??string.Empty);
                view.SetControllsEnabled(true);
                return;
            }

            view.SuccessMessage = "Sucessfully connected";
            MainController.Instance!.IsModerator = packet.Mode == ConnectMode.Moderator;
            MainController.Instance!.Username = packet.Username;
            MainController.Instance!.SetController(MainController.ControllerType.Lobby);
        }
    }
}

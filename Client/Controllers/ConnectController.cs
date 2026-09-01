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
            if (!(await client.TryConnectAsync(view.ServerAddress, view.Port)).Success)
            {
                view.ErrorMessage = "Error: Server is unreachable.";
                view.SetControllsEnabled(true);
                return;
            }

            view.HintMessage = "Encrypting channel...";

            if (!(await client.DoHandshakeAsync()).Success)
            {
                view.ErrorMessage = "Error: Server connection aborted.";
                view.SetControllsEnabled(true);
                return;
            }

            view.HintMessage = "Register user...";

            string secret = "";
            AuthenticationPacket.AuthenticationType connectMode = AuthenticationPacket.AuthenticationType.Player;

            if (view.IsRecoveryChecked)
            {
                connectMode = AuthenticationPacket.AuthenticationType.Recovery;
                secret = view.RecoveryKey;
            }
            else if (view.IsModChecked)
            {
                connectMode = AuthenticationPacket.AuthenticationType.Moderator;
                secret = view.ModKey;
            }

            if (!(await client.SendPacketAsync(new AuthenticationPacket() { Username = username, Type = connectMode, Secret = secret })).Success)
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
                case AuthenticationPacket connectPacket:
                    view.Dispatcher.Invoke(() => HandleConnectPacket(connectPacket));
                    break;
                default:
                    return;
            }
        }

        private void HandleConnectPacket(AuthenticationPacket packet)
        {
            if (packet.Type == AuthenticationPacket.AuthenticationType.NONE)
            {
                view.ErrorMessage = string.Format("Error: {0}", packet.Secret??string.Empty);
                view.SetControllsEnabled(true);
                return;
            }

            view.SuccessMessage = "Sucessfully connected";
            MainController.Instance!.IsModerator = packet.Type == AuthenticationPacket.AuthenticationType.Moderator;
            MainController.Instance!.Username = packet.Username;
        }
    }
}

using _57 = Client.Controllers.Games._57;
using Lobby = Client.Controllers.Games.Lobby;
using Network;
using Network.Packets;
using System.Windows;

namespace Client.Controllers
{
    internal class MainController : IDisposable
    {
        public static MainController? Instance;

        public bool IsModerator = false;
        public string Username = string.Empty;

        public NetworkClient Client { get; private set; }
        public readonly CancellationTokenSource Cts;

        private readonly MainWindow mainWindow;
        private IController currentController;

        public MainController(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            Client = new NetworkClient();
            Instance = this;

            Cts = new CancellationTokenSource();
            _ = Handle(Cts.Token);

            currentController = new ConnectController();
            mainWindow.Border.Child = currentController.View;
        }

        public void SetView(SetViewPacket.ViewType viewType)
        {
            currentController.Dispose();
            mainWindow.Dispatcher.Invoke(() =>
            {
                switch (viewType)
                {
                    case SetViewPacket.ViewType.Connect:
                        Client.Dispose();
                        Client = new NetworkClient();
                        currentController = new ConnectController();
                        break;
                    case SetViewPacket.ViewType.Lobby:
                        SetLobbyController();
                        break;
                    case SetViewPacket.ViewType._57_Lobby:
                        Set57LobbyController();
                        break;
                }
                mainWindow.Border.Child = currentController.View;
            });
        }

        private void SetLobbyController()
            => currentController = IsModerator
                ? new MultiViewPanelController(
                    [
                        new Lobby.PlayerPlayersController(),
                        new Lobby.ModeratorGamesController(),
                        new Lobby.ModeratorSettingsController(),
                    ]
                )
                : new MultiViewPanelController(
                    [
                        new Lobby.PlayerPlayersController(),
                        new Lobby.PlayerGamesController(),
                    ]
                );
        private void Set57LobbyController()
            => currentController = IsModerator
                ? new MultiViewPanelController(
                    [
                        new _57.PlayerLobbyController(),
                        new _57.ModeratorLobbySettingsController(),
                    ]
                )
                : new _57.PlayerLobbyController();

        public void Dispose()
        {
            Cts.Cancel();

            currentController.Dispose();
            Cts.Dispose();
            Client.Dispose();
            Instance = null;
        }

        private async Task Handle(CancellationToken ct)
        {
            while (!Client.IsConnected && !ct.IsCancellationRequested)
            {
                await Task.Delay(100, ct);
            }

            while (!ct.IsCancellationRequested)
            {
                Result<object> result = await Client.ReceivePacketAsync(ct);

                if (!result.Success)
                {
                    MessageBox.Show(result.Error?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
                object packet = result.Value!;

                switch (packet)
                {
                    case HeartbeatPacket heartbeatPacket:
                        await Client.SendPacketAsync(heartbeatPacket, ct);
                        continue;
                    case SetViewPacket setViewPacket:
                        SetView(setViewPacket.View);
                        continue;
                }

                await currentController.HandleAsync(packet, ct);
            }
        }
    }
}

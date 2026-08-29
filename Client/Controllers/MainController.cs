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
        public string[] RelatedUsers = [];

        public readonly NetworkClient Client = new();
        public readonly CancellationTokenSource Cts;
        private readonly Thread handlerThread;

        private readonly MainWindow mainWindow;
        private IController? currentController;

        public MainController(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;

            Cts = new CancellationTokenSource();
            handlerThread = new Thread(() => _ = Handle(Cts.Token));
            handlerThread.Start();
        }

        public void SetController(ControllerType controller)
        {
            switch (controller)
            {
                case ControllerType.Connect:
                    SetControllerConnect();
                    break;
                case ControllerType.Lobby:
                    SetControllerLobby();
                    break;
            }
        }

        private void SetControllerConnect()
        {
            currentController?.Dispose();
            currentController = new ConnectController();
            mainWindow.Border.Child = currentController.View;
        }
        private void SetControllerLobby()
        {
            currentController?.Dispose();
            if (IsModerator)
            {
                currentController = new ModeratorPanelController(
                    [
                        new LobbyController(),
                        new SettingsController(),
                        new GameListController(),
                    ]
                );
            }
            else
            {
                currentController = new LobbyController();
            }
            mainWindow.Border.Child = currentController.View;
        }

        public void Dispose()
        {
            Cts.Cancel();
            handlerThread.Join();

            currentController?.Dispose();
            Cts.Dispose();
            Client.Dispose();
            Instance = null;
        }

        private async Task Handle(CancellationToken ct)
        {
            try
            {
                while ((!Client.IsConnected || currentController is null) && !ct.IsCancellationRequested)
                {
                    await Task.Delay(100, ct);
                }

                while (!ct.IsCancellationRequested)
                {
                    Result<object> result = await Client.ReceivePacketAsync().WaitAsync(ct);

                    if (!result.Success)
                    {
                        MessageBox.Show(result.Error?.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        continue;
                    }
                    object packet = result.Value!;

                    switch (packet)
                    {
                        case HeartbeatPacket heartbeatPacket:
                            await Client.SendPacketAsync(heartbeatPacket).WaitAsync(ct);
                            continue;
                    }

                    await (currentController?.HandleAsync(packet).WaitAsync(ct) ?? Task.CompletedTask);
                }
            }
            catch (OperationCanceledException) { }
        }

        public enum ControllerType
        {
            Connect,
            Lobby,
        }
    }
}

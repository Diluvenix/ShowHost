using Network;
using Network.Packets;
using Serilog;
using Server.Keys;
using Server.Services;

namespace Server.Model
{
    internal class Player : IDisposable
    {
        private NetworkClient? client;
        public IService? Service;

        public int PingMS { get; private set; }
        public bool IsConnected => PingMS > 0;
        public string Username;
        public PlayerRole Role;

        public readonly KeyManager KeyManager;

        private CancellationTokenSource cts = new();
        private Task? handlerTask;
        private DateTime lastHeartbeat;

        private readonly ILogger networkLogger;


        public Player(NetworkClient client, string username, PlayerRole role)
        {
            this.client = client;
            Username = username;
            Role = role;
            PingMS = 1;

            networkLogger = Log.ForContext("SourceContext", "Network").ForContext("Player", Username);

            KeyManager = new();
            handlerTask = Handle(cts.Token);
        }
        public void Dispose()
        {
            Disconnect("Deleted").Wait();

            cts.Cancel();
            handlerTask?.Wait();
            cts.Dispose();

            client?.Dispose();
        }

        public async Task SendPacketAsync<T>(T packet)
        {
            if (IsConnected)
                await (client?.SendPacketAsync(packet) ?? Task.CompletedTask);
        }
        public void SetClient(NetworkClient client)
        {
            cts.Cancel();
            handlerTask?.Wait();
            cts.Dispose();
            cts = new();

            this.client?.Dispose();
            this.client = client;
            PingMS = 1;
            handlerTask = Handle(cts.Token);
        }

        public async Task Disconnect(string reason)
        {
            if (!IsConnected) return;

            await (client?.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Connect }) ?? Task.CompletedTask);
            cts.Cancel();

            PingMS = 0;
            client = null;
            networkLogger.ForContext("Reason", reason).Information("Player lost connection", Username);
        }

        public static bool IsUsernameValid(string username) 
            => !string.IsNullOrEmpty(username) && username.Length <= 10 && username.AsSpan().IndexOfAnyExceptInRange('!', '~') == -1;

        private async Task Handle(CancellationToken ct)
        {
            Task heartbeatTask = Heartbeat(ct);

            try
            {
                while (client is not null && !ct.IsCancellationRequested)
                {
                    Result<object> result = await client.ReceivePacketAsync().WaitAsync(ct);
                    if (!result.Success)
                    {
                        continue;
                    }

                    object packet = result.Value!;
                    if (packet is HeartbeatPacket heartbeatPacket)
                    {
                        lastHeartbeat = DateTime.UtcNow;
                        TimeSpan ping = lastHeartbeat - new DateTime(heartbeatPacket.Timestamp);
                        PingMS = (int)ping.TotalMilliseconds;

                        continue;
                    }

                    networkLogger.ForContext("Packet", packet.GetType()).Debug("Recieved new Packet");
                    if (await (Server.Instance?.TryHandleServerPackageAsync(packet, this).WaitAsync(ct) ?? Task.FromResult(false)))
                        continue;

                    await (Service?.HandleAsync(packet, this).WaitAsync(ct) ?? Task.CompletedTask);
                }
            }
            catch (OperationCanceledException) { }

            await heartbeatTask;
        }

        private async Task Heartbeat(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
            lastHeartbeat = DateTime.UtcNow;
            PingMS = 1;

            try
            {
                while (client is not null && !ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
                {
                    await SendPacketAsync(new HeartbeatPacket() { Timestamp = DateTime.UtcNow.Ticks });

                    TimeSpan ping = DateTime.UtcNow - lastHeartbeat;
                    if (ping > TimeSpan.FromSeconds(10))
                        await Disconnect("Timeout").WaitAsync(ct);
                }
            }
            catch (OperationCanceledException) { }
        }
    }

    internal enum PlayerRole
    {
        Player,
        Moderator
    }
}

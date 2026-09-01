using Network;
using Network.Packets;
using Serilog;
using Server.Keys;
using Server.Services;

namespace Server.Model
{
    internal class Player : IDisposable
    {
        public IService Service { get; private set; } = Context.Lobby;
        public int PingMS { get; private set; }
        public bool IsConnected => PingMS > 0;
        public readonly string Username;
        public readonly PlayerRole Role;
        public readonly KeyManager KeyManager;

        private CancellationTokenSource cts = new();
        private Task handlerTask;
        private NetworkClient? client;
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
            handlerTask = HandleAsync(cts.Token);
        }
        public void Dispose()
        {
            try
            {
                cts.Cancel();
            }
            catch(ObjectDisposedException) { }

            DisconnectAsync("Deleted", CancellationToken.None).Wait();

            client?.Dispose();
        }

        public async Task SetServiceAsync(IService newService, CancellationToken ct)
        {
            await Service.RemovePlayerAsync(this, ct);
            Service = newService;
            await newService.AddPlayerAsync(this, ct);
        }

        public async Task SendPacketAsync<T>(T packet, CancellationToken ct)
        {
            if (IsConnected && client is not null)
                await client.SendPacketAsync(packet, ct);
        }
        public void SetClient(NetworkClient client)
        {
            cts.Cancel();
            try
            {
                handlerTask.Wait();
            } 
            catch (AggregateException) { }
            cts.Dispose();
            cts = new();

            this.client?.Dispose();
            this.client = client;
            PingMS = 1;
            handlerTask = HandleAsync(cts.Token);
        }

        public async Task DisconnectAsync(string reason, CancellationToken ct)
        {
            if (!IsConnected) return;

            if (client is not null)
                await client.SendPacketAsync(new SetViewPacket() { View = SetViewPacket.ViewType.Connect }, ct);
            cts.Cancel();

            PingMS = 0;
            client = null;
            networkLogger.ForContext("Reason", reason).Information("Player lost connection", Username);
        }

        public static bool IsUsernameValid(string username) 
            => !string.IsNullOrEmpty(username) && username.Length <= 10 && username.AsSpan().IndexOfAnyExceptInRange('!', '~') == -1;

        private async Task HandleAsync(CancellationToken ct)
        {
            _ = HeartbeatAsync(ct);

            try
            {
                while (client is not null && !ct.IsCancellationRequested)
                {
                    Result<object> result = await client.ReceivePacketAsync(ct);
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

                    networkLogger.ForContext("Packet", packet.GetType().Name).Debug("Recieved new Packet");
                    if (await Server.TryHandleServerPackageAsync(packet, this, ct))
                        continue;

                    await Service.HandleAsync(packet, this, ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        private async Task HeartbeatAsync(CancellationToken ct)
        {
            PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
            lastHeartbeat = DateTime.UtcNow;
            PingMS = 1;

            try
            {
                while (client is not null && !ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
                {
                    await SendPacketAsync(new HeartbeatPacket() { Timestamp = DateTime.UtcNow.Ticks }, ct);

                    TimeSpan ping = DateTime.UtcNow - lastHeartbeat;
                    if (ping > TimeSpan.FromSeconds(10))
                        await DisconnectAsync("Timeout", ct);
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

using Network;
using Network.Packets;
using Serilog;
using Server.Commands;
using Server.Keys;
using Server.Model;
using System.Net;
using System.Net.Sockets;

namespace Server
{
    internal class Server : IDisposable
    {
        public static Server? Instance { get; private set; }

        public readonly ServerContext Context;
        private readonly CancellationTokenSource cts = new();

        private readonly TcpListener serverListener;
        private readonly Task connectionTask;

        private readonly ILogger systemLogger = Log.ForContext("SourceContext", "System");
        private readonly ILogger networkLogger = Log.ForContext("SourceContext", "Network");
        private readonly ILogger authLogger = Log.ForContext("SourceContext", "Auth");

        public Server(int port)
        {
            serverListener = new(IPAddress.Any, port);
            Context = new ServerContext();

            serverListener.Start();

            connectionTask = HandleConnections(cts.Token);
            HandleInput(cts.Token);

            systemLogger.Information("Server started Port={0}", port);
            systemLogger.Information("Press Ctrl-C to shutdown the server at any time.");
            CommandManager.Execute("modkey", this);
            Console.WriteLine("Use 'help' to get a list of available commands");

            Instance = this;
        }

        public void Dispose()
        {
            if (!cts.IsCancellationRequested)
            {
                systemLogger.Information("Closing server...");
                cts.Cancel();

                connectionTask.Wait();

                serverListener.Stop();
                serverListener.Dispose();

                Context.Dispose();

                systemLogger.Information("Server closed");
            }
        }

        private async Task HandleConnections(CancellationToken ct)
        {
            networkLogger.Information("Waiting for incoming connections...");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    TcpClient tcpClient = await serverListener.AcceptTcpClientAsync(ct);
                    networkLogger.Debug("Handling new connection Client=({0})", tcpClient.Client.RemoteEndPoint);
                    _ = AuthenticateUser(new NetworkClient(tcpClient), tcpClient);
                }
            }
            catch (OperationCanceledException) { }
        }

        public void HandleInput(CancellationToken ct)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        string command = Console.ReadLine() ?? "";
                        CommandManager.Execute(command, this);
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        public async Task AuthenticateUser(NetworkClient client, TcpClient tcpClient)
        {
            string endpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "unknown";

            async Task Reject<T>(string reason, Exception? exception = null, T? packet = default, object?[]? properties = null)
            {
                if (exception is not null && packet is not null)
                {
                    authLogger.Warning(exception, reason, properties);
                    await client.SendPacketAsync(packet);
                    client.Dispose();
                }
                else if (exception is not null)
                {
                    authLogger.Warning(exception, reason, properties);
                    client.Dispose();
                }
                else if (packet is not null)
                {
                    authLogger.Warning(reason, properties);
                    await client.SendPacketAsync(packet);
                    client.Dispose();
                }
                else
                {
                    authLogger.Warning(reason, properties);
                    client.Dispose();
                }
            }


            Result result = await client.ReceiveHandshake();
            if (!result.Success)
            {
                networkLogger.Warning(result.Error!, "Error during ECDH handshake Client=({0})", endpoint);
                client.Dispose();
                return;
            }

            Result<object> packetResult = await client.ReceivePacketAsync();
            if (!packetResult.Success)
            {
                await Reject<object>("Error whilst waiting for user authentication Client=({0})", exception: packetResult.Error, properties: [endpoint]);
                return;
            }
            if (packetResult.Value is not ConnectPacket packet)
            {
                await Reject<object>("Unexpected Packet during authentication Client=({0})", properties: [endpoint]);
                return;
            }

            string username = packet.Username;
            Base32Token token;
            switch (packet.Mode)
            {
                case ConnectMode.Moderator:
                    try
                    {
                        token = Base32Token.FromCode(packet.Secret ?? "");

                        if (!Context.ModeratorKeys.TryUseKey(token.Hash))
                        {
                            await Reject(
                                "Invalid moderator key Client=({0})", properties: [endpoint],
                                packet: new ConnectPacket() { Secret = "Invalid moderator key", Mode = ConnectMode.NONE }
                            );
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        await Reject(
                            "Invalid moderator key Client=({0})", properties: [endpoint],
                            exception: e,
                            packet: new ConnectPacket() { Secret = "Invalid moderator key", Mode = ConnectMode.NONE }
                        );
                        return;
                    }
                    goto case ConnectMode.Player;
                case ConnectMode.Player:
                    if (!Player.IsUsernameValid(username))
                    {
                        await Reject(
                            "Invalid username Client=({0}) Username={1}", properties: [endpoint, username],
                            packet: new ConnectPacket() { Secret = "Invalid username", Mode = ConnectMode.NONE }
                        );
                        return;
                    }

                    PlayerRole role = packet.Mode == ConnectMode.Moderator ? PlayerRole.Moderator : PlayerRole.Player;
                    Player? player = new(client, username, role);
                    if (!Context.Players.TryAdd(username, player))
                    {
                        await Reject(
                            "Username already taken Client=({0}) Username={1}", properties: [endpoint, username],
                            packet: new ConnectPacket() { Secret = "Username already taken", Mode = ConnectMode.NONE }
                        );
                        return;
                    }

                    await client.SendPacketAsync(new ConnectPacket() { Username = username, Mode = packet.Mode });
                    authLogger.Information("Player authenticated Client=({0}) Username={1} Role={2}", endpoint, username, role);

                    await Context.Lobby.AddPlayerAsync(player);
                    player.Service = Context.Lobby;
                    return;
                case ConnectMode.Recovery:
                    if (!Context.Players.TryGetValue(username, out player))
                    {
                        await Reject(
                            "Username is unknown Client=({0}) Username={1}", properties: [endpoint, username],
                            packet: new ConnectPacket() { Secret = "Unknown username", Mode = ConnectMode.NONE }
                        );
                        return;
                    }

                    token = Base32Token.FromCode(packet.Secret ?? "");
                    if (!player.KeyManager.TryUseKey(token.Hash))
                    {
                        await Reject(
                            "Invalid recovery key Client=({0}) Username={1}", properties: [endpoint, username],
                            packet: new ConnectPacket() { Secret = "Invalid recovery key", Mode = ConnectMode.NONE }
                        );
                        return;
                    }

                    player.SetClient(client);

                    await client.SendPacketAsync(new ConnectPacket()
                    {
                        Username = username,
                        Mode = player.Role switch
                        {
                            PlayerRole.Player => ConnectMode.Player,
                            PlayerRole.Moderator => ConnectMode.Moderator,
                            _ => ConnectMode.Player,
                        }
                    });
                    await (player.Service?.RecoverAsync(player) ?? Task.CompletedTask);
                    authLogger.Information("Player recovered Client=({0}) Username={1}", endpoint, username);
                    return;
                default:
                    await Reject(
                        "Invalid connect Client=({0}) ConnectMode={1}", properties: [endpoint, packet.Mode], 
                        packet: new ConnectPacket() { Secret = "Invalid connect mode", Mode = ConnectMode.NONE }
                    );
                    return;
            }
        }

        public async Task<bool> TryHandleServerPackageAsync(object package, Player sender)
        {
            switch (package)
            {
                case KickPacket kickPacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        systemLogger.Warning("Denied access to kick command Username={0}", sender.Username);
                        return true;
                    }
                    if (!Context.Players.TryGetValue(kickPacket.Target, out Player? player))
                    {
                        systemLogger.Warning("Kick: Couldn't find target Username={0} Target={1}", sender.Username, kickPacket.Target);
                        return true;
                    }

                    if (player.IsConnected)
                        player.Disconnect();
                    systemLogger.Information("Kick: Sucessfully kicked target Username={0} Target={1}", sender.Username, kickPacket.Target);
                    break;
                case DeletePacket deletePacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        systemLogger.Warning("Denied access to delete command Username={0}", sender.Username);
                        return true;
                    }
                    if (!Context.Players.TryGetValue(deletePacket.Target, out player))
                    {
                        systemLogger.Warning("Delete: Couldn't find target Username={0} Target={1}", sender.Username, deletePacket.Target);
                        return true;
                    }

                    await (player.Service?.RemovePlayerAsync(player) ?? Task.CompletedTask);
                    Context.Players.Remove(player.Username, out _);
                    player.Dispose();
                    systemLogger.Information("Delete: Sucessfully deleted player connection Username={0} Target={1}", sender.Username, deletePacket.Target);
                    break;
                case GenerateRecoveryKeyPacket generateRecoveryKeyPacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        systemLogger.Warning("Denied access to recover command Username={0}", sender.Username);
                        return true;
                    }
                    if (!Context.Players.TryGetValue(generateRecoveryKeyPacket.Username, out player))
                    {
                        systemLogger.Warning("Recover: Couldn't find target Username={0} Target={1}", sender.Username, generateRecoveryKeyPacket.Username);
                        return true;
                    }

                    Base32Token base32Token = Base32Token.FromRandom();
                    KeyToken keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));
                    player.KeyManager.RegisterKey(keyToken);

                    await sender.SendPacketAsync(new GenerateRecoveryKeyPacket() { Username = generateRecoveryKeyPacket.Username, Key = base32Token.Code });
                    systemLogger.Information("Recover: Sucessfully generated recovery key for target Username={0} Target={1}", sender.Username, generateRecoveryKeyPacket.Username);
                    break;
                case GenerateModeratorKeyPacket generateModeratorKeyPacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        systemLogger.Warning("Denied access to modkey command Username={0}", sender.Username);
                        return true;
                    }

                    base32Token = Base32Token.FromRandom();
                    keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));
                    Context.ModeratorKeys.RegisterKey(keyToken);

                    await sender.SendPacketAsync(new GenerateModeratorKeyPacket() { Key = base32Token.Code });
                    systemLogger.Information("Modkey: Sucessfully generated moderator key Username={0}", sender.Username);
                    break;
                default:
                    return false;
            }

            return true;
        }
    }
}

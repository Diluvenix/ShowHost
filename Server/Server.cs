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
        private readonly CancellationTokenSource cts = new();

        private readonly TcpListener serverListener;
        private readonly Task connectionTask;

        private static readonly ILogger systemLogger = Log.ForContext("SourceContext", "System");
        private static readonly ILogger networkLogger = Log.ForContext("SourceContext", "Network");
        private static readonly ILogger authLogger = Log.ForContext("SourceContext", "Auth");

        public Server(int port)
        {
            serverListener = new(IPAddress.Any, port);

            serverListener.Start();
            systemLogger.ForContext("Port", port).Information("Server started", port);

            connectionTask = HandleConnections(cts.Token);
            HandleInput(cts.Token);

            Console.WriteLine("Press Ctrl-C or use 'stop' to shutdown the server at any time.");
            Console.WriteLine("Use 'help' to get a list of available commands");
            CommandManager.Execute("modkey");
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

                ServerContext.Dispose();

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
                    networkLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint).Debug("Handling new connection");
                    _ = AuthenticateUser(new NetworkClient(tcpClient), tcpClient);
                }
            }
            catch (OperationCanceledException) { }
        }

        private static void HandleInput(CancellationToken ct)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        string command = Console.ReadLine() ?? "";
                        CommandManager.Execute(command);
                    }
                }
                catch (OperationCanceledException) { }
            }, ct);
        }

        private static async Task AuthenticateUser(NetworkClient client, TcpClient tcpClient)
        {
            Result result = await client.ReceiveHandshake();
            if (!result.Success)
            {
                networkLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint).Warning(result.Error, "Error during ECDH handshake");
                client.Dispose();
                return;
            }

            ILogger configuredAuthLogger = authLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint);

            Result<object> packetResult = await client.ReceivePacketAsync();
            if (!packetResult.Success)
            {
                configuredAuthLogger.Warning(packetResult.Error, "Error whilst waiting for user authentication");
                client.Dispose();
                return;
            }
            if (packetResult.Value is not ConnectPacket packet)
            {
                configuredAuthLogger.Warning(packetResult.Error, "Unexpected Packet during authentication");
                client.Dispose();
                return;
            }

            string username = packet.Username;
            configuredAuthLogger = configuredAuthLogger.ForContext("Username", username);
            Base32Token token;
            switch (packet.Mode)
            {
                case ConnectPacket.ConnectMode.Moderator:
                    try
                    {
                        token = Base32Token.FromCode(packet.Secret ?? "");

                        if (!ServerContext.ModeratorKeys.TryUseKey(token.Hash))
                        {
                            configuredAuthLogger.Warning(packetResult.Error, "Invalid moderator key");
                            await client.SendPacketAsync(new ConnectPacket() { Secret = "Invalid moderator key", Mode = ConnectPacket.ConnectMode.NONE });
                            client.Dispose();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        configuredAuthLogger.Warning(e, "Invalid moderator key");
                        await client.SendPacketAsync(new ConnectPacket() { Secret = "Invalid moderator key", Mode = ConnectPacket.ConnectMode.NONE });
                        client.Dispose();
                        return;
                    }
                    goto case ConnectPacket.ConnectMode.Player;
                case ConnectPacket.ConnectMode.Player:
                    if (!Player.IsUsernameValid(username))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Invalid username");
                        await client.SendPacketAsync(new ConnectPacket() { Secret = "Invalid username", Mode = ConnectPacket.ConnectMode.NONE });
                        client.Dispose();
                        return;
                    }

                    PlayerRole role = packet.Mode == ConnectPacket.ConnectMode.Moderator ? PlayerRole.Moderator : PlayerRole.Player;
                    Player? player = new(client, username, role);
                    if (!ServerContext.Players.TryAdd(username, player))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Username already taken");
                        await client.SendPacketAsync(new ConnectPacket() { Secret = "Username already taken", Mode = ConnectPacket.ConnectMode.NONE });
                        client.Dispose();
                        return;
                    }

                    await client.SendPacketAsync(new ConnectPacket() { Username = username, Mode = packet.Mode });
                    configuredAuthLogger.ForContext("Role", role).Information("Player authenticated");

                    await ServerContext.Lobby.AddPlayerAsync(player);
                    return;
                case ConnectPacket.ConnectMode.Recovery:
                    if (!ServerContext.Players.TryGetValue(username, out player))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Username is unknown");
                        await client.SendPacketAsync(new ConnectPacket() { Secret = "Username is unknown", Mode = ConnectPacket.ConnectMode.NONE });
                        client.Dispose();
                        return;
                    }

                    token = Base32Token.FromCode(packet.Secret ?? "");
                    if (!player.KeyManager.TryUseKey(token.Hash))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Invalid recovery key");
                        await client.SendPacketAsync(new ConnectPacket() { Secret = "Invalid recovery key", Mode = ConnectPacket.ConnectMode.NONE });
                        client.Dispose();
                        return;
                    }

                    player.SetClient(client);

                    await client.SendPacketAsync(new ConnectPacket()
                    {
                        Username = username,
                        Mode = player.Role switch
                        {
                            PlayerRole.Moderator => ConnectPacket.ConnectMode.Moderator,
                            _ => ConnectPacket.ConnectMode.Player,
                        }
                    });
                    await player.Service.RecoverAsync(player);
                    configuredAuthLogger.ForContext("Role", player.Role).Information("Player recovered");
                    return;
                default:
                    configuredAuthLogger.Warning(packetResult.Error, "Invalid connect mode");
                    await client.SendPacketAsync(new ConnectPacket() { Secret = "Invalid connect mode", Mode = ConnectPacket.ConnectMode.NONE });
                    client.Dispose();
                    return;
            }
        }

        public static async Task<bool> TryHandleServerPackageAsync(object package, Player sender)
        {
            ILogger configuredSystemLogger = systemLogger.ForContext("Actor", sender.Username);

            switch (package)
            {
                case ModerationPacket moderationPacket:
                    configuredSystemLogger = configuredSystemLogger.ForContext("Action", moderationPacket.Action).ForContext("Target", moderationPacket.Target);

                    if (sender.Role != PlayerRole.Moderator)
                    {
                        configuredSystemLogger.Warning("Access to moderation denied");
                        return true;
                    }
                    if (!ServerContext.Players.TryGetValue(moderationPacket.Target, out Player? player))
                    {
                        configuredSystemLogger.Warning("Couldn't find player to moderate");
                        return true;
                    }
                    if (player == sender)
                    {
                        configuredSystemLogger.Warning("Can't moderate self");
                        return true;
                    }

                    switch (moderationPacket.Action)
                    {
                        case ModerationPacket.ModerationAction.Kick:
                            if (player.IsConnected)
                                await player.Disconnect("Kicked");
                            configuredSystemLogger.Information("Player kicked");
                            break;
                        case ModerationPacket.ModerationAction.Delete:
                            await player.Service.RemovePlayerAsync(player);
                            ServerContext.Players.Remove(player.Username, out _);
                            player.Dispose();
                            configuredSystemLogger.Information("Player deleted");
                            break;
                    }
                    return true;
                case GenerateRecoveryKeyPacket generateRecoveryKeyPacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        configuredSystemLogger.Warning("Access denied to recover command");
                        return true;
                    }
                    configuredSystemLogger = configuredSystemLogger.ForContext("Target", generateRecoveryKeyPacket.Target);
                    if (!ServerContext.Players.TryGetValue(generateRecoveryKeyPacket.Target, out player))
                    {
                        configuredSystemLogger.Warning("Couldn't find player to recover");
                        return true;
                    }

                    Base32Token base32Token = Base32Token.FromRandom();
                    KeyToken keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));
                    player.KeyManager.RegisterKey(keyToken);

                    await sender.SendPacketAsync(new GenerateRecoveryKeyPacket() { Target = generateRecoveryKeyPacket.Target, Key = base32Token.Code });
                    configuredSystemLogger.Information("Recovery key generated");
                    return true;
                case GenerateModeratorKeyPacket:
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        configuredSystemLogger.Warning("Access denied to modkey command");
                        return true;
                    }

                    base32Token = Base32Token.FromRandom();
                    keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));
                    ServerContext.ModeratorKeys.RegisterKey(keyToken);

                    await sender.SendPacketAsync(new GenerateModeratorKeyPacket() { Key = base32Token.Code });
                    configuredSystemLogger.Information("Moderator key generated");
                    return true;
                default:
                    return false;
            }
        }
    }
}

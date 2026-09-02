using Network;
using Network.Packets;
using Serilog;
using Server.Commands;
using Server.Keys;
using System.Net;
using System.Net.Sockets;

namespace Server.Model
{
    internal class Server : IDisposable
    {
        private readonly TcpListener serverListener;

        private static readonly ILogger systemLogger = Log.ForContext("SourceContext", "System");
        private static readonly ILogger networkLogger = Log.ForContext("SourceContext", "Network");
        private static readonly ILogger authLogger = Log.ForContext("SourceContext", "Auth");

        public Server(int port)
        {
            serverListener = new(IPAddress.Any, port);

            serverListener.Start();
            systemLogger.ForContext("Port", port).Information("Server started", port);

            _ = HandleConnectionsAsync(Context.Cts.Token);
            _ = HandleInputAsync(Context.Cts.Token);

            Console.WriteLine("Press Ctrl-C or use 'stop' to shutdown the server at any time.");
            Console.WriteLine("Use 'help' to get a list of available commands");
            CommandManager.Execute("modkey");
        }

        public void Dispose()
        {
            systemLogger.Information("Closing server...");

            serverListener.Stop();
            serverListener.Dispose();

            Context.Dispose();

            systemLogger.Information("Server closed");
        }

        private async Task HandleConnectionsAsync(CancellationToken ct)
        {
            networkLogger.Information("Waiting for incoming connections...");

            while (!ct.IsCancellationRequested)
            {
                TcpClient tcpClient = await serverListener.AcceptTcpClientAsync(ct);
                networkLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint).Debug("Handling new connection");
                _ = AuthenticateUserAsync(new NetworkClient(tcpClient), tcpClient, ct);
            }
        }

        private static async Task HandleInputAsync(CancellationToken ct)
        {
            await Task.Run(() =>
            {
                while (!ct.IsCancellationRequested)
                {
                    string command = Console.ReadLine() ?? "";
                    CommandManager.Execute(command);
                }
            }, ct);
        }

        private static async Task AuthenticateUserAsync(NetworkClient client, TcpClient tcpClient, CancellationToken ct)
        {
            Result result = await client.ReceiveHandshakeAsync(ct);
            if (!result.Success)
            {
                networkLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint).Warning(result.Error, "Error during ECDH handshake");
                client.Dispose();
                return;
            }

            ILogger configuredAuthLogger = authLogger.ForContext("RemoteEndPoint", tcpClient.Client.RemoteEndPoint);

            Result<object> packetResult = await client.ReceivePacketAsync(ct);
            if (!packetResult.Success)
            {
                configuredAuthLogger.Warning(packetResult.Error, "Error whilst waiting for user authentication");
                client.Dispose();
                return;
            }
            if (packetResult.Value is not AuthenticationPacket packet)
            {
                configuredAuthLogger.Warning(packetResult.Error, "Unexpected Packet during authentication");
                client.Dispose();
                return;
            }

            string username = packet.Username;
            configuredAuthLogger = configuredAuthLogger.ForContext("Username", username);
            Base32Token token;
            switch (packet.Type)
            {
                case AuthenticationPacket.AuthenticationType.Moderator:
                    try
                    {
                        token = Base32Token.FromCode(packet.Secret ?? "");

                        if (!Context.ModeratorKeys.TryUseKey(token.Hash))
                        {
                            configuredAuthLogger.Warning(packetResult.Error, "Invalid moderator key");
                            await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Invalid moderator key", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                            client.Dispose();
                            return;
                        }
                    }
                    catch (Exception e)
                    {
                        configuredAuthLogger.Warning(e, "Invalid moderator key");
                        await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Invalid moderator key", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                        client.Dispose();
                        return;
                    }
                    goto case AuthenticationPacket.AuthenticationType.Player;
                case AuthenticationPacket.AuthenticationType.Player:
                    if (!Player.IsUsernameValid(username))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Invalid username");
                        await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Invalid username", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                        client.Dispose();
                        return;
                    }

                    PlayerRole role = packet.Type == AuthenticationPacket.AuthenticationType.Moderator ? PlayerRole.Moderator : PlayerRole.Player;
                    Player? player = new(client, username, role);
                    if (!Context.Players.TryAdd(username, player))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Username already taken");
                        await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Username already taken", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                        client.Dispose();
                        return;
                    }

                    await client.SendPacketAsync(new AuthenticationPacket() { Username = username, Type = packet.Type }, ct);
                    configuredAuthLogger.ForContext("Role", role).Information("Player authenticated");

                    await Context.Lobby.TryAddPlayerAsync(player, ct);
                    return;
                case AuthenticationPacket.AuthenticationType.Recovery:
                    if (!Context.Players.TryGetValue(username, out player))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Username is unknown");
                        await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Username is unknown", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                        client.Dispose();
                        return;
                    }

                    token = Base32Token.FromCode(packet.Secret ?? "");
                    if (!player.KeyManager.TryUseKey(token.Hash))
                    {
                        configuredAuthLogger.Warning(packetResult.Error, "Invalid recovery key");
                        await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Invalid recovery key", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                        client.Dispose();
                        return;
                    }

                    player.SetClient(client);

                    await client.SendPacketAsync(new AuthenticationPacket()
                    {
                        Username = username,
                        Type = player.Role switch
                        {
                            PlayerRole.Moderator => AuthenticationPacket.AuthenticationType.Moderator,
                            _ => AuthenticationPacket.AuthenticationType.Player,
                        }
                    }, ct);
                    await player.Service.RecoverPlayerAsync(player, ct);
                    configuredAuthLogger.ForContext("Role", player.Role).Information("Player recovered");
                    return;
                default:
                    configuredAuthLogger.Warning(packetResult.Error, "Invalid connect mode");
                    await client.SendPacketAsync(new AuthenticationPacket() { Secret = "Invalid connect mode", Type = AuthenticationPacket.AuthenticationType.NONE }, ct);
                    client.Dispose();
                    return;
            }
        }

        public static async Task<bool> TryHandleServerPackageAsync(object package, Player sender, CancellationToken ct)
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
                    if (!Context.Players.TryGetValue(moderationPacket.Target, out Player? player))
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
                                await player.DisconnectAsync("Kicked", ct);
                            configuredSystemLogger.Information("Player kicked");
                            break;
                        case ModerationPacket.ModerationAction.Delete:
                            await player.Service.RemovePlayerAsync(player, ct);
                            Context.Players.Remove(player.Username, out _);
                            player.Dispose();
                            configuredSystemLogger.Information("Player deleted");
                            break;
                    }
                    return true;
                case ModerationSecretPacket moderationSecretPacket:
                    configuredSystemLogger = configuredSystemLogger.ForContext("Type", moderationSecretPacket.Type).ForContext("Target", moderationSecretPacket.Target);
                    if (sender.Role != PlayerRole.Moderator)
                    {
                        configuredSystemLogger.Warning("Access to moderation secrets denied");
                        return true;
                    }

                    Base32Token base32Token = Base32Token.FromRandom();
                    KeyToken keyToken = new(base32Token.Hash, TimeSpan.FromMinutes(10));
                    moderationSecretPacket.Secret = base32Token.Code;

                    switch (moderationSecretPacket.Type)
                    {
                        case ModerationSecretPacket.SecretType.Recovery:
                            if (moderationSecretPacket.Target is null || !Context.Players.TryGetValue(moderationSecretPacket.Target, out player))
                            {
                                configuredSystemLogger.Warning("Couldn't find player to recover");
                                return true;
                            }
                            player.KeyManager.RegisterKey(keyToken);
                            break;
                        case ModerationSecretPacket.SecretType.Moderator:
                            Context.ModeratorKeys.RegisterKey(keyToken);
                            break;
                    }
                    await sender.SendPacketAsync(moderationSecretPacket, ct);
                    configuredSystemLogger.Information("Secret generated");
                    return true;
                default:
                    return false;
            }
        }
    }
}

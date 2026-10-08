using Network.Packets;
using Network.Packets.Games.Lobby;
using Network.Packets.Games._57;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;

namespace Network
{
    public class NetworkClient : IDisposable
    {
        private const UInt32 MAX_PACKET_SIZE = 64 * 1024;

        public bool IsConnected => tcpClient.Connected;
        public bool IsEncrypted => aes is not null;
        public bool IsAvailable => tcpClient.Available > 0;

        private TcpClient tcpClient;
        private NetworkStream? stream;
        private byte[]? key;
        private AesGcm? aes;

        private readonly SemaphoreSlim sendLock = new(1, 1);
        private readonly SemaphoreSlim receiveLock = new(1, 1);

        public NetworkClient()
        {
            tcpClient = new TcpClient();
        }

        public NetworkClient(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            stream = tcpClient.GetStream();
        }

        public void Dispose()
        {
            tcpClient.Dispose();
            stream = null;
            aes?.Dispose();

            GC.SuppressFinalize(this);
        }

        public async Task<Result> TryConnectAsync(string ip, int port, CancellationToken ct = default)
        {
            if (IsConnected)
            {
                tcpClient.Dispose();
                stream = null;
                tcpClient = new TcpClient();
            }

            try
            {
                await tcpClient.ConnectAsync(ip, port, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (SocketException e)
            {
                return Result.Fail(e);
            }

            stream = tcpClient.GetStream();
            return Result.Ok();
        }

        public async Task<Result> DoHandshakeAsync(CancellationToken ct = default)
        {
            aes?.Dispose();

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey, ct);

            Result<byte[]> result = await ReceivePlainBytesAsync(ct);
            if (!result.Success)
                return result;

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }
        public async Task<Result> ReceiveHandshakeAsync(CancellationToken ct = default)
        {
            aes?.Dispose();

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            Result<byte[]> result = await ReceivePlainBytesAsync(ct);
            if (!result.Success)
                return result;

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey, ct);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }



        public async Task<Result> SendPacketAsync<T>(T packet, CancellationToken ct = default)
        {
            PacketEnvelope envelope = new()
            {
                Type = typeof(T).Name,
                Data = JsonSerializer.SerializeToElement(packet)
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(envelope);
            return await SendEncryptedBytesAsync(data, ct);
        }
        private async Task<Result> SendEncryptedBytesAsync(byte[] plaintext, CancellationToken ct = default)
        {
            Debug.Assert(aes is not null, $"{nameof(aes)} should be set before sending encrypted bytes.");

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];

            aes.Encrypt(nonce, plaintext, ciphertext, tag);

            byte[] packet = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, packet, 0, 12);
            Buffer.BlockCopy(tag, 0, packet, 12, 16);
            Buffer.BlockCopy(ciphertext, 0, packet, 28, ciphertext.Length);

            return await SendPlainBytesAsync(packet, ct);
        }
        private async Task<Result> SendPlainBytesAsync(byte[] data, CancellationToken ct = default)
        {
            Debug.Assert(stream is not null, $"{nameof(stream)} should be set before sending bytes.");

            byte[] length = BitConverter.GetBytes((UInt32)data.Length);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(length);

            await sendLock.WaitAsync(ct);

            try
            {
                await stream.WriteAsync(length, ct);
                await stream.WriteAsync(data, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                return Result.Fail(e);
            }
            finally
            {
                sendLock.Release();
            }

            return Result.Ok();
        }



        public async Task<Result<object>> ReceivePacketAsync(CancellationToken ct = default)
        {
            Result<byte[]> bytesResult = await ReceiveEncryptedBytesAsync(ct);
            if (!bytesResult.Success)
                return bytesResult;

            try
            {
                PacketEnvelope envelope = JsonSerializer.Deserialize<PacketEnvelope>(bytesResult.Value!)!;

                Debug.Assert(packetRegistry.ContainsKey(envelope.Type), $"{nameof(packetRegistry)} does not contain \"{envelope.Type}\".");
                Type type = packetRegistry[envelope.Type];
                object packet = envelope.Data.Deserialize(type)!;

                return Result<object>.Ok(packet);
            }
            catch (Exception e)
            {
                return Result<object>.Fail(e);
            }
        }
        private async Task<Result<byte[]>> ReceiveEncryptedBytesAsync(CancellationToken ct = default)
        {
            Debug.Assert(aes is not null, $"{nameof(aes)} should be set before receiving encrypted bytes.");

            Result<byte[]> plainBytesResult = await ReceivePlainBytesAsync(ct);
            if (!plainBytesResult.Success)
                return plainBytesResult;

            byte[] plainBytes = plainBytesResult.Value!;

            byte[] nonce = plainBytes[..12];
            byte[] tag = plainBytes[12..28];
            byte[] ciphertext = plainBytes[28..];

            byte[] data = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, data);

            return Result<byte[]>.Ok(data);
        }

        private async Task<Result<byte[]>> ReceivePlainBytesAsync(CancellationToken ct = default)
        {
            Debug.Assert(stream is not null, $"{nameof(stream)} should be set before receiving bytes.");

            await receiveLock.WaitAsync(ct);
            byte[] lengthBytes = new byte[4];

            try
            {
                await stream.ReadExactlyAsync(lengthBytes, ct);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(lengthBytes);

                UInt32 length = BitConverter.ToUInt32(lengthBytes);
                if (length <= 0 || length > MAX_PACKET_SIZE)
                    throw new InvalidDataException("Invalid packet size.");

                byte[] data = new byte[length];

                await stream.ReadExactlyAsync(data, ct);

                return Result<byte[]>.Ok(data);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception e)
            {
                return Result<byte[]>.Fail(e);
            }
            finally
            {
                receiveLock.Release();
            }
        }

        private static readonly Dictionary<string, Type> packetRegistry = new()
        {
            ["HeartbeatPacket"] = typeof(HeartbeatPacket),
            ["AuthenticationPacket"] = typeof(AuthenticationPacket),
            ["SetViewPacket"] = typeof(SetViewPacket),
            ["ModerationPacket"] = typeof(ModerationPacket),
            ["ModerationSecretPacket"] = typeof(ModerationSecretPacket),
            ["PingPacket"] = typeof(PingPacket),

            ["Lobby_PlayerListPacket"] = typeof(Lobby_PlayerListPacket),
            ["Lobby_GameCreatePacket"] = typeof(Lobby_GameCreatePacket),
            ["Lobby_GameListPacket"] = typeof(Lobby_GameListPacket),
            ["Lobby_GameJoinPacket"] = typeof(Lobby_GameJoinPacket),

            ["_57_LobbyPacket"] = typeof(_57_LobbyPacket),
            ["_57_LobbySettingsUpdatePacket"] = typeof(_57_LobbySettingsUpdatePacket),
        };
    }
}

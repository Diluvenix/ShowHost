using Network.Packets;
using Network.Packets.Games.Lobby;
using Network.Packets.Games._57;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;

namespace Network
{
    public class NetworkClient : IDisposable
    {
        private const int MAX_PACKET_SIZE = 64 * 1024;

        public bool IsConnected => tcpClient.Connected;
        public bool IsAvailable => tcpClient.Available > 0;

        private TcpClient tcpClient;
        private NetworkStream? stream;
        private byte[]? key;
        private AesGcm? aes;

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
            if (IsConnected)
            {
                tcpClient.Close();
            }
            tcpClient.Dispose();
            aes?.Dispose();

            GC.SuppressFinalize(this);
        }

        public async Task<Result> TryConnectAsync(string ip, int port, CancellationToken ct = default)
        {
            if (IsConnected)
            {
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = new TcpClient();
            }

            try
            {
                await tcpClient.ConnectAsync(ip, port, ct);
            }
            catch (SocketException e)
            {
                return Result.Fail(e);
            }

            stream = tcpClient.GetStream();
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

        public async Task<Result<object>> ReceivePacketAsync(CancellationToken ct = default)
        {
            Result<byte[]> result = await ReceiveEncryptedBytesAsync(ct);
            if (!result.Success)
                return Result<object>.Fail(result.Error!);

            try
            {
                PacketEnvelope envelope = JsonSerializer.Deserialize<PacketEnvelope>(result.Value!)!;

                Type type = packetRegistry[envelope.Type];
                object packet = envelope.Data.Deserialize(type)!;

                return Result<object>.Ok(packet);
            }
            catch (Exception e)
            {
                return Result<object>.Fail(e);
            }
        }

        public async Task<Result> DoHandshakeAsync(CancellationToken ct = default)
        {
            aes?.Dispose();
            aes = null;

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey, ct);

            Result<byte[]> result = await ReceivePlainBytesAsync(ct);
            if (!result.Success)
                return Result.Fail(result.Error!);

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }
        public async Task<Result> ReceiveHandshakeAsync(CancellationToken ct = default)
        {
            aes?.Dispose();
            aes = null;

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            Result<byte[]> result = await ReceivePlainBytesAsync(ct);
            if (!result.Success)
                return Result.Fail(result.Error!);

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey, ct);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }

        private async Task<Result> SendPlainBytesAsync(byte[] data, CancellationToken ct = default)
        {
            byte[] length = BitConverter.GetBytes(data.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(length);

            try
            {
                await stream!.WriteAsync(length, ct);
                await stream!.WriteAsync(data, ct);
            }
            catch (Exception e)
            {
                return Result.Fail(e);
            }

            return Result.Ok();
        }

        private async Task<Result<byte[]>> ReceivePlainBytesAsync(CancellationToken ct = default)
        {
            try
            {
                byte[] length = new byte[4];
                await stream!.ReadExactlyAsync(length, ct);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(length);

                int size = BitConverter.ToInt32(length);
                if (size <= 0 || size > MAX_PACKET_SIZE)
                    throw new InvalidDataException("Invalid packet size.");

                byte[] data = new byte[size];

                await stream!.ReadExactlyAsync(data, ct);

                return Result<byte[]>.Ok(data);
            }
            catch (Exception e)
            {
                return Result<byte[]>.Fail(e);
            }
        }


        private async Task<Result> SendEncryptedBytesAsync(byte[] plaintext, CancellationToken ct = default)
        {
            lock (this)
            {
                if (!IsConnected || aes == null) 
                    return Result.Fail(new Exception("Connection not encrypted."));

                byte[] nonce = RandomNumberGenerator.GetBytes(12);
                byte[] ciphertext = new byte[plaintext.Length];
                byte[] tag = new byte[16];

                aes.Encrypt(nonce, plaintext, ciphertext, tag);

                byte[] packet = new byte[nonce.Length + tag.Length + ciphertext.Length];
                Buffer.BlockCopy(nonce, 0, packet, 0, 12);
                Buffer.BlockCopy(tag, 0, packet, 12, 16);
                Buffer.BlockCopy(ciphertext, 0, packet, 28, ciphertext.Length);

                return SendPlainBytesAsync(packet, ct).Result;
            }
        }
        private async Task<Result<byte[]>> ReceiveEncryptedBytesAsync(CancellationToken ct = default)
        {
            while (tcpClient.Available < 4)
            {
                await Task.Delay(10, ct);
            }

            lock (this)
            {
                if (!IsConnected || aes == null)
                    return Result<byte[]>.Fail(new Exception("Connection not encrypted."));

                Result<byte[]> result = ReceivePlainBytesAsync(ct).Result;
                if (!result.Success)
                    return Result<byte[]>.Fail(result.Error!);

                byte[] packet = result.Value!;

                byte[] nonce = packet[..12];
                byte[] tag = packet[12..28];
                byte[] ciphertext = packet[28..];

                byte[] plaintext = new byte[ciphertext.Length];

                aes.Decrypt(nonce, ciphertext, tag, plaintext);

                return Result<byte[]>.Ok(plaintext);
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

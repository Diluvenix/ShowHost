using Network.Packets;
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
        }

        public async Task<Result> TryConnect(string ip, int port)
        {
            if (IsConnected)
            {
                tcpClient.Close();
                tcpClient.Dispose();
                tcpClient = new TcpClient();
            }

            try
            {
                await tcpClient.ConnectAsync(ip, port);
            }
            catch (SocketException e)
            {
                return Result.Fail(e);
            }

            stream = tcpClient.GetStream();
            return Result.Ok();
        }

        public async Task<Result> SendPacketAsync<T>(T packet)
        {
            PacketEnvelope envelope = new()
            {
                Type = typeof(T).Name,
                Data = JsonSerializer.SerializeToElement(packet)
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(envelope);
            return await SendEncryptedBytesAsync(data);
        }

        public async Task<Result<object>> ReceivePacketAsync()
        {
            Result<byte[]> result = await ReceiveEncryptedBytesAsync();
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

        public async Task<Result> DoHandshake()
        {
            aes?.Dispose();
            aes = null;

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey);

            Result<byte[]> result = await ReceivePlainBytesAsync();
            if (!result.Success)
                return Result.Fail(result.Error!);

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }
        public async Task<Result> ReceiveHandshake()
        {
            aes?.Dispose();
            aes = null;

            using ECDiffieHellman self = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            using ECDiffieHellman other = ECDiffieHellman.Create();

            Result<byte[]> result = await ReceivePlainBytesAsync();
            if (!result.Success)
                return Result.Fail(result.Error!);

            byte[] otherKey = result.Value!;
            other.ImportSubjectPublicKeyInfo(otherKey, out _);

            byte[] myKey = self.ExportSubjectPublicKeyInfo();
            await SendPlainBytesAsync(myKey);

            key = self.DeriveKeyMaterial(other.PublicKey);
            aes = new AesGcm(key, 16);

            return Result.Ok();
        }

        private async Task<Result> SendPlainBytesAsync(byte[] data)
        {
            byte[] length = BitConverter.GetBytes(data.Length);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(length);

            try
            {
                await stream!.WriteAsync(length);
                await stream!.WriteAsync(data);
            }
            catch (Exception e)
            {
                return Result.Fail(e);
            }

            return Result.Ok();
        }

        private async Task<Result<byte[]>> ReceivePlainBytesAsync()
        {
            try
            {
                byte[] length = new byte[4];
                await stream!.ReadExactlyAsync(length);

                if (BitConverter.IsLittleEndian)
                    Array.Reverse(length);

                int size = BitConverter.ToInt32(length);
                if (size <= 0 || size > MAX_PACKET_SIZE)
                    throw new InvalidDataException("Invalid packet size.");

                byte[] data = new byte[size];

                await stream!.ReadExactlyAsync(data);

                return Result<byte[]>.Ok(data);
            }
            catch (Exception e)
            {
                return Result<byte[]>.Fail(e);
            }
        }


        private async Task<Result> SendEncryptedBytesAsync(byte[] plaintext)
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

                return SendPlainBytesAsync(packet).Result;
            }
        }
        private async Task<Result<byte[]>> ReceiveEncryptedBytesAsync()
        {
            while (tcpClient.Available < 4)
            {
                await Task.Delay(10);
            }

            lock (this)
            {
                if (!IsConnected || aes == null)
                    return Result<byte[]>.Fail(new Exception("Connection not encrypted."));

                Result<byte[]> result = ReceivePlainBytesAsync().Result;
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
            ["ConnectPacket"] = typeof(ConnectPacket),
            ["LobbyPacket"] = typeof(LobbyPacket),
            ["HeartbeatPacket"] = typeof(HeartbeatPacket),
            ["KickPacket"] = typeof(KickPacket),
            ["DeletePacket"] = typeof(DeletePacket),
            ["GenerateRecoveryKeyPacket"] = typeof(GenerateRecoveryKeyPacket),
            ["GenerateModeratorKeyPacket"] = typeof(GenerateModeratorKeyPacket),
            ["CreateGamePacket"] = typeof(CreateGamePacket),
            ["SetViewPacket"] = typeof(SetViewPacket),
            ["GameListPacket"] = typeof(GameListPacket),
            ["JoinGamePacket"] = typeof(JoinGamePacket),
        };
    }
}

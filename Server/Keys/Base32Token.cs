using System.Security.Cryptography;

namespace Server.Keys
{
    internal struct Base32Token
    {
        public const string ALPHABET = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        private byte[] bytes = [];
        public byte[] Bytes
        {
            readonly get => bytes;
            set {
                bytes = value;
                code = Encode(value);
                hash = Convert.ToHexString(SHA256.HashData(value));
            }
        }

        private string code = "";
        public string Code
        {
            readonly get => code;
            set
            {
                code = value;
                bytes = Decode(value);
                hash = Convert.ToHexString(SHA256.HashData(bytes));
            }
        }

        private string hash = "";
        public readonly string Hash
            => hash;

        public Base32Token()
        {

        }

        public static Base32Token FromBytes(byte[] bytes) 
            => new() { Bytes = bytes };
        public static Base32Token FromRandom()
            => new() { Bytes = RandomNumberGenerator.GetBytes(8) };
        public static Base32Token FromCode(string code)
            => new() { Code = code };

        public static string Encode(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 8)
                throw new ArgumentException("Exactly 8 bytes are required.", nameof(bytes));

            Span<char> chars = stackalloc char[15];

            int bitBuffer = 0;
            int bitCount = 0;
            int outputIndex = 0;

            foreach (byte b in bytes)
            {
                bitBuffer = (bitBuffer << 8) | b;
                bitCount += 8;

                while (bitCount >= 5)
                {
                    bitCount -= 5;

                    int value = (bitBuffer >> bitCount) & 0b11111;
                    chars[outputIndex++] = ALPHABET[value];

                    if (outputIndex is 4 or 10)
                        chars[outputIndex++] = '-';
                }
            }
            if (bitCount > 0)
            {
                int value = (bitBuffer << (5 - bitCount)) & 0b11111;
                chars[outputIndex++] = ALPHABET[value];
            }

            return new string(chars);
        }

        public static byte[] Decode(string token)
        {
            Span<char> normalized = stackalloc char[13];

            int length = 0;
            foreach (char c in token)
            {
                if (c is '-' or ' ' or '\t')
                    continue;

                if (length >= 13)
                    throw new ArgumentException("Token must contain excatly 13 Base32 character.", nameof(token));

                char ch = char.ToUpperInvariant(c);
                ch = ch switch
                {
                    'O' => '0',
                    'I' => '1',
                    'L' => '1',
                    _ => ch
                };

                normalized[length++] = ch;
            }

            if (length != 13)
                throw new ArgumentException("Token must contain excatly 13 Base32 character.", nameof(token));

            byte[] result = new byte[8];

            int bitBuffer = 0;
            int bitCount = 0;
            int outputIndex = 0;

            foreach (char c in normalized)
            {
                int value = ALPHABET.IndexOf(c);
                if (value < 0)
                    throw new FormatException($"Invalid Base32 character: '{c}'.");

                bitBuffer = (bitBuffer << 5) | value;
                bitCount += 5;

                if (bitCount >= 8)
                {
                    bitCount -= 8;

                    result[outputIndex++] = (byte)((bitBuffer >> bitCount) & 0xFF);
                }
            }

            return result;
        }
    }
}

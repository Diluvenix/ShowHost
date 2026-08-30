using Server.Keys.Words;
using System.Security.Cryptography;

namespace Server.Keys
{
    internal static class NameGenerator
    {
        public static string Generate()
        {
            int adjectiveId = RandomNumberGenerator.GetInt32(Adjectives.DATA.Length + Colours.DATA.Length);

            return $"{(adjectiveId < Adjectives.DATA.Length ? Adjectives.DATA[adjectiveId] : Colours.DATA[adjectiveId - Adjectives.DATA.Length])} {Animals.DATA[RandomNumberGenerator.GetInt32(Animals.DATA.Length)]} {RandomNumberGenerator.GetInt32(10, 100)}";
        }
    }
}

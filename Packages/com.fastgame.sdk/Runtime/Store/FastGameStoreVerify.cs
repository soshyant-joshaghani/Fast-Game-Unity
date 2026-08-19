using System;
using System.Security.Cryptography;
using System.Text;

namespace FastGame
{
    /// <summary>
    /// FG1 unwrap for Myket / Cafe Bazaar RSA public keys fetched from Fast Game.
    /// The wrap is obfuscation (bound to game_code + provider), not encryption.
    /// JWT / api_secret are never part of this payload.
    /// </summary>
    public static class FastGameStoreVerify
    {
        public const string WrapVersion = "FG1";
        const string Domain = "fastgame.store-verify.v1";

        public static bool NeedsRemoteRsa(string provider)
        {
            var id = FastGameConfig.NormalizeProviderId(provider);
            return id == "myket" || id == "caffebazar";
        }

        public static string Unwrap(string wrapped, string gameCode, string provider)
        {
            var blob = (wrapped ?? "").Trim();
            var prefix = WrapVersion + ".";
            if (!blob.StartsWith(prefix, StringComparison.Ordinal))
                throw new FastGameException("FastGame: unsupported store-verify wrap");

            var token = blob.Substring(prefix.Length);
            token = token.Replace('-', '+').Replace('_', '/');
            switch (token.Length % 4)
            {
                case 2: token += "=="; break;
                case 3: token += "="; break;
            }
            var xored = Convert.FromBase64String(token);
            var mask = Mask(gameCode, provider);
            var pemBytes = new byte[xored.Length];
            for (var i = 0; i < xored.Length; i++)
                pemBytes[i] = (byte)(xored[i] ^ mask[i % mask.Length]);
            return Encoding.UTF8.GetString(pemBytes).Trim();
        }

        static byte[] Mask(string gameCode, string provider)
        {
            var material = Domain
                + "|"
                + (gameCode ?? "").Trim()
                + "|"
                + FastGameConfig.NormalizeProviderId(provider);
            using (var sha = SHA256.Create())
                return sha.ComputeHash(Encoding.UTF8.GetBytes(material));
        }
    }
}

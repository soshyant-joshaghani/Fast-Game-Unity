using System.Text.RegularExpressions;

namespace FastGame
{
    public enum FastGameIdentityKind
    {
        Unknown = 0,
        Email = 1,
        Phone = 2,
    }

    public enum FastGameIdentityChannel
    {
        Auto = 0,
        Email = 1,
        Phone = 2,
    }

    /// <summary>
    /// Classifies login/signup/recovery contact values as email or phone.
    /// Login uses one backend endpoint (<c>POST /base/login/access-token</c>) with OAuth
    /// <c>username</c> = email or phone; the server resolves via <c>find_user_by_identity</c>.
    /// Signup/recovery split into JSON <c>email</c> / <c>phone</c> fields.
    /// </summary>
    public static class FastGameIdentity
    {
        static readonly Regex EmailRegex = new Regex(
            @"^[^\s@]+@[^\s@]+\.[^\s@]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        static readonly Regex NonDigitsRegex = new Regex(@"\D+", RegexOptions.Compiled);

        public static FastGameIdentityKind Classify(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return FastGameIdentityKind.Unknown;

            var trimmed = identity.Trim();
            if (trimmed.IndexOf('@') >= 0)
                return EmailRegex.IsMatch(trimmed) ? FastGameIdentityKind.Email : FastGameIdentityKind.Unknown;

            return TryNormalizePhone(trimmed, out _) ? FastGameIdentityKind.Phone : FastGameIdentityKind.Unknown;
        }

        public static bool LooksLikeEmail(string identity) =>
            Classify(identity) == FastGameIdentityKind.Email;

        public static bool LooksLikePhone(string identity) =>
            Classify(identity) == FastGameIdentityKind.Phone;

        /// <summary>
        /// Normalize Iranian mobiles to <c>9xxxxxxxxx</c> (matches backend <c>normalize_iran_mobile</c>).
        /// </summary>
        public static bool TryNormalizePhone(string identity, out string normalized)
        {
            normalized = null;
            if (string.IsNullOrWhiteSpace(identity))
                return false;

            var digits = NonDigitsRegex.Replace(identity.Trim(), "");
            if (digits.StartsWith("98") && digits.Length >= 12)
                digits = digits.Substring(2);
            if (digits.StartsWith("0") && digits.Length == 11)
                digits = digits.Substring(1);
            if (!digits.StartsWith("9") || digits.Length != 10)
                return false;

            normalized = digits;
            return true;
        }

        /// <summary>
        /// Split a single identity string into exactly one of email or phone.
        /// Phone values are normalized when possible.
        /// </summary>
        public static bool TrySplitContact(string identity, out string email, out string phone)
        {
            email = null;
            phone = null;
            switch (Classify(identity))
            {
                case FastGameIdentityKind.Email:
                    email = identity.Trim().ToLowerInvariant();
                    return true;
                case FastGameIdentityKind.Phone:
                    return TryNormalizePhone(identity, out phone);
                default:
                    return false;
            }
        }

        public static void RequireMatchingPasswords(string password, string passwordConfirm)
        {
            if (string.IsNullOrEmpty(password))
                throw new FastGameException("Password is required");
            if (password.Length < 8)
                throw new FastGameException("Password must be at least 8 characters");
            if (password != passwordConfirm)
                throw new FastGameException("Passwords do not match");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FastGame.Models;
using UnityEngine;

namespace FastGame
{
    public sealed class SteamLinkStatus
    {
        public bool Linked;
        public string Steamid;
    }

    public sealed class FastGameSignupResult
    {
        public string UserId;
        public string Email;
        public string Phone;
    }

    /// <summary>
    /// ENTER contract result (POST /base/login/enter). No widgets — route in your UI.
    /// Follow-up identity is <see cref="Email"/> ?? <see cref="Phone"/>.
    /// </summary>
    public sealed class FastGameEnterResult
    {
        public bool Exists;
        public bool PasswordRequired;
        public string Channel;
        public string Email;
        public string Phone;
        public string Identity => !string.IsNullOrEmpty(Email) ? Email : Phone;
        public bool IsEmail => string.Equals(Channel, "email", System.StringComparison.OrdinalIgnoreCase);
        public bool IsPhone => string.Equals(Channel, "phone", System.StringComparison.OrdinalIgnoreCase);
    }

    public sealed class FastGameAuth
    {
        readonly FastGameHttp _http;
        readonly FastGameConfig _config;

        public FastGameAuth(FastGameHttp http, FastGameConfig config)
        {
            _http = http;
            _config = config;
            LoadPersistedAccessToken();
            LoadPersistedEnteredIdentity();
        }

        public bool IsLoggedIn => !string.IsNullOrEmpty(_http.AccessToken);

        /// <summary>Same as <see cref="IsLoggedIn"/> — use before branching to login vs home.</summary>
        public bool IsAuthenticated => IsLoggedIn;

        public string AccessToken => _http.AccessToken;

        /// <summary>ENTER-stored identity (empty if none).</summary>
        public string EnteredIdentity { get; private set; }

        /// <summary>ENTER-stored channel (Email or Phone when set).</summary>
        public FastGameIdentityChannel EnteredChannel { get; private set; } = FastGameIdentityChannel.Auto;

        public bool HasEnteredIdentity => !string.IsNullOrEmpty(EnteredIdentity);

        /// <summary>Fired when an access token is set (login / signup / restore). Used to freeze store wallet.</summary>
        public Action OnLoggedIn;

        /// <summary>Result of the most recent LoginAsync (also true after successful SignupAsync).</summary>
        public bool LastLoginSucceeded { get; private set; }

        /// <summary>Result of the most recent SignupAsync.</summary>
        public bool LastSignupSucceeded { get; private set; }

        /// <summary>Cached profile from the last successful GetMeAsync.</summary>
        public UserProfile CurrentUser { get; private set; }

        /// <summary>
        /// ENTER contract: probe identity for login / signup / recovery routing (no widgets).
        /// <paramref name="channel"/>: Auto (detect), Email, or Phone.
        /// On success, stores identity for <see cref="LoginAsync"/> with empty identity.
        /// New-user OTP is decided by the client from catalog <c>auth_requirements</c> + config GameCode.
        /// </summary>
        public async Task<FastGameEnterResult> EnterAsync(
            string identity,
            FastGameIdentityChannel channel = FastGameIdentityChannel.Auto)
        {
            string outEmail = null;
            string outPhone = null;
            if (channel == FastGameIdentityChannel.Email)
            {
                if (!FastGameIdentity.LooksLikeEmail(identity))
                    throw new FastGameException("Valid email is required");
                outEmail = identity.Trim().ToLowerInvariant();
            }
            else if (channel == FastGameIdentityChannel.Phone)
            {
                if (!FastGameIdentity.TryNormalizePhone(identity, out outPhone))
                    throw new FastGameException("Valid phone number is required");
            }
            else if (!FastGameIdentity.TrySplitContact(identity, out outEmail, out outPhone))
            {
                throw new FastGameException("Identity must be a valid email or phone number");
            }

            var body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(outEmail))
                body["email"] = outEmail;
            else
                body["phone"] = outPhone;

            var text = await _http.RequestRawAsync("POST", "/base/login/enter", FastGameJson.Stringify(body));
            var obj = FastGameJson.ParseObject(text);
            var result = new FastGameEnterResult
            {
                Exists = FastGameJson.GetBool(obj, "exists"),
                PasswordRequired = FastGameJson.GetBool(obj, "password_required"),
                Channel = FastGameJson.GetString(obj, "channel"),
                Email = FastGameJson.GetString(obj, "email"),
                Phone = FastGameJson.GetString(obj, "phone"),
            };
            if (!string.IsNullOrEmpty(result.Identity))
                StoreEnteredIdentity(result.Identity, result.IsEmail);
            return result;
        }

        /// <summary>
        /// Login with email or phone. <paramref name="channel"/>: Auto (detect), Email, or Phone.
        /// Empty <paramref name="identity"/> uses ENTER-stored identity.
        /// Hits <c>POST /base/login/access-token</c>.
        /// </summary>
        public Task LoginAsync(
            string identity,
            string password,
            FastGameIdentityChannel channel = FastGameIdentityChannel.Auto)
        {
            if (string.IsNullOrEmpty(password))
            {
                LastLoginSucceeded = false;
                return Task.FromException(new FastGameException("Email/phone and password are required"));
            }

            var effectiveIdentity = identity?.Trim() ?? "";
            var effectiveChannel = channel;
            if (string.IsNullOrEmpty(effectiveIdentity))
            {
                if (string.IsNullOrEmpty(EnteredIdentity))
                    LoadPersistedEnteredIdentity();
                if (string.IsNullOrEmpty(EnteredIdentity))
                {
                    LastLoginSucceeded = false;
                    return Task.FromException(
                        new FastGameException("No Identity provided and no ENTER-stored identity"));
                }
                effectiveIdentity = EnteredIdentity;
                if (effectiveChannel == FastGameIdentityChannel.Auto)
                    effectiveChannel = EnteredChannel;
            }

            if (effectiveChannel == FastGameIdentityChannel.Email)
                return LoginWithEmailAsync(effectiveIdentity, password);
            if (effectiveChannel == FastGameIdentityChannel.Phone)
                return LoginWithPhoneAsync(effectiveIdentity, password);

            switch (FastGameIdentity.Classify(effectiveIdentity))
            {
                case FastGameIdentityKind.Email:
                    return LoginWithEmailAsync(effectiveIdentity, password);
                case FastGameIdentityKind.Phone:
                    return LoginWithPhoneAsync(effectiveIdentity, password);
                default:
                    LastLoginSucceeded = false;
                    return Task.FromException(
                        new FastGameException("Identity must be a valid email or phone number"));
            }
        }

        /// <summary>Clear ENTER-stored identity (memory + PlayerPrefs).</summary>
        public void ClearEnteredIdentity()
        {
            EnteredIdentity = null;
            EnteredChannel = FastGameIdentityChannel.Auto;
            if (!string.IsNullOrEmpty(_config.EnteredIdentityPrefsKey))
                PlayerPrefs.DeleteKey(_config.EnteredIdentityPrefsKey);
            if (!string.IsNullOrEmpty(_config.EnteredChannelPrefsKey))
                PlayerPrefs.DeleteKey(_config.EnteredChannelPrefsKey);
            PlayerPrefs.Save();
        }

        void StoreEnteredIdentity(string identity, bool isEmail)
        {
            EnteredIdentity = identity?.Trim();
            EnteredChannel = isEmail ? FastGameIdentityChannel.Email : FastGameIdentityChannel.Phone;
            PersistEnteredIdentity();
        }

        void PersistEnteredIdentity()
        {
            if (string.IsNullOrEmpty(_config.EnteredIdentityPrefsKey))
                return;
            if (string.IsNullOrEmpty(EnteredIdentity))
            {
                PlayerPrefs.DeleteKey(_config.EnteredIdentityPrefsKey);
                if (!string.IsNullOrEmpty(_config.EnteredChannelPrefsKey))
                    PlayerPrefs.DeleteKey(_config.EnteredChannelPrefsKey);
            }
            else
            {
                PlayerPrefs.SetString(_config.EnteredIdentityPrefsKey, EnteredIdentity);
                if (!string.IsNullOrEmpty(_config.EnteredChannelPrefsKey))
                {
                    PlayerPrefs.SetString(
                        _config.EnteredChannelPrefsKey,
                        EnteredChannel == FastGameIdentityChannel.Phone ? "phone" : "email");
                }
            }
            PlayerPrefs.Save();
        }

        void LoadPersistedEnteredIdentity()
        {
            if (string.IsNullOrEmpty(_config.EnteredIdentityPrefsKey))
                return;
            var id = PlayerPrefs.GetString(_config.EnteredIdentityPrefsKey, "");
            if (string.IsNullOrEmpty(id))
                return;
            EnteredIdentity = id;
            var ch = string.IsNullOrEmpty(_config.EnteredChannelPrefsKey)
                ? ""
                : PlayerPrefs.GetString(_config.EnteredChannelPrefsKey, "email");
            EnteredChannel = string.Equals(ch, "phone", System.StringComparison.OrdinalIgnoreCase)
                ? FastGameIdentityChannel.Phone
                : FastGameIdentityChannel.Email;
        }

        Task LoginWithEmailAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || !FastGameIdentity.LooksLikeEmail(email))
            {
                LastLoginSucceeded = false;
                return Task.FromException(new FastGameException("Valid email is required"));
            }
            return LoginWithUsernameAsync(email.Trim().ToLowerInvariant(), password);
        }

        Task LoginWithPhoneAsync(string phone, string password)
        {
            if (!FastGameIdentity.TryNormalizePhone(phone, out var normalized))
            {
                LastLoginSucceeded = false;
                return Task.FromException(new FastGameException("Valid phone number is required"));
            }
            return LoginWithUsernameAsync(normalized, password);
        }

        async Task LoginWithUsernameAsync(string username, string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                LastLoginSucceeded = false;
                throw new FastGameException("Password is required");
            }
            try
            {
                // Backend: POST /base/login/access-token — find_user_by_identity(username)
                var text = await _http.PostFormAsync("/base/login/access-token", new Dictionary<string, string>
                {
                    { "username", username },
                    { "password", password },
                });
                var obj = FastGameJson.ParseObject(text);
                var token = FastGameJson.GetString(obj, "access_token");
                if (string.IsNullOrEmpty(token))
                    throw new FastGameException("Login response missing access_token");
                SetAccessToken(token);
                LastLoginSucceeded = true;
            }
            catch
            {
                LastLoginSucceeded = false;
                throw;
            }
        }

        /// <summary>
        /// Register a new account (email and/or phone — at least one, or both empty → ENTER store),
        /// verify password confirmation locally, then log in. Only one password is sent.
        /// Uses <see cref="FastGameConfig.GameCode"/> when catalog verify is on (after signup OTP).
        /// </summary>
        public async Task<FastGameSignupResult> SignupAsync(
            string email,
            string password,
            string passwordConfirm,
            string fullName = null,
            string phone = null)
        {
            var e = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            var p = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (e == null && p == null)
            {
                if (!TryFillFromEntered(out e, out p))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("No contact provided and no ENTER-stored identity");
                }
            }
            if (e != null)
            {
                if (!FastGameIdentity.LooksLikeEmail(e))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("Invalid email format");
                }
                e = e.ToLowerInvariant();
            }
            if (p != null)
            {
                if (!FastGameIdentity.TryNormalizePhone(p, out var normalizedPhone))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("Invalid phone format");
                }
                p = normalizedPhone;
            }
            try
            {
                FastGameIdentity.RequireMatchingPasswords(password, passwordConfirm);
            }
            catch
            {
                LastSignupSucceeded = false;
                LastLoginSucceeded = false;
                throw;
            }

            try
            {
                var body = new Dictionary<string, object>
                {
                    { "password", password },
                };
                if (e != null)
                    body["email"] = e;
                if (p != null)
                    body["phone"] = p;
                if (!string.IsNullOrEmpty(fullName))
                    body["full_name"] = fullName;
                var gameCode = (_config.GameCode ?? "").Trim();
                if (!string.IsNullOrEmpty(gameCode))
                    body["game_code"] = gameCode;

                var text = await _http.RequestRawAsync("POST", "/base/users/signup", FastGameJson.Stringify(body));
                var obj = FastGameJson.ParseObject(text);
                var result = new FastGameSignupResult
                {
                    UserId = FastGameJson.GetString(obj, "id"),
                    Email = FastGameJson.GetString(obj, "email") ?? e,
                    Phone = FastGameJson.GetString(obj, "phone") ?? p,
                };
                var loginId = e ?? p;
                await LoginAsync(loginId, password);
                LastSignupSucceeded = true;
                return result;
            }
            catch
            {
                LastSignupSucceeded = false;
                LastLoginSucceeded = false;
                throw;
            }
        }

        /// <summary>
        /// Complete Account: set password (+ optional full name) on a passwordless existing user.
        /// Empty email+phone → ENTER store. Auto-login. No OTP.
        /// </summary>
        public async Task<FastGameSignupResult> CompleteAccountAsync(
            string email,
            string password,
            string passwordConfirm,
            string fullName = null,
            string phone = null)
        {
            var e = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            var p = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (e == null && p == null)
            {
                if (!TryFillFromEntered(out e, out p))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("No contact provided and no ENTER-stored identity");
                }
            }
            if (e != null)
            {
                if (!FastGameIdentity.LooksLikeEmail(e))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("Invalid email format");
                }
                e = e.ToLowerInvariant();
            }
            if (p != null)
            {
                if (!FastGameIdentity.TryNormalizePhone(p, out var normalizedPhone))
                {
                    LastSignupSucceeded = false;
                    LastLoginSucceeded = false;
                    throw new FastGameException("Invalid phone format");
                }
                p = normalizedPhone;
            }
            FastGameIdentity.RequireMatchingPasswords(password, passwordConfirm);

            try
            {
                var body = new Dictionary<string, object> { { "password", password } };
                if (e != null)
                    body["email"] = e;
                if (p != null)
                    body["phone"] = p;
                if (!string.IsNullOrEmpty(fullName))
                    body["full_name"] = fullName;

                var text = await _http.RequestRawAsync("POST", "/base/login/complete", FastGameJson.Stringify(body));
                var obj = FastGameJson.ParseObject(text);
                var result = new FastGameSignupResult
                {
                    UserId = FastGameJson.GetString(obj, "id"),
                    Email = FastGameJson.GetString(obj, "email") ?? e,
                    Phone = FastGameJson.GetString(obj, "phone") ?? p,
                };
                await LoginAsync(e ?? p, password);
                LastSignupSucceeded = true;
                return result;
            }
            catch
            {
                LastSignupSucceeded = false;
                LastLoginSucceeded = false;
                throw;
            }
        }

        /// <summary>PATCH /base/login/me — display name only. Requires login.</summary>
        public async Task<UserProfile> UpdateFullNameAsync(string fullName)
        {
            if (!IsLoggedIn)
                throw new FastGameException("Not logged in");
            var body = new Dictionary<string, object> { { "full_name", fullName ?? "" } };
            var text = await _http.RequestRawAsync("PATCH", "/base/login/me", FastGameJson.Stringify(body));
            CurrentUser = ParseUserProfile(text);
            return CurrentUser;
        }

        /// <summary>Signup OTP step 1/2: send code for a new identity. Empty identity → ENTER store.</summary>
        public Task RequestSignupVerificationAsync(string identity) =>
            RequestSignupVerificationAsync(email: null, phone: null, identity: identity);

        public async Task RequestSignupVerificationAsync(
            string email = null,
            string phone = null,
            string identity = null)
        {
            var gameCode = RequireGameCode();
            ResolveContact(identity, ref email, ref phone);
            var body = BuildContactBody(gameCode, email, phone);
            await _http.RequestRawAsync("POST", "/base/signup/request", FastGameJson.Stringify(body));
        }

        /// <summary>Signup OTP step 2/2: verify code then show Signup canvas. Empty identity → ENTER store.</summary>
        public Task VerifySignupVerificationAsync(string identity, string code) =>
            VerifySignupVerificationAsync(code, email: null, phone: null, identity: identity);

        public async Task VerifySignupVerificationAsync(
            string code,
            string email = null,
            string phone = null,
            string identity = null)
        {
            var gameCode = RequireGameCode();
            if (string.IsNullOrWhiteSpace(code))
                throw new FastGameException("Verification code is required");
            ResolveContact(identity, ref email, ref phone);
            var body = BuildContactBody(gameCode, email, phone);
            body["code"] = code.Trim();
            await _http.RequestRawAsync("POST", "/base/signup/verify", FastGameJson.Stringify(body));
        }

        /// <summary>
        /// Forgot password step 1/3: send recovery OTP. Empty identity → ENTER-stored identity.
        /// </summary>
        public Task RequestPasswordRecoveryAsync(string identity) =>
            RequestPasswordRecoveryAsync(email: null, phone: null, identity: identity);

        /// <summary>Forgot password step 1/3 with explicit email and/or phone.</summary>
        public async Task RequestPasswordRecoveryAsync(
            string email = null,
            string phone = null,
            string identity = null)
        {
            var gameCode = RequireGameCode();
            ResolveContact(identity, ref email, ref phone);
            var body = BuildContactBody(gameCode, email, phone);
            await _http.RequestRawAsync("POST", "/base/recovery/request", FastGameJson.Stringify(body));
        }

        /// <summary>
        /// Forgot password step 2/3: verify recovery OTP. Empty identity → ENTER store.
        /// </summary>
        public Task VerifyPasswordRecoveryAsync(string identity, string code) =>
            VerifyPasswordRecoveryAsync(code, email: null, phone: null, identity: identity);

        public async Task VerifyPasswordRecoveryAsync(
            string code,
            string email = null,
            string phone = null,
            string identity = null)
        {
            var gameCode = RequireGameCode();
            if (string.IsNullOrWhiteSpace(code))
                throw new FastGameException("Verification code is required");

            ResolveContact(identity, ref email, ref phone);
            var body = BuildContactBody(gameCode, email, phone);
            body["code"] = code.Trim();
            await _http.RequestRawAsync("POST", "/base/recovery/verify", FastGameJson.Stringify(body));
        }

        /// <summary>
        /// Forgot password step 3/3: set new password after <see cref="VerifyPasswordRecoveryAsync"/>.
        /// Empty identity → ENTER-stored identity. No code and no full name — OTP was already checked in step 2.
        /// Auto-login on success (same as Register / Complete Account).
        /// </summary>
        public Task ConfirmPasswordRecoveryAsync(
            string identity,
            string newPassword,
            string newPasswordConfirm) =>
            ConfirmPasswordRecoveryAsync(
                newPassword, newPasswordConfirm,
                code: null, email: null, phone: null, identity: identity);

        /// <summary>
        /// Confirm new password. Prefer identity overload after Verify (omit <paramref name="code"/>).
        /// Empty identity → ENTER store. Advanced one-shot: pass <paramref name="code"/> with passwords.
        /// </summary>
        public async Task ConfirmPasswordRecoveryAsync(
            string newPassword,
            string newPasswordConfirm,
            string code = null,
            string email = null,
            string phone = null,
            string identity = null)
        {
            var gameCode = RequireGameCode();

            FastGameIdentity.RequireMatchingPasswords(newPassword, newPasswordConfirm);
            ResolveContact(identity, ref email, ref phone);

            var body = BuildContactBody(gameCode, email, phone);
            if (!string.IsNullOrWhiteSpace(code))
                body["code"] = code.Trim();
            body["new_password"] = newPassword;
            await _http.RequestRawAsync("POST", "/base/recovery/confirm", FastGameJson.Stringify(body));
            await LoginAsync(email ?? phone, newPassword);
        }

        /// <summary>Request phone OTP when the game has verify_phone and an SMS provider configured.</summary>
        public async Task RequestPhoneVerificationAsync(string phone = null)
        {
            if (!IsLoggedIn)
                throw new FastGameException("Not logged in");
            var gameCode = RequireGameCode();
            var body = new Dictionary<string, object> { { "game_code", gameCode } };
            if (!string.IsNullOrWhiteSpace(phone))
                body["phone"] = phone;
            await _http.RequestRawAsync("POST", "/base/login/phone/verification", FastGameJson.Stringify(body));
        }

        /// <summary>Confirm phone OTP for a game that requires phone verification.</summary>
        public async Task<UserProfile> ConfirmPhoneVerificationAsync(
            string code,
            string phone = null)
        {
            if (!IsLoggedIn)
                throw new FastGameException("Not logged in");
            var gameCode = RequireGameCode();
            var body = new Dictionary<string, object>
            {
                { "game_code", gameCode },
                { "code", code },
            };
            if (!string.IsNullOrWhiteSpace(phone))
                body["phone"] = phone;
            var text = await _http.RequestRawAsync(
                "POST",
                "/base/login/phone/verification/confirm",
                FastGameJson.Stringify(body));
            CurrentUser = ParseUserProfile(text);
            return CurrentUser;
        }

        /// <summary>Current user profile (id, email, phone, flags — no password). Requires login.</summary>
        public async Task<UserProfile> GetMeAsync()
        {
            if (!IsLoggedIn)
                throw new FastGameException("Not logged in");
            var text = await _http.RequestRawAsync("GET", "/base/login/me");
            CurrentUser = ParseUserProfile(text);
            return CurrentUser;
        }

        /// <summary>
        /// Server-validated auth gate. Returns false (and clears token) when missing/invalid.
        /// Use for login-vs-home branching; <see cref="IsAuthenticated"/> is the local-token check only.
        /// </summary>
        public async Task<bool> CheckAuthenticationAsync()
        {
            if (!IsLoggedIn)
                return false;
            try
            {
                await GetMeAsync();
                return true;
            }
            catch
            {
                Logout();
                return false;
            }
        }

        public void Logout()
        {
            SetAccessToken(null);
            LastLoginSucceeded = false;
            LastSignupSucceeded = false;
            CurrentUser = null;
        }

        public void SetAccessToken(string token)
        {
            _http.AccessToken = token;
            PersistAccessToken(token);
            if (!string.IsNullOrEmpty(token))
                OnLoggedIn?.Invoke();
        }

        /// <summary>Clear access token, ENTER-stored identity, and pending-payment cache.</summary>
        public void ClearLocalCache()
        {
            Logout();
            ClearEnteredIdentity();
            if (!string.IsNullOrEmpty(_config.PendingPaymentPrefsKey))
            {
                PlayerPrefs.DeleteKey(_config.PendingPaymentPrefsKey);
                PlayerPrefs.Save();
            }
        }

        void LoadPersistedAccessToken()
        {
            if (string.IsNullOrEmpty(_config.AccessTokenPrefsKey))
                return;
            var token = PlayerPrefs.GetString(_config.AccessTokenPrefsKey, "");
            if (!string.IsNullOrEmpty(token))
                _http.AccessToken = token;
        }

        void PersistAccessToken(string token)
        {
            if (string.IsNullOrEmpty(_config.AccessTokenPrefsKey))
                return;
            if (string.IsNullOrEmpty(token))
                PlayerPrefs.DeleteKey(_config.AccessTokenPrefsKey);
            else
                PlayerPrefs.SetString(_config.AccessTokenPrefsKey, token);
            PlayerPrefs.Save();
        }

        /// <summary>Bind Steam via Steamworks session ticket (native builds).</summary>
        public async Task<SteamLinkStatus> LinkSteamWithTicketAsync(
            string ticket,
            string identity = null)
        {
            var gameCode = RequireGameCode();
            var body = new Dictionary<string, object>
            {
                { "ticket", ticket },
                { "game_code", gameCode },
            };
            if (!string.IsNullOrEmpty(identity))
                body["identity"] = identity;
            var text = await _http.RequestRawAsync("POST", "/base/steam/link", FastGameJson.Stringify(body));
            var obj = FastGameJson.ParseObject(text);
            return new SteamLinkStatus
            {
                Linked = FastGameJson.GetBool(obj, "linked"),
                Steamid = FastGameJson.GetString(obj, "steamid"),
            };
        }

        public async Task<SteamLinkStatus> GetSteamStatusAsync()
        {
            var text = await _http.RequestRawAsync("GET", "/base/steam/status");
            var obj = FastGameJson.ParseObject(text);
            return new SteamLinkStatus
            {
                Linked = FastGameJson.GetBool(obj, "linked"),
                Steamid = FastGameJson.GetString(obj, "steamid"),
            };
        }

        public async Task UnlinkSteamAsync()
        {
            await _http.RequestRawAsync("DELETE", "/base/steam/link");
        }

        static UserProfile ParseUserProfile(string text)
        {
            var obj = FastGameJson.ParseObject(text);
            return new UserProfile
            {
                Id = FastGameJson.GetString(obj, "id"),
                Email = FastGameJson.GetString(obj, "email"),
                Phone = FastGameJson.GetString(obj, "phone"),
                EmailVerified = FastGameJson.GetBool(obj, "email_verified"),
                PhoneVerified = FastGameJson.GetBool(obj, "phone_verified"),
                FullName = FastGameJson.GetString(obj, "full_name"),
                IsActive = FastGameJson.GetBool(obj, "is_active", true),
                IsSuperuser = FastGameJson.GetBool(obj, "is_superuser"),
            };
        }

        bool TryFillFromEntered(out string email, out string phone)
        {
            email = null;
            phone = null;
            if (string.IsNullOrEmpty(EnteredIdentity))
                LoadPersistedEnteredIdentity();
            if (string.IsNullOrEmpty(EnteredIdentity))
                return false;
            if (EnteredChannel == FastGameIdentityChannel.Phone)
            {
                phone = EnteredIdentity;
                return true;
            }
            if (EnteredChannel == FastGameIdentityChannel.Email)
            {
                email = EnteredIdentity;
                return true;
            }
            if (FastGameIdentity.LooksLikeEmail(EnteredIdentity))
            {
                email = EnteredIdentity;
                return true;
            }
            if (FastGameIdentity.LooksLikePhone(EnteredIdentity))
            {
                phone = EnteredIdentity;
                return true;
            }
            return false;
        }

        void ResolveContact(string identity, ref string email, ref string phone)
        {
            var e = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
            var p = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            if (e == null && p == null)
            {
                if (string.IsNullOrWhiteSpace(identity))
                {
                    if (!TryFillFromEntered(out e, out p))
                        throw new FastGameException("No contact provided and no ENTER-stored identity");
                }
                else if (!FastGameIdentity.TrySplitContact(identity, out e, out p))
                {
                    throw new FastGameException("Provide a valid email or phone number");
                }
            }
            else if (e != null && !FastGameIdentity.LooksLikeEmail(e))
            {
                throw new FastGameException("Invalid email format");
            }
            else if (p != null && !FastGameIdentity.LooksLikePhone(p))
            {
                throw new FastGameException("Invalid phone format");
            }
            email = e;
            phone = p;
        }

        string RequireGameCode()
        {
            var gameCode = (_config.GameCode ?? "").Trim();
            if (string.IsNullOrEmpty(gameCode))
                throw new FastGameException(
                    "FastGame: GameCode not set — call Initialize Game");
            return gameCode;
        }

        static Dictionary<string, object> BuildContactBody(string gameCode, string email, string phone)
        {
            var body = new Dictionary<string, object> { { "game_code", gameCode } };
            if (!string.IsNullOrEmpty(email))
                body["email"] = email;
            if (!string.IsNullOrEmpty(phone))
                body["phone"] = phone;
            return body;
        }
    }
}

using System.Threading.Tasks;
using UnityEngine;

namespace FastGame
{
    /// <summary>
    /// Scene component that owns the shared <see cref="FastGameClient"/>.
    /// Initialize Game (1x build + OS) then Initialize Client (Nx network). Auth / Shop resolve this automatically.
    /// </summary>
    [AddComponentMenu("Fast Game/Client")]
    [DisallowMultipleComponent]
    public sealed class FastGameClientBehaviour : MonoBehaviour
    {
        public static FastGameClientBehaviour Instance { get; private set; }

        [Tooltip("Initialize Client: full API base (prefer http://api.localhost/api/v1). Host-only api.localhost also normalizes.")]
        public string ApiBaseUrl = "http://api.localhost/api/v1";

        [Tooltip("Initialize Game: active catalog game (storage NAME). Auth OTP / recovery use this.")]
        public string GameCode = "game";

        [Tooltip("Initialize Game: this APK store platform.")]
        public FastGameStorePlatform StorePlatform = FastGameStorePlatform.Unset;

        [Tooltip("Initialize Client: Dev / Production / EarlyAccess — token must match this stage.")]
        public FastGameProjectStage ProjectStage = FastGameProjectStage.Dev;

        [Tooltip("Initialize Client: single build access token for the selected project stage.")]
        public string ClientAccessToken = "fg-dev-game";

        [Tooltip("Optional override. Leave empty to fetch Cafe Bazaar / Myket RSA from Editor payment config after login (do not paste JWT / api_secret).")]
        public string StorePublicKey = "";

        public bool PersistAcrossScenes = true;

        public FastGameClient Client { get; private set; }

        public bool IsInitialized => Client != null;

        public string LastSetupMessage { get; private set; } = "";

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (PersistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            if (!InitializeGame())
            {
                if (!string.IsNullOrEmpty(LastSetupMessage))
                    Debug.LogError("FastGame Initialize Game: " + LastSetupMessage);
            }
            InitializeClient(ApiBaseUrl);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>One-time build config + OS store install check. Does not wipe token or Enter identity.</summary>
        public bool InitializeGame(string gameCode = null, FastGameStorePlatform? storePlatform = null)
        {
            if (gameCode != null)
                GameCode = gameCode.Trim();
            if (storePlatform.HasValue)
                StorePlatform = storePlatform.Value;

            ApplyGameConfigToClient();
            return EnsureSetup(out _);
        }

        /// <summary>Obsolete string overload — prefer <see cref="InitializeGame(string, FastGameStorePlatform?)"/>.</summary>
        [System.Obsolete("Use InitializeGame(gameCode, FastGameStorePlatform)")]
        public bool InitializeGame(string gameCode, string storePlatform)
        {
            FastGameStorePlatform parsed = FastGameConfig.StorePlatformFromId(storePlatform);
            return InitializeGame(gameCode, parsed);
        }

        /// <summary>Network / reconnect only. Registers project stage + client access token with the server.</summary>
        public bool InitializeClient(string apiBaseUrl)
        {
            ApiBaseUrl = FastGameConfig.NormalizeApiBaseUrl(apiBaseUrl);
            LastSetupMessage = "";

            if (Client != null
                && string.Equals(Client.Config.ApiBaseUrl, ApiBaseUrl, System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyGameConfigToClient();
                return RegisterClientBuildSync();
            }

            Client = new FastGameClient(new FastGameConfig
            {
                ApiBaseUrl = ApiBaseUrl,
                GameCode = GameCode ?? "",
                StorePlatform = FastGameConfig.StorePlatformToId(StorePlatform),
                StorePublicKey = StorePublicKey ?? "",
                ProjectStage = ProjectStage,
                ClientAccessToken = ClientAccessToken ?? "",
            });
            ApplyGameConfigToClient();
            return RegisterClientBuildSync();
        }

        /// <summary>Obsolete — call <see cref="InitializeGame"/> (1×) then 1-arg <see cref="InitializeClient(string)"/> (N×).</summary>
        [System.Obsolete("Use InitializeGame(gameCode, storePlatform) then InitializeClient(apiBaseUrl).")]
        public bool InitializeClient(string apiBaseUrl, string gameCode, string storePlatform)
        {
            var gameOk = InitializeGame(gameCode, storePlatform);
            var netOk = InitializeClient(apiBaseUrl);
            return gameOk && netOk;
        }

        bool RegisterClientBuildSync()
        {
            ApplyGameConfigToClient();
            var task = FastGameClientBuild.InitializeAsync(Client.Http, Client.Config);
            task.Wait();
            var (ok, message) = task.Result;
            if (!ok)
            {
                LastSetupMessage = message;
                Debug.LogError("FastGame Initialize Client: " + message);
            }
            return ok;
        }

        public bool EnsureSetup(out string message)
        {
            message = "";
            LastSetupMessage = "";

            if (!string.IsNullOrEmpty(StorePublicKey))
                FastGameStore.StorePublicKey = StorePublicKey;
            else if (Client != null && !string.IsNullOrEmpty(Client.Config.StorePublicKey))
                FastGameStore.StorePublicKey = Client.Config.StorePublicKey;

            ApplyGameConfigToClient();

            var provider = FastGameConfig.StorePlatformToId(
                Client != null
                    ? FastGameConfig.StorePlatformFromId(Client.Config.StorePlatform)
                    : StorePlatform);
            if (!IsAndroidStore(provider))
                return true;

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!FastGameStore.IsStoreAppInstalled(provider))
            {
                message = FastGameConfig.StoreNotInstalledMessage(provider);
                LastSetupMessage = message;
                return false;
            }
#else
            message = "FastGameStore: editor/non-Android — install check skipped";
            LastSetupMessage = message;
#endif
            return true;
        }

        public void SetGameCode(string gameCode)
        {
            GameCode = (gameCode ?? "").Trim();
            ApplyGameConfigToClient();
        }

        public void SetStorePlatform(FastGameStorePlatform storePlatform)
        {
            StorePlatform = storePlatform;
            ApplyGameConfigToClient();
        }

        public void SetStorePublicKey(string publicKey)
        {
            StorePublicKey = (publicKey ?? "").Trim();
            ApplyGameConfigToClient();
            FastGameStore.StorePublicKey = StorePublicKey;
        }

        void ApplyGameConfigToClient()
        {
            if (Client == null)
                return;
            Client.Config.GameCode = GameCode ?? "";
            Client.Config.StorePlatform = FastGameConfig.StorePlatformToId(StorePlatform);
            Client.Config.ProjectStage = ProjectStage;
            Client.Config.ClientAccessToken = ClientAccessToken ?? "";
            if (!string.IsNullOrEmpty(StorePublicKey))
                Client.Config.StorePublicKey = StorePublicKey;
            var rsa = !string.IsNullOrEmpty(StorePublicKey)
                ? StorePublicKey
                : (Client.Config.StorePublicKey ?? "");
            FastGameStore.StorePublicKey = rsa;
            if (Client.Auth.IsLoggedIn)
                _ = Client.Shop.BindStoreLockAsync();
        }

        static bool IsAndroidStore(string provider)
        {
            return provider == "myket" || provider == "caffebazar" || provider == "googleplay";
        }

        public static FastGameClient RequireClient(FastGameClientBehaviour preferred = null)
        {
            var host = preferred != null ? preferred : Instance;
            if (host == null)
                host = FindObjectOfType<FastGameClientBehaviour>();
            if (host == null || host.Client == null)
                throw new FastGameException(
                    "No FastGameClientBehaviour in scene — add Fast Game/Client and call Initialize Client");
            return host.Client;
        }
    }
}

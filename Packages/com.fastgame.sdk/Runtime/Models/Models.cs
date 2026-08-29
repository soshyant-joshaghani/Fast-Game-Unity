using System.Collections.Generic;

namespace FastGame.Models
{
    public class GameCatalog
    {
        public string Id;
        public string GameId;
        public string Label;
        public string Description;
        public string ColyseusRoom;
        public bool Available;
        /// <summary>en / fa / ar → name + description. See docs/entity-locales.md</summary>
        public Dictionary<string, LocaleCopy> Translations = new Dictionary<string, LocaleCopy>();
    }

    /// <summary>One locale entry inside Translations (read-only for SDKs; editor writes).</summary>
    public sealed class LocaleCopy
    {
        public string Name;
        public string Description;
    }

    public sealed class GameMode
    {
        public string Id;
        public string ModeId;
        public string Topology;
        public string WinKind;
        public int MinPlayers;
        public int MaxPlayers;
        public string Kind;
    }

    public sealed class GameMap
    {
        public string Id;
        public string MapId;
        public string Label;
        public List<string> SupportedModes = new List<string>();
        public bool Purchasable;
        public int Price;
        public Dictionary<string, LocaleCopy> Translations = new Dictionary<string, LocaleCopy>();
    }

    public sealed class AssetPack
    {
        public string Id;
        public string PackId;
        public string Label;
        public int Revision;
        public string Version;
        public string Url;
        public string Hash;
    }

    public sealed class GameCatalogDetail : GameCatalog
    {
        public List<GameMode> Modes = new List<GameMode>();
        public List<GameMap> Maps = new List<GameMap>();
        public List<AssetPack> AssetPacks = new List<AssetPack>();
        /// <summary>Per-game new-user OTP gates (provider ready + verify flag).</summary>
        public bool AuthVerifyPhone;
        public bool AuthVerifyEmail;
    }

    public sealed class Character
    {
        public string Id;
        public string CharacterId;
        public string Label;
        /// <summary>player | npc | both</summary>
        public string Role;
        public string BodyKind;
        public Dictionary<string, object> Stats = new Dictionary<string, object>();
        public Dictionary<string, LocaleCopy> Translations = new Dictionary<string, LocaleCopy>();
        public int SortOrder;
    }

    public sealed class Cosmetic
    {
        public string Id;
        public string CosmeticId;
        public string Slot;
        public string Label;
        public string Availability;
        public int Price;
        public string AssetRef;
    }

    public sealed class Ability
    {
        public string Id;
        public string AbilityId;
        public string Label;
        public string Kind;
        public Dictionary<string, object> Params = new Dictionary<string, object>();
    }

    public sealed class Loadout
    {
        public string UserId;
        public string GameCode;
        public string CharacterId;
        public Dictionary<string, string> EquippedCosmetics = new Dictionary<string, string>();
        public Dictionary<string, string> ModularParts = new Dictionary<string, string>();
        public int Level;
        public int Xp;
    }

    public sealed class ShopLine
    {
        public string GameCode;
        public string SkuKind;
        public string SkuId;
        public string Label;
        public int Price;
        public bool Owned;
        public Dictionary<string, object> Meta = new Dictionary<string, object>();
    }

    public sealed class PaymentInitiateResult
    {
        public string Authority;
        public string PaymentUrl;
        public string PaymentToken;
        public int Amount;
        public string Provider;
        public string StoreProductId;
        public string OrderId;
    }

    public sealed class PaymentVerifyResult
    {
        public bool Success;
        public bool Owned;
        public string Message;
    }

    public sealed class ShopUnlockResult
    {
        public bool Owned;
        public bool Pending;
        public bool Locked;
        public string Mode;
        public string Provider;
        public string Authority;
        public string PaymentToken;
        public string PaymentUrl;
        public string StoreProductId;
        public string OrderId;
        public int Amount;
        public string Currency;
        public bool Success;
    }

    public sealed class PreparedSession
    {
        public GameCatalogDetail Game;
        public Dictionary<string, object> MapRuntime;
        public Dictionary<string, object> Spawn;
        public string GameId;
        public string ModeId;
        public string MapId;
        public string ColyseusRoom;
    }

    public sealed class GameServerInfo
    {
        public string Url;
    }

    /// <summary>POST /apps/games/realtime/seat — JoinMap ticket (prefer over GetGameServer).</summary>
    public sealed class SeatMintResult
    {
        public string SeatToken;
        public string ExpiresAt;
        public string GameServerUrl;
        public string RoomName;
        public string GameId;
        public string MapId;
        public string ModeId;
    }

    /// <summary>Public user profile from GET /base/login/me (no password).</summary>
    public sealed class UserProfile
    {
        public string Id;
        public string Email;
        public string Phone;
        public bool EmailVerified;
        public bool PhoneVerified;
        public string FullName;
        public bool IsActive = true;
        public bool IsSuperuser;
    }

    public sealed class AdvertisementRequest
    {
        public string GameId;
        public string Slot;
        public string MediaType;
        public string Format;
        public List<string> Tags = new List<string>();
        public string Locale;
        public string Country;
        public string Platform;
        public string Engine;
        public Dictionary<string, object> Capabilities = new Dictionary<string, object>();
    }

    public sealed class AdvertisementMedia
    {
        public string Type;
        public string Url;
        public int Width;
        public int Height;
    }

    public sealed class AdvertisementClick
    {
        public bool Enabled;
        public string Url;
    }

    public sealed class AdvertisementTracking
    {
        public string ImpressionUrl;
        public string ClickUrl;
    }

    /// <summary>Provider-opaque ad payload from POST /apps/games/ads/request.</summary>
    public sealed class Advertisement
    {
        public string Id;
        public string CampaignId;
        public AdvertisementMedia Media = new AdvertisementMedia();
        public AdvertisementClick Click = new AdvertisementClick();
        public AdvertisementTracking Tracking = new AdvertisementTracking();
        /// <summary>Extensible creative meta. Text ads: title, body, background_url, background_color.</summary>
        public Dictionary<string, object> Meta = new Dictionary<string, object>();
        public string Title;
        public string Body;
        public string BackgroundUrl;
        public string BackgroundColor;
    }

    public sealed class AdvertisementEvent
    {
        /// <summary>AdvertisementDisplayed | AdvertisementClicked | AdvertisementClosed</summary>
        public string EventType;
        public string AdId;
        public string GameId;
        public string CampaignId;
        public string Timestamp;
        public Dictionary<string, object> Extras = new Dictionary<string, object>();
    }

    public sealed class CollectibleDef
    {
        public string Id;
        public string Code;
        public string Label;
        public string ImageUrl;
        public bool Locked;
    }
}

namespace FastGame
{
    /// <summary>
    /// Official Fast Game client — named FastAPI surface only.
    /// For multiplayer, install the Colyseus SDK as a sibling and join using catalog
    /// <c>colyseus_room</c> + <see cref="FastGameCatalog.GetGameServerAsync"/>.
    /// </summary>
    public sealed class FastGameClient
    {
        public FastGameConfig Config { get; }
        public FastGameHttp Http { get; }
        public FastGameAuth Auth { get; }
        public FastGameCatalog Catalog { get; }
        public FastGameContent Content { get; }
        public FastGameShop Shop { get; }
        public FastGameAssets Assets { get; }
        public FastGameAds Ads { get; }

        public FastGameClient(FastGameConfig config = null)
        {
            Config = config ?? new FastGameConfig();
            Http = new FastGameHttp(Config);
            Auth = new FastGameAuth(Http, Config);
            Catalog = new FastGameCatalog(Http);
            Content = new FastGameContent(Http, Catalog);
            Shop = new FastGameShop(Http, Config);
            Assets = new FastGameAssets();
            Ads = new FastGameAds(Http);
            Auth.OnLoggedIn = () => { _ = Shop.BindStoreLockAsync(); };
            if (Auth.IsLoggedIn)
                _ = Shop.BindStoreLockAsync();
        }
    }
}

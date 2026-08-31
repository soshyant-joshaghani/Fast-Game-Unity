namespace FastGame
{
    /// <summary>Client build stage — must match the configured access token for that stage.</summary>
    public enum FastGameProjectStage
    {
        Dev,
        Production,
        EarlyAccess,
    }

    /// <summary>Initialize Game store / payment platform (matches Unreal EFastGameStorePlatform).</summary>
    public enum FastGameStorePlatform
    {
        Unset,
        Myket,
        CafeBazaar,
        GooglePlay,
        Steam,
        ZarinPal,
        AppStore,
    }
}

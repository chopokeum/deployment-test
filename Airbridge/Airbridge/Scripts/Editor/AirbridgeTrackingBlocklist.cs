#if UNITY_EDITOR

/// <summary>
/// Tracking data types to block from collection
/// </summary>
public enum AirbridgeTrackingBlocklist
{
    // ios
    IDFA,
    IDFV,

    // android
    GAID,
    OAID,
    AppSetID
}

// Exclude a class from the document
/// @cond HIDDEN_SYMBOLS
public static class AirbridgeTrackingBlocklistExtension
{
    public static readonly string[] Values =
    {
        nameof(AirbridgeTrackingBlocklist.IDFA),
        nameof(AirbridgeTrackingBlocklist.IDFV),
        nameof(AirbridgeTrackingBlocklist.GAID),
        nameof(AirbridgeTrackingBlocklist.OAID),
        nameof(AirbridgeTrackingBlocklist.AppSetID)
    };
}

// ReSharper disable once InvalidXmlDocComment
/// @endcond

#endif

#if UNITY_EDITOR

/// <summary>
/// Tracking data types to block from collection
/// </summary>
internal enum AirbridgeTrackingBlocklist
{
    // ios
    IDFA,
    IDFV,

    // android
    GAID,
    OAID,
    AppSetID
}

internal static class AirbridgeTrackingBlocklistExtension
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

#endif
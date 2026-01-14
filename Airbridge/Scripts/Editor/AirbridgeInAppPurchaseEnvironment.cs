#if UNITY_EDITOR

/// <summary>
/// Enumeration of in-app purchase environment used in [AirbridgeData](@ref AirbridgeData#inAppPurchaseEnvironment).
/// </summary>
public enum AirbridgeInAppPurchaseEnvironment
{
    Production,
    Sandbox
}

// Exclude a class from the document
/// @cond HIDDEN_SYMBOLS
public static class AirbridgeInAppPurchaseEnvironmentExtension
{
    public static readonly string[] Environments =
    {
        AirbridgeInAppPurchaseEnvironment.Production.ToLowerString(),
        AirbridgeInAppPurchaseEnvironment.Sandbox.ToLowerString()
    };

    public static string ToLowerString(this AirbridgeInAppPurchaseEnvironment environment)
    {
        return environment.ToString().ToLower();
    }
}
// ReSharper disable once InvalidXmlDocComment
/// @endcond

#endif
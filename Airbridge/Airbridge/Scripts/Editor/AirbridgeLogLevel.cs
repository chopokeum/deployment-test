#if UNITY_EDITOR

using System;

/// <summary>
/// Enumeration of log levels used in [AirbridgeData](@ref AirbridgeData#logLevel).
/// </summary>
public enum AirbridgeLogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fault
}

// Exclude a class from the document
/// @cond HIDDEN_SYMBOLS
public static class AirbridgeLogLevelExtension
{
    public static readonly string[] Levels =
    {
        nameof(AirbridgeLogLevel.Debug),
        nameof(AirbridgeLogLevel.Info),
        nameof(AirbridgeLogLevel.Warning),
        nameof(AirbridgeLogLevel.Error),
        nameof(AirbridgeLogLevel.Fault)
    };

    public static int ToInt(this AirbridgeLogLevel level)
    {
        return Convert.ToInt32(level);
    }
}
// ReSharper disable once InvalidXmlDocComment
/// @endcond

#endif
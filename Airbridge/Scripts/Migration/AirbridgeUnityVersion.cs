// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System;
using System.Collections.Generic;
using System.Linq;

namespace Assets.Airbridge.Scripts.Migration
{
    internal static class AirbridgeUnityVersion
    {
        internal const string Author = "AB180";
        internal const string Version = @"${WRAPPER_VERSION}";

        internal static int CompareVersion(string currentVersion, string otherVersion)
        {
            try
            {
                IEnumerable<int> currentParts = currentVersion.Split('.').Select(int.Parse);
                IEnumerable<int> otherParts = otherVersion.Split('.').Select(int.Parse);

                return currentParts
                    .Zip(otherParts, (current, other) => current.CompareTo(other))
                    .FirstOrDefault(result => result != 0);
            }
            catch (FormatException)
            {
                return 1; // Return 1 if parsing fails, indicating a higher version
            }
        }
    }
}
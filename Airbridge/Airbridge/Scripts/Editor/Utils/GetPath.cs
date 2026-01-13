#if UNITY_EDITOR

using System;
using System.IO;
using UnityEngine;

// Exclude a class from the document
/// @cond HIDDEN_SYMBOLS
partial class AirbridgeUtils
{
    /// <summary>
    /// Airbridge Unity SDK 의 Assets 폴더 경로를 반환합니다,
    /// </summary>
    internal static string GetUnityPackageAssetsPath()
    {
        string path = Application.dataPath;

        try
        {
            string packagePath = Path.GetFullPath("Packages/co.ab180.airbridge-unity-sdk/Assets");
            if (Directory.Exists(packagePath))
            {
                path = packagePath;
            }
        }
        catch (Exception)
        {
            /* ignored */
        }

        return path;
    }

    /// <summary>
    /// Unity 프로젝트의 Plugins/{Platform}/Airbridge 폴더 경로를 반환합니다.
    /// 해당 경로가 존재하지 않으면 새로 생성한 후 반환합니다.
    /// </summary>
    internal static string GetPluginPath(Platform platform)
    {
        try
        {
            string airbridgeDirPath = Path.Combine(Application.dataPath, "Plugins", platform.ToString(), "Airbridge");

            if (!Directory.Exists(airbridgeDirPath))
            {
                Directory.CreateDirectory(airbridgeDirPath);
                Debug.LogFormat(
                    "Create Airbridge {0} Plugin directory : {1}",
                    platform.ToString(),
                    airbridgeDirPath
                );
            }

            return airbridgeDirPath;
        }
        catch (Exception exception)
        {
            Debug.LogErrorFormat(
                "Something broken while getting Airbridge {0} Plugin directory path : {1}",
                platform.ToString(),
                exception
            );
            return null;
        }
    }

    // ReSharper disable once InconsistentNaming
    internal enum Platform
    {
        Android,
        iOS
    }
}
// ReSharper disable once InvalidXmlDocComment
/// @endcond

#endif
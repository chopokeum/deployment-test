#if UNITY_EDITOR

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Exclude a class from the document
/// @cond HIDDEN_SYMBOLS
partial class AirbridgeUtils
{
    private static string ParentDirectory(string path, int depth = 0)
    {
        if (depth < 0) depth = 0;

        var dir = path;
        for (var i = 0; i < depth; i++)
        {
            dir = Path.GetDirectoryName(dir);
        }

        return dir;
    }

    /// <summary>
    /// Airbridge Unity SDK 의 Assets 폴더 경로를 반환합니다,
    /// </summary>
    internal static string GetUnityPackageAssetsPath()
    {
        try
        {
            MonoScript script = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<AirbridgeData>());

            // scriptPath = ".../AirbridgeData.cs"
            string scriptPath = AssetDatabase.GetAssetPath(script);
            string assetPath = ParentDirectory(scriptPath, 4);
            return Path.GetFullPath(assetPath);
        }
        catch (Exception)
        {
            /* ignored */
        }

        return Application.dataPath;
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

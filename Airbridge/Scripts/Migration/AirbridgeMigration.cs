using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR

namespace Assets.Airbridge.Scripts.Migration
{
    [InitializeOnLoad]
    public class AirbridgeEditorInitializer
    {
        static AirbridgeEditorInitializer()
        {
            string latestVersion = AirbridgeMigration.GetLatestVersion();
            if (latestVersion == null) { return; }
            if (latestVersion == AirbridgeUnityVersion.Version) { return; }
         
            Debug.Log("Migration started: " + latestVersion + " -> " + AirbridgeUnityVersion.Version);
            AirbridgeMigration.Migration(latestVersion);
            AirbridgeMigration.WriteLatestVersion();
            Debug.Log("Migration completed");
        }
    }

    internal struct Migration
    {
        internal string Version;
        internal Action Action;
    }
    internal class AirbridgeMigration : ScriptableObject
    {
        private static readonly List<Migration> Migrations = new List<Migration>
        {
            new Migration
            {
                Version = "4.7.0",
                Action = () =>
                {
                    string airbridgeDirectoryPath = GetAirbridgeDirectoryPath();
                    string path = Path.Combine(
                        airbridgeDirectoryPath, 
                        "Scripts",
                        "Editor",
                        "Utils",
                        "ArrayDataUtils.cs"
                    );

                    SafeRemoveFile(path);
                }
            }
        };

        private static void SafeRemoveFile(string filePath)
        {
            List<string> paths = new List<string>
            {
                filePath, filePath + ".meta"
            };
            
            paths.ForEach(path =>
            {
                if (!File.Exists(path)) return;
                try { File.Delete(path); }
                catch (Exception) { /* ignored */ }
            });
        }

        internal static string GetLatestVersion()
        {
            if (AirbridgeUnityVersion.Version.Split('.').Length < 3) { return null; }
            
            string latestVersionFilePath = Path.Combine(
                GetMigrationDirectoryPath(),
                "LatestVersion.txt"
            );
            if (!File.Exists(latestVersionFilePath))
            {
                WriteLatestVersion();
            }
                
            return File.ReadAllText(latestVersionFilePath);
        }

        internal static void WriteLatestVersion()
        {
            string latestVersionFilePath = Path.Combine(
                GetMigrationDirectoryPath(),
                "LatestVersion.txt"
            );
            File.WriteAllText(latestVersionFilePath, AirbridgeUnityVersion.Version);
        }
        
        private static string GetMigrationDirectoryPath()
        {
            return Path.Combine(
                GetAirbridgeDirectoryPath(), 
                "Scripts",
                "Migration"
            );
        }

        private static string GetAirbridgeDirectoryPath()
        {
            MonoScript script = MonoScript.FromScriptableObject(CreateInstance<AirbridgeMigration>());
            string assetPath = AssetDatabase.GetAssetPath(script);
            string parentDirectory = Path.GetDirectoryName(Path.GetDirectoryName(assetPath));

            return Path.GetDirectoryName(parentDirectory);
        }

        internal static void Migration(string latestVersion)
        {
            Migrations
                .Where(migration => AirbridgeUnityVersion.CompareVersion(
                    latestVersion, migration.Version
                ) <= 0).ToList()
                .ForEach(migration =>
                {
                    Debug.Log("Running migration for version: " + migration.Version);
                    migration.Action.Invoke();
                });
        }
    }
}

#endif

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

[InitializeOnLoad]
internal class AirbridgeMigrationHandler
{
    private const string Version = "${WRAPPER_VERSION}";

    private static string MigrationDirectoryPath => Path.Combine(AirbridgeFileUtils.GetPackageDataPath(),
        "Airbridge", "Scripts", "Editor", "Migration");

    private static string SavedVersionFilePath => Path.Combine(MigrationDirectoryPath, "SavedVersion.txt");

    private static AirbridgeVersion SavedVersion
    {
        get
        {
            var version = !File.Exists(SavedVersionFilePath)
                ? "0.0.0"
                : File.ReadAllText(SavedVersionFilePath);

            return version.ToAirbridgeVersion();
        }
    }

    private static readonly List<AirbridgeMigration> Migrations = new List<AirbridgeMigration>()
    {
        new AirbridgeMigration.V4_8_0()
    };

    static AirbridgeMigrationHandler()
    {
        if (Application.isBatchMode) return;

        var sortedMigrations = Migrations
            .OrderBy(migration => migration.Version)
            .Where(migration => migration.Version.CompareTo(Version.ToAirbridgeVersion()) <= 0)
            .ToList();

        sortedMigrations.ForEach(migration =>
        {
            if (SavedVersion.CompareTo(migration.Version) < 0)
            {
                try
                {
                    Debug.Log($"[Airbridge] Running migration for version: {migration.Version}");
                    migration.Migrate();
                }
                catch
                {
                    /* ignored */
                }
            }
        });

        // Write latest version
        File.WriteAllText(SavedVersionFilePath, Version);
    }
}

#endif
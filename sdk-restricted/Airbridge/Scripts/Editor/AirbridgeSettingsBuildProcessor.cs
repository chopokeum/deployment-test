#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

internal class AirbridgeSettingsBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Console.WriteLine("AirbridgeSettingsBuildProcessor.OnPreprocessBuild for target " + report.summary.platform);

        AirbridgeSettingsWindow.IsDevelopment = (report.summary.options & BuildOptions.Development) != 0;

        switch (report.summary.platform)
        {
            case BuildTarget.Android:
                AirbridgeSettingsWindow.UpdateAndroidNativeCode();
                AirbridgeSettingsWindow.UpdateAndroidAirbridgeSettings();
                break;
            case BuildTarget.iOS:
                AirbridgeSettingsWindow.UpdateIOSAppSetting();
                break;
            default:
                return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

#endif
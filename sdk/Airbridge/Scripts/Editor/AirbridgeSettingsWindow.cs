#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;

// ReSharper disable once CheckNamespace
internal class AirbridgeSettingsWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private List<AirbridgeDataToggleSection> _sections;

    [MenuItem("Airbridge/Airbridge Settings")]
    public static void ShowWindow()
    {
        EditorWindow window = GetWindow<AirbridgeSettingsWindow>("Airbridge Settings");

        // Set minimum size
        float minWidth = 900;
        float minHeight = 900;
        window.minSize = new Vector2(minWidth, minHeight);
    }

    private void OnEnable()
    {
        _sections = new List<AirbridgeDataToggleSection>
        {
            new AirbridgeDataToggleSection("Default", AirbridgeData.Variant.Default) { Expanded = true },
            new AirbridgeDataToggleSection("Development", AirbridgeData.Variant.Dev),
            new AirbridgeDataToggleSection("Production", AirbridgeData.Variant.Prod),
        };
    }

    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(
            _scrollPos,
            GUILayout.Width(position.width),
            GUILayout.Height(position.height)
        );

        for (int i = 0; i < _sections.Count; i++)
        {
            bool newExpanded = _sections[i].Foldout();

            if (newExpanded && !_sections[i].Expanded)
            {
                for (int j = 0; j < _sections.Count; j++)
                {
                    _sections[j].Expanded = (j == i);
                }
            }
            else
            {
                _sections[i].Expanded = newExpanded;
            }

            _sections[i].Draw();
        }

        EditorGUILayout.EndScrollView();
    }

    private static bool? _isDevelopment;

    internal static bool IsDevelopment
    {
        get
        {
            if (_isDevelopment.HasValue) return _isDevelopment.Value;

#if DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
        set { _isDevelopment = value; }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    internal static AirbridgeData GetAirbridgeData()
    {
        if (IsDevelopment)
        {
            var dev = AirbridgeScriptableObject.GetInstance<AirbridgeData>(
                AirbridgeData.GetAssetName(AirbridgeData.Variant.Dev));
            if (dev.isActive) return dev;
        }
        else
        {
            var prod = AirbridgeScriptableObject.GetInstance<AirbridgeData>(
                AirbridgeData.GetAssetName(AirbridgeData.Variant.Prod));
            if (prod.isActive) return prod;
        }

        return AirbridgeScriptableObject.GetInstance<AirbridgeData>(
            AirbridgeData.GetAssetName(AirbridgeData.Variant.Default));
    }

    internal static void UpdateAndroidNativeCode()
    {
        AirbridgeAndroidApplicationEntry entry = AirbridgeAndroidApplicationEntry.None;
#if UNITY_2023_1_OR_NEWER
        entry = PlayerSettings.Android.applicationEntry.ConvertToAirbridgeType();
#else
        entry = AirbridgeAndroidApplicationEntry.Activity;
#endif

        UpdateAndroidActivity(entry);
        UpdateAndroidManifest(entry);
    }
    
    private static void UpdateAndroidActivity(AirbridgeAndroidApplicationEntry entry)
    {
        string pluginPath = AirbridgeFileUtils.GetProjectAirbridgePluginPath(AirbridgeFileUtils.Platform.Android);
        if (pluginPath == null) return;

        try
        {
            // 기존에 생성된 Airbridge Activity 파일들을 모두 삭제한다.
            string airbridgeActivityPath =
                Path.Combine(pluginPath, AirbridgeAndroidApplicationEntry.Activity.GetActivityFileName());
            string airbridgeGameActivityPath =
                Path.Combine(pluginPath, AirbridgeAndroidApplicationEntry.GameActivity.GetActivityFileName());

            if (File.Exists(airbridgeActivityPath))
            {
                File.Delete(airbridgeActivityPath);
            }

            if (File.Exists(airbridgeGameActivityPath))
            {
                File.Delete(airbridgeGameActivityPath);
            }

            // AirbridgeAndroidApplicationEntry가 None이면 처리를 종료한다.
            if (entry.IsNone()) return;

            // AirbridgeAndroidApplicationEntry 값에 해당하는 Airbridge Activity 파일을 생성한다.
            string defaultActivityPath = Path.Combine(
                AirbridgeFileUtils.GetPackageAirbridgePluginPath(AirbridgeFileUtils.Platform.Android),
                "java/co/ab180/airbridge/unity",
                entry.GetActivityFileName() + ".template"
            );
            string activityPath = Path.Combine(pluginPath, entry.GetActivityFileName());

            File.Copy(defaultActivityPath, activityPath);
            Debug.Log("[Airbridge] Updated Airbridge Android Activity file.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Airbridge] Failed to update Airbridge Android Activity file: {e.Message}");
        }
    }

    private static void UpdateAndroidManifest(AirbridgeAndroidApplicationEntry entry)
    {
        try
        {
            string destAndroidManifestPath = Path.Combine(
                AirbridgeFileUtils.GetProjectPluginPath(AirbridgeFileUtils.Platform.Android),
                "AndroidManifest.xml");

            // AirbridgeAndroidApplicationEntry가 None이 아닌 경우,
            // AndroidManifest.xml이 존재하지 않으면 기본 AndroidManifest.xml 템플릿을 추가한다.
            if (!entry.IsNone())
            {
                string srcAndroidManifestPath = Path.Combine(
                    AirbridgeFileUtils.GetPackageAirbridgePluginPath(AirbridgeFileUtils.Platform.Android),
                    entry.GetManifestFileName());

                if (AirbridgeFileUtils.PrepareFile(destAndroidManifestPath))
                {
                    File.Copy(srcAndroidManifestPath, destAndroidManifestPath, true);
                }
            }

            AndroidManifest androidManifest = new AndroidManifest(destAndroidManifestPath);
            androidManifest.SetPackageName(Application.identifier);
            androidManifest.SetPermission("android.permission.INTERNET");
            androidManifest.SetPermission("android.permission.ACCESS_NETWORK_STATE");

            androidManifest.SetContentProvider("co.ab180.airbridge.unity.AirbridgeContentProvider",
                "${applicationId}.co.ab180.airbridge.unity.AirbridgeContentProvider", "false");

            // AirbridgeAndroidApplicationEntry가 None이 아닌 경우,
            // 기존에 추가된 AndroidManifest.xml 템플릿을 현재 빌드 시점의 AirbridgeAndroidApplicationEntry 값에 맞게 수정한다.
            if (!entry.IsNone())
            {
                androidManifest.ReplaceActivityName(entry.GetUnityPlayerActivityName(),
                    entry.GetAirbridgeActivityName());

                androidManifest.ReplaceTheme(AirbridgeAndroidApplicationEntryExtension.Themes, entry.GetTheme());
                androidManifest.ReplaceActivityName(AirbridgeAndroidApplicationEntryExtension.AirbridgeActivityNames,
                    entry.GetAirbridgeActivityName());
            }

            androidManifest.Save(destAndroidManifestPath);

            // Airbridge Setting의 다음 값을 참고하여 AndroidManifest.xml을 수정한다.
            //  - appName
            //  - androidURIScheme
            //  - customDomain
            //
            // Default, Dev, Prod 환경별로 서로 다른 값을 가지므로, 별도의 AndroidManifest.xml 파일로 관리한다.
            AirbridgeData airbridgeData = GetAirbridgeData();

            string destLibraryAndroidManifestPath = Path.Combine(
                AirbridgeFileUtils.GetProjectPluginPath(AirbridgeFileUtils.Platform.Android),
                "Airbridge.androidlib", "AndroidManifest.xml");

            string srcLibraryAndroidManifestPath = Path.Combine(
                AirbridgeFileUtils.GetPackageAirbridgePluginPath(AirbridgeFileUtils.Platform.Android),
                "Library_AndroidManifest.xml");

            if (AirbridgeFileUtils.PrepareFile(destLibraryAndroidManifestPath))
            {
                File.Copy(srcLibraryAndroidManifestPath, destLibraryAndroidManifestPath, true);
            }

            AndroidManifest airbridgeAndroidManifest = new AndroidManifest(destLibraryAndroidManifestPath);
            airbridgeAndroidManifest.ResetAirbridgeAndroidManifest(androidManifest);

            // Airbridge App Links
            if (!string.IsNullOrEmpty(airbridgeData.appName))
            {
                airbridgeAndroidManifest.SetUnityActivityAppLinksIntentFilter($"{airbridgeData.appName}.abr.ge");
                airbridgeAndroidManifest.SetUnityActivityAppLinksIntentFilter($"{airbridgeData.appName}.airbridge.io");
                airbridgeAndroidManifest.SetUnityActivityAppLinksIntentFilter($"{airbridgeData.appName}.deeplink.page");
            }

            // URI Scheme of the deep link
            if (!string.IsNullOrEmpty(airbridgeData.androidURIScheme))
            {
                airbridgeAndroidManifest.SetUnityActivityIntentFilter(
                    false,
                    "android.intent.action.VIEW",
                    new[] { "android.intent.category.DEFAULT", "android.intent.category.BROWSABLE" },
                    airbridgeData.androidURIScheme
                );
            }

            // Custom Domain
            foreach (string customDomain in airbridgeData.customDomainList)
            {
                airbridgeAndroidManifest.SetUnityActivityAppLinksIntentFilter(customDomain);
            }

            airbridgeAndroidManifest.Save(destLibraryAndroidManifestPath);

            Debug.Log("[Airbridge] Updated Airbridge Android Manifest file.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Airbridge] Failed to update Airbridge Android Manifest file: {e.Message}");
        }
    }

    internal static void UpdateAndroidAirbridgeSettings()
    {
        string pluginPath = AirbridgeFileUtils.GetProjectAirbridgePluginPath(AirbridgeFileUtils.Platform.Android);
        if (pluginPath == null) return;
        string path = Path.Combine(pluginPath, "AirbridgeSettings.java");
        
        try
        {
            AirbridgeData airbridgeData = GetAirbridgeData();
            string content = 
                "package co.ab180.airbridge.unity;\n"
                + "\n"
                + "public class AirbridgeSettings {\n"
                + "\n"
                + "public static String appName = \"" + airbridgeData.appName + "\";\n"
                + "public static String appToken = \"" + airbridgeData.appToken + "\";\n"
                + "public static String sdkSignatureSecretID = \"" + airbridgeData.sdkSignatureSecretID + "\";\n"
                + "public static String sdkSignatureSecret = \"" + airbridgeData.sdkSignatureSecret + "\";\n"
                + "public static int logLevel = " + airbridgeData.logLevel + ";\n"
                + "public static String customDomain = \"" + string.Join(
                    separator: AirbridgeEditorConstant.CustomDomain.CustomDomainSeparator.ToString(),
                    values: airbridgeData.customDomainList
                ) + "\";\n"
                + "public static int sessionTimeoutSeconds = " + airbridgeData.sessionTimeoutSeconds + ";\n"
                + "public static boolean userInfoHashEnabled = " + airbridgeData.userInfoHashEnabled.ToString().ToLower() + ";\n"
                + "public static boolean locationCollectionEnabled = " + airbridgeData.locationCollectionEnabled.ToString().ToLower() + ";\n"
                + "public static boolean trackAirbridgeLinkOnly = " + airbridgeData.trackAirbridgeLinkOnly.ToString().ToLower() + ";\n"
                + "public static boolean autoStartTrackingEnabled = " + airbridgeData.autoStartTrackingEnabled.ToString().ToLower() + ";\n"
                + "public static boolean facebookDeferredAppLinkEnabled = " + airbridgeData.facebookDeferredAppLinkEnabled.ToString().ToLower() + ";\n"
                + "public static boolean trackInSessionLifeCycleEventEnabled = " + airbridgeData.trackInSessionLifeCycleEventEnabled.ToString().ToLower() + ";\n"
                + "public static boolean pauseEventTransmitOnBackgroundEnabled = " + airbridgeData.pauseEventTransmitOnBackgroundEnabled.ToString().ToLower() + ";\n"
                + "public static boolean clearEventBufferOnInitializeEnabled = " + airbridgeData.resetEventBufferEnabled.ToString().ToLower() + ";\n"
                + "public static boolean sdkEnabled = " + airbridgeData.sdkEnabled.ToString().ToLower() + ";\n"
                + "public static String appMarketIdentifier = \"" + airbridgeData.appMarketIdentifier + "\";\n"
                + "public static int eventBufferCountLimitInGibibyte = " + airbridgeData.eventMaximumBufferCount + ";\n"
                + "public static double eventBufferSizeLimitInGibibyte = " + airbridgeData.eventMaximumBufferSize + ";\n"
                + "public static long eventTransmitIntervalSeconds = " + airbridgeData.eventTransmitIntervalSeconds + ";\n"
                + "public static String facebookAppId = \"" + airbridgeData.facebookAppId + "\";\n"
                + "public static boolean isHandleAirbridgeDeeplinkOnly = " + airbridgeData.isHandleAirbridgeDeeplinkOnly.ToString().ToLower() + ";\n"
                + "public static String inAppPurchaseEnvironment = \"" + airbridgeData.inAppPurchaseEnvironment.ToLowerString() + "\";\n"
                + "public static boolean collectTCFDataEnabled = " + airbridgeData.collectTCFDataEnabled.ToString().ToLower() + ";\n"
                + "public static String trackingBlocklist = \"" + string.Join(
                    separator: AirbridgeEditorConstant.BlockList.TrackingBlocklistSeparator,
                    values: airbridgeData.trackingBlocklist
                ) + "\";\n"
                + "\n"
                + "}\n";
            
            AirbridgeFileUtils.PrepareFile(path);
            File.WriteAllText(path, content);
            Debug.Log("[Airbridge] Updated Android Airbridge settings file.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Airbridge] Failed to update Android Airbridge settings file: {e.Message}");
        }
    }

    internal static void UpdateIOSAppSetting()
    {
        string pluginPath = AirbridgeFileUtils.GetProjectAirbridgePluginPath(AirbridgeFileUtils.Platform.iOS);
        if (pluginPath == null) return;
        string path = Path.Combine(pluginPath, "AUAppSetting.h");
        
        try
        {
            AirbridgeData airbridgeData = GetAirbridgeData();
            string content = 
                "#ifndef AUAppSetting_h\n"
                + "#define AUAppSetting_h\n"
                + "\n"
                + "static NSString* appName = @\"" + airbridgeData.appName + "\";\n"
                + "static NSString* appToken = @\"" + airbridgeData.appToken + "\";\n"
                + "static NSString* sdkSignatureSecretID = @\"" + airbridgeData.sdkSignatureSecretID + "\";\n"
                + "static NSString* sdkSignatureSecret = @\"" + airbridgeData.sdkSignatureSecret + "\";\n"
                + "static NSUInteger logLevel = " + airbridgeData.logLevel + ";\n"
                + "static NSString* appScheme = @\"" + airbridgeData.iOSURIScheme + "\";\n"
                + "static NSString* customDomain = @\"" + string.Join(
                    separator: AirbridgeEditorConstant.CustomDomain.CustomDomainSeparator.ToString(),
                    values: airbridgeData.customDomainList
                ) + "\";\n"
                + "static NSInteger sessionTimeoutSeconds = " + airbridgeData.sessionTimeoutSeconds + ";\n"
                + "static BOOL autoStartTrackingEnabled = " + airbridgeData.autoStartTrackingEnabled.ToString().ToLower() + ";\n"
                + "static BOOL userInfoHashEnabled = " + airbridgeData.userInfoHashEnabled.ToString().ToLower() + ";\n"
                + "static BOOL trackAirbridgeLinkOnly = " + airbridgeData.trackAirbridgeLinkOnly.ToString().ToLower() + ";\n"
                + "static BOOL facebookDeferredAppLinkEnabled = " + airbridgeData.facebookDeferredAppLinkEnabled.ToString().ToLower() + ";\n"
                + "static NSInteger trackingAuthorizeTimeoutSeconds = " + airbridgeData.iOSTrackingAuthorizeTimeoutSeconds + ";\n"
                + "static BOOL trackInSessionLifeCycleEventEnabled = " + airbridgeData.trackInSessionLifeCycleEventEnabled.ToString().ToLower() + ";\n"
                + "static BOOL pauseEventTransmitOnBackgroundEnabled = " + airbridgeData.pauseEventTransmitOnBackgroundEnabled.ToString().ToLower() + ";\n"
                + "static BOOL clearEventBufferOnInitializeEnabled = " + airbridgeData.resetEventBufferEnabled.ToString().ToLower() + ";\n"
                + "static BOOL sdkEnabled = " + airbridgeData.sdkEnabled.ToString().ToLower() + ";\n"
                + "static NSInteger eventBufferCountLimitInGibibyte = " + airbridgeData.eventMaximumBufferCount + ";\n"
                + "static NSInteger eventBufferSizeLimitInGibibyte = " + airbridgeData.eventMaximumBufferSize + ";\n"
                + "static NSInteger eventTransmitIntervalSeconds = " + airbridgeData.eventTransmitIntervalSeconds + ";\n"
                + "static BOOL isHandleAirbridgeDeeplinkOnly = " + airbridgeData.isHandleAirbridgeDeeplinkOnly.ToString().ToLower() + ";\n"
                + "static NSString* inAppPurchaseEnvironment = @\"" + airbridgeData.inAppPurchaseEnvironment.ToLowerString() + "\";\n"
                + "static BOOL collectTCFDataEnabled = " + airbridgeData.collectTCFDataEnabled.ToString().ToLower() + ";\n"
                + "static NSString* trackingBlocklist = @\"" + string.Join(
                    separator: AirbridgeEditorConstant.BlockList.TrackingBlocklistSeparator,
                    values: airbridgeData.trackingBlocklist
                ) + "\";\n"
                + "static BOOL calculateSKAdNetworkByServer = " + airbridgeData.calculateSKAdNetworkByServer.ToString().ToLower() + ";\n"
                + "\n"
                + "#endif\n";

            AirbridgeFileUtils.PrepareFile(path);
            File.WriteAllText(path, content);
            Debug.Log("[Airbridge] Updated iOS Airbridge settings file.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Airbridge] Failed to update iOS Airbridge settings file: {e.Message}");
        }
    }
}

#endif
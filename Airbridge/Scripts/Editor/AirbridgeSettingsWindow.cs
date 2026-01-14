#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Assets.Airbridge.Scripts.Editor.Constant;

// ReSharper disable once CheckNamespace
internal class AirbridgeSettingsWindow : EditorWindow
{
    private const string TemplatePrefix = ".template";
    private const string MetaPrefix = ".meta";

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
        string pluginPath = AirbridgeUtils.GetPluginPath(AirbridgeUtils.Platform.Android);
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
                AirbridgeUtils.GetUnityPackageAssetsPath(),
                "Plugins", "Airbridge", "Android",
                "java/co/ab180/airbridge/unity",
                entry.GetActivityFileName() + TemplatePrefix
            );
            string activityPath = Path.Combine(pluginPath, entry.GetActivityFileName());

            File.Copy(defaultActivityPath, activityPath);
            Debug.LogFormat("Copied default Android Airbridge Activity file from \'{0}\'", defaultActivityPath);
        }
        catch (Exception exception)
        {
            Debug.LogErrorFormat("Something broken while updating Android Airbridge Activity file : {0}", exception);
        }
    }

    private static void UpdateAndroidManifest(AirbridgeAndroidApplicationEntry entry)
    {
        try
        {
            string androidManifestDirPath = Path.Combine(Application.dataPath, "Plugins", "Android");
            string androidManifestPath = Path.Combine(androidManifestDirPath, "AndroidManifest.xml");

            // AirbridgeAndroidApplicationEntry가 None이 아닌 경우,
            // AndroidManifest.xml이 존재하지 않으면 기본 AndroidManifest.xml 템플릿을 추가한다.
            if (!entry.IsNone())
            {
                string defaultManifestPath = Path.Combine(
                    AirbridgeUtils.GetUnityPackageAssetsPath(),
                    "Plugins", "Airbridge", "Android",
                    entry.GetManifestFileName()
                );

                if (!File.Exists(androidManifestPath))
                {
                    Debug.Log("Couldn't find any Android App Manifest file");

                    if (!Directory.Exists(androidManifestDirPath))
                    {
                        Directory.CreateDirectory(androidManifestDirPath);
                        Debug.LogFormat("Create Android App Manifest directory : {0}", androidManifestDirPath);
                    }

                    File.Copy(defaultManifestPath, androidManifestPath);
                    Debug.LogFormat("Copied default Android App Manifest file from \'{0}\'", defaultManifestPath);
                }
            }

            AndroidManifest androidManifest = new AndroidManifest(androidManifestPath);
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

            androidManifest.Save(androidManifestPath);

            // Airbridge Setting의 다음 값을 참고하여 AndroidManifest.xml을 수정한다.
            //  - appName
            //  - androidURIScheme
            //  - customDomain
            //
            // Default, Dev, Prod 환경별로 서로 다른 값을 가지므로, 별도의 AndroidManifest.xml 파일로 관리한다.
            AirbridgeData airbridgeData = GetAirbridgeData();

            string airbridgeAndroidManifestPath = Path.Combine(AirbridgeUtils.GetUnityPackageAssetsPath(),
                "Plugins", "Android", "Airbridge.androidlib", "AndroidManifest.xml");

            if (!File.Exists(airbridgeAndroidManifestPath))
            {
                Debug.Log("Couldn't find Airbridge App Manifest file");

                string airbridgeAndroidManifestTemplatePath = airbridgeAndroidManifestPath + TemplatePrefix;
                File.Copy(airbridgeAndroidManifestTemplatePath, airbridgeAndroidManifestPath);
                File.Copy(airbridgeAndroidManifestTemplatePath + MetaPrefix, airbridgeAndroidManifestPath + MetaPrefix);

                Debug.LogFormat("Copied Airbridge Android App Manifest file from \'{0}\'",
                    airbridgeAndroidManifestTemplatePath);
            }

            AndroidManifest airbridgeAndroidManifest = new AndroidManifest(airbridgeAndroidManifestPath);
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

            airbridgeAndroidManifest.Save(airbridgeAndroidManifestPath);

            Debug.Log("Updated Android App Manifest (AndroidManifest.xml)");
        }
        catch (Exception exception)
        {
            Debug.LogErrorFormat("Something broken while updating Android App Manifest file : {0}", exception);
        }
    }

    internal static void UpdateAndroidAirbridgeSettings()
    {
        string pluginPath = AirbridgeUtils.GetPluginPath(AirbridgeUtils.Platform.Android);
        if (pluginPath == null) return;

        try
        {
            string settingsPath = Path.Combine(pluginPath, "AirbridgeSettings.java");

            if (!File.Exists(settingsPath))
            {
                File.Create(settingsPath).Dispose();
            }

            AirbridgeData airbridgeData = GetAirbridgeData();

            string content = $$"""
                               package co.ab180.airbridge.unity;

                               public class AirbridgeSettings {
                               public static String appName = "{{airbridgeData.appName}}";
                               public static String appToken = "{{airbridgeData.appToken}}";
                               public static String sdkSignatureSecretID = "{{airbridgeData.sdkSignatureSecretID}}";
                               public static String sdkSignatureSecret = "{{airbridgeData.sdkSignatureSecret}}";
                               public static int logLevel = {{airbridgeData.logLevel}};
                               public static String customDomain = "{{string.Join(AirbridgeEditorConstant.CustomDomain.CustomDomainSeparator.ToString(), airbridgeData.customDomainList)}}";
                               public static int sessionTimeoutSeconds = {{airbridgeData.sessionTimeoutSeconds}};
                               public static boolean userInfoHashEnabled = {{airbridgeData.userInfoHashEnabled.ToString().ToLower()}};
                               public static boolean locationCollectionEnabled = {{airbridgeData.locationCollectionEnabled.ToString().ToLower()}};
                               public static boolean trackAirbridgeLinkOnly = {{airbridgeData.trackAirbridgeLinkOnly.ToString().ToLower()}};
                               public static boolean autoStartTrackingEnabled = {{airbridgeData.autoStartTrackingEnabled.ToString().ToLower()}};
                               public static boolean facebookDeferredAppLinkEnabled = {{airbridgeData.facebookDeferredAppLinkEnabled.ToString().ToLower()}};
                               public static boolean trackInSessionLifeCycleEventEnabled = {{airbridgeData.trackInSessionLifeCycleEventEnabled.ToString().ToLower()}};
                               public static boolean pauseEventTransmitOnBackgroundEnabled = {{airbridgeData.pauseEventTransmitOnBackgroundEnabled.ToString().ToLower()}};
                               public static boolean clearEventBufferOnInitializeEnabled = {{airbridgeData.resetEventBufferEnabled.ToString().ToLower()}};
                               public static boolean sdkEnabled = {{airbridgeData.sdkEnabled.ToString().ToLower()}};
                               public static String appMarketIdentifier = "{{airbridgeData.appMarketIdentifier}}";
                               public static int eventBufferCountLimitInGibibyte = {{airbridgeData.eventMaximumBufferCount}};
                               public static double eventBufferSizeLimitInGibibyte = {{airbridgeData.eventMaximumBufferSize}};
                               public static long eventTransmitIntervalSeconds = {{airbridgeData.eventTransmitIntervalSeconds}};
                               public static String facebookAppId = "{{airbridgeData.facebookAppId}}";
                               public static boolean isHandleAirbridgeDeeplinkOnly = {{airbridgeData.isHandleAirbridgeDeeplinkOnly.ToString().ToLower()}};
                               public static String inAppPurchaseEnvironment = "{{airbridgeData.inAppPurchaseEnvironment.ToLowerString()}}";
                               public static boolean collectTCFDataEnabled = {{airbridgeData.collectTCFDataEnabled.ToString().ToLower()}};
                               public static String trackingBlocklist = "{{string.Join(AirbridgeEditorConstant.BlockList.TrackingBlocklistSeparator, airbridgeData.trackingBlocklist)}}";
                               }
                               """;

            File.WriteAllText(settingsPath, content);

            Debug.Log("Updated Android Airbridge settings (AirbridgeSettings.java)");
        }
        catch (Exception exception)
        {
            Debug.LogErrorFormat("Something broken while updating Android Airbridge settings file : {0}", exception);
        }
    }

    internal static void UpdateIOSAppSetting()
    {
        string pluginPath = AirbridgeUtils.GetPluginPath(AirbridgeUtils.Platform.iOS);
        if (pluginPath == null) return;

        try
        {
            string path = Path.Combine(pluginPath, "AUAppSetting.h");
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }

            AirbridgeData airbridgeData = GetAirbridgeData();

            string content = $$"""
                               #ifndef AUAppSetting_h
                               #define AUAppSetting_h

                               static NSString* appName = @"{{airbridgeData.appName}}";
                               static NSString* appToken = @"{{airbridgeData.appToken}}";
                               static NSString* sdkSignatureSecretID = "{{airbridgeData.sdkSignatureSecretID}}";
                               static NSString* sdkSignatureSecret = "{{airbridgeData.sdkSignatureSecret}}";
                               static NSUInteger logLevel = {{airbridgeData.logLevel}};
                               static NSString* appScheme = @"{{airbridgeData.iOSURIScheme}}";
                               static NSString* customDomain = @"{{string.Join(AirbridgeEditorConstant.CustomDomain.CustomDomainSeparator.ToString(), airbridgeData.customDomainList)}}";
                               static NSInteger sessionTimeoutSeconds = {{airbridgeData.sessionTimeoutSeconds}};
                               static BOOL userInfoHashEnabled = {{airbridgeData.userInfoHashEnabled.ToString().ToLower()}};
                               static BOOL locationCollectionEnabled = {{airbridgeData.locationCollectionEnabled.ToString().ToLower()}};
                               static BOOL trackAirbridgeLinkOnly = {{airbridgeData.trackAirbridgeLinkOnly.ToString().ToLower()}};
                               static BOOL autoStartTrackingEnabled = {{airbridgeData.autoStartTrackingEnabled.ToString().ToLower()}};
                               static BOOL facebookDeferredAppLinkEnabled = {{airbridgeData.facebookDeferredAppLinkEnabled.ToString().ToLower()}};
                               static NSInteger trackingAuthorizeTimeoutSeconds = {{airbridgeData.iOSTrackingAuthorizeTimeoutSeconds}};
                               static BOOL trackInSessionLifeCycleEventEnabled = {{airbridgeData.trackInSessionLifeCycleEventEnabled.ToString().ToLower()}};
                               static BOOL pauseEventTransmitOnBackgroundEnabled = {{airbridgeData.pauseEventTransmitOnBackgroundEnabled.ToString().ToLower()}};
                               static BOOL clearEventBufferOnInitializeEnabled = {{airbridgeData.resetEventBufferEnabled.ToString().ToLower()}};
                               static BOOL sdkEnabled = {{airbridgeData.sdkEnabled.ToString().ToLower()}};
                               static NSString* appMarketIdentifier = @"{{airbridgeData.appMarketIdentifier}}";
                               static NSInteger eventBufferCountLimitInGibibyte = {{airbridgeData.eventMaximumBufferCount}};
                               static NSInteger eventBufferSizeLimitInGibibyte = {{airbridgeData.eventMaximumBufferSize}};
                               static NSInteger eventTransmitIntervalSeconds = {{airbridgeData.eventTransmitIntervalSeconds}};
                               static NSString* facebookAppId = @"{{airbridgeData.facebookAppId}}";
                               static BOOL isHandleAirbridgeDeeplinkOnly = {{airbridgeData.isHandleAirbridgeDeeplinkOnly.ToString().ToLower()}};
                               static NSString* inAppPurchaseEnvironment = @"{{airbridgeData.inAppPurchaseEnvironment.ToLowerString()}}";
                               static BOOL collectTCFDataEnabled = {{airbridgeData.collectTCFDataEnabled.ToString().ToLower()}};
                               static NSString* trackingBlocklist = @"{{string.Join(AirbridgeEditorConstant.BlockList.TrackingBlocklistSeparator, airbridgeData.trackingBlocklist)}}";
                               static BOOL calculateSKAdNetworkByServer = {{airbridgeData.calculateSKAdNetworkByServer.ToString().ToLower()}};

                               #endif
                               """;

            File.WriteAllText(path, content);

            Debug.Log("Updated iOS Airbridge settings (AUAppSetting.h)");
        }
        catch (Exception exception)
        {
            Debug.LogErrorFormat("Something broken while updating iOS Airbridge settings file : {0}", exception);
        }
    }
}

#endif

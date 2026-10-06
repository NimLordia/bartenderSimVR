using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace BartenderSimVR.Editor
{
    /// <summary>Repeatable setup for the standalone Quest prototype and optional Windows Link iteration.</summary>
    public static class QuestProjectConfiguration
    {
        public const string ApplicationId = "com.nimlordia.bartendersimvr";
        private const string SettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        private const string MobilePipelinePath = "Assets/Settings/Mobile_RPAsset.asset";

        public static void Configure()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before configuring the Quest project.");

            ConfigureAndroidPlayer();
            ConfigureMobileRendering();
            var settings = GetOrCreateGeneralSettings();
            ConfigureOpenXR(settings, BuildTargetGroup.Android);
            ConfigureOpenXR(settings, BuildTargetGroup.Standalone);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("Quest setup saved: Android ARM64/IL2CPP, Vulkan, OpenXR multiview, Meta Quest support, and Oculus Touch input. Windows OpenXR is available for Link iteration.");
        }

        private static void ConfigureAndroidPlayer()
        {
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // Quest 2 uses modern Horizon OS; API 32 keeps the prototype on the Android 12 baseline.
            if ((int)PlayerSettings.Android.minSdkVersion < 32)
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.runInBackground = true;

            // Unity has no public setter for Active Input Handling. Preserve the new-input setting
            // already selected by the template; update older templates without enabling legacy input.
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset"));
            var inputHandling = playerSettings.FindProperty("activeInputHandler");
            if (inputHandling == null)
                throw new InvalidOperationException("Cannot locate Unity's Active Input Handling setting.");
            if (inputHandling.intValue != 1)
            {
                inputHandling.intValue = 1;
                playerSettings.ApplyModifiedPropertiesWithoutUndo();
                Debug.LogWarning("Active Input Handling changed to Input System Package. Restart the Unity editor before testing controller input.");
            }
        }

        private static void ConfigureMobileRendering()
        {
            var mobilePipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobilePipelinePath);
            if (mobilePipeline == null)
                throw new InvalidOperationException($"Expected the URP template's mobile pipeline at {MobilePipelinePath}.");

            mobilePipeline.supportsHDR = false;
            mobilePipeline.msaaSampleCount = 4;
            mobilePipeline.renderScale = 1f;
            mobilePipeline.shadowDistance = 10f;
            // These URP options have internal setters in 17.6; use their serialized settings.
            var serializedPipeline = new SerializedObject(mobilePipeline);
            serializedPipeline.FindProperty("m_MainLightShadowsSupported").boolValue = false;
            serializedPipeline.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            serializedPipeline.FindProperty("m_AnyShadowsSupported").boolValue = false;
            serializedPipeline.FindProperty("m_SoftShadowsSupported").boolValue = false;
            serializedPipeline.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(mobilePipeline);

            // The URP template already maps Android to its Mobile quality level. Validate that
            // its pipeline still exists instead of replacing the user's desktop quality settings.
            var mobileQuality = Array.IndexOf(QualitySettings.names, "Mobile");
            if (mobileQuality < 0 || QualitySettings.GetRenderPipelineAssetAt(mobileQuality) != mobilePipeline)
                throw new InvalidOperationException("The Mobile quality level must use Mobile_RPAsset for the Quest prototype.");
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreateGeneralSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget settings) && settings != null)
                return settings;

            var existing = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget");
            if (existing.Length > 0)
                settings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(existing[0]));
            else
            {
                if (!AssetDatabase.IsValidFolder("Assets/XR"))
                    AssetDatabase.CreateFolder("Assets", "XR");
                settings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, settings, true);
            return settings;
        }

        private static void ConfigureOpenXR(XRGeneralSettingsPerBuildTarget settings, BuildTargetGroup target)
        {
            if (!settings.HasSettingsForBuildTarget(target))
                settings.CreateDefaultSettingsForBuildTarget(target);
            if (!settings.HasManagerSettingsForBuildTarget(target))
                settings.CreateDefaultManagerSettingsForBuildTarget(target);

            var general = settings.SettingsForBuildTarget(target);
            general.InitManagerOnStart = true;
            EditorUtility.SetDirty(general);
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, typeof(OpenXRLoader).FullName, target))
                throw new InvalidOperationException($"Could not assign the OpenXR loader to {target}.");

            // This public helper creates the package settings and available feature subassets
            // before we enable them, avoiding hand-authored YAML or duplicate feature assets.
            FeatureHelpers.RefreshFeatures(target);
            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
            if (openXR == null)
                throw new InvalidOperationException($"OpenXR settings were not created for {target}.");
            openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
            openXR.depthSubmissionMode = OpenXRSettings.DepthSubmissionMode.None;
            EnableFeature<OculusTouchControllerProfile>(openXR);

            if (target == BuildTargetGroup.Android)
            {
                openXR.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeInputPolling;
                var quest = EnableFeature<MetaQuestFeature>(openXR);
                var serializedQuest = new SerializedObject(quest);
                var devices = serializedQuest.FindProperty("targetDevices");
                for (var index = 0; index < devices.arraySize; index++)
                {
                    var device = devices.GetArrayElementAtIndex(index);
                    var manifestName = device.FindPropertyRelative("manifestName").stringValue;
                    // Quest 2 is the confirmed baseline. Keep newer device defaults, and exclude Quest 1.
                    if (manifestName == "quest" || manifestName == "quest2")
                        device.FindPropertyRelative("enabled").boolValue = manifestName == "quest2";
                }
                serializedQuest.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(quest);
            }

            EditorUtility.SetDirty(openXR);
        }

        private static T EnableFeature<T>(OpenXRSettings settings) where T : OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null)
                throw new InvalidOperationException($"The installed OpenXR package does not expose {typeof(T).Name}.");
            feature.enabled = true;
            EditorUtility.SetDirty(feature);
            return feature;
        }

        /// <summary>Fail early on required setup errors and print the package's remaining recommendations.</summary>
        public static void Validate()
        {
            var errors = new List<string>();
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != ApplicationId)
                errors.Add("Android application ID is incorrect.");
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
                errors.Add("Android must use IL2CPP.");
            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
                errors.Add("Android must target ARM64 only.");
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.GameActivity)
                errors.Add("Android must use GameActivity.");
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android) || !PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.Vulkan }))
                errors.Add("Android must explicitly use Vulkan.");

            foreach (var target in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var general = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(target);
                if (general == null || !general.InitManagerOnStart || general.Manager == null || !general.Manager.activeLoaders.Any(loader => loader is OpenXRLoader))
                    errors.Add($"{target}: OpenXR must initialize on startup.");
                var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(target);
                if (openXR == null || openXR.renderMode != OpenXRSettings.RenderMode.SinglePassInstanced || openXR.GetFeature<OculusTouchControllerProfile>()?.enabled != true)
                    errors.Add($"{target}: OpenXR multiview and Oculus Touch profile are required.");
                if (target == BuildTargetGroup.Android && openXR?.GetFeature<MetaQuestFeature>()?.enabled != true)
                    errors.Add("Android: Meta Quest Support must be enabled.");
            }

            // Package checks depend on the active build target; call after root's Android switch.
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                var issues = new List<OpenXRFeature.ValidationRule>();
                OpenXRProjectValidation.GetCurrentValidationIssues(issues, BuildTargetGroup.Android);
                foreach (var issue in issues)
                {
                    if (issue.error)
                        errors.Add(issue.message);
                    else
                        Debug.LogWarning("OpenXR recommendation: " + issue.message);
                }
            }

            if (errors.Count > 0)
                throw new BuildFailedException("Quest configuration validation failed:\n" + string.Join("\n", errors));
            Debug.Log("Quest configuration validation passed. Device tracking, frame timing, and comfort still require headset testing.");
        }
    }
}

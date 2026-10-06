using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

namespace BartenderSimVR.Editor
{
    /// <summary>Explicit editor commands; importing scripts never overwrites a scene.</summary>
    public static class PrototypeSetup
    {
        public const string ScenePath = "Assets/Scenes/BartenderPrototype.unity";

        [MenuItem("Bartender VR/Set Up First Prototype")]
        public static void Setup()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            QuestProjectConfiguration.Configure();
            PrototypeSceneBuilder.CreateScene();
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("Bartender VR: prototype setup complete. Open BartenderPrototype and press Play, or build the Quest APK.");
        }

        [MenuItem("Bartender VR/Validate Setup")]
        public static void Validate()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                throw new OperationCanceledException("Validation cancelled to preserve the open scene.");
            QuestProjectConfiguration.Validate();
            if (!File.Exists(Path.Combine(ProjectRoot, ScenePath)))
                throw new InvalidOperationException("Run Bartender VR/Set Up First Prototype before validation.");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var origins = roots.SelectMany(root => root.GetComponentsInChildren<XROrigin>(true)).ToArray();
            var glasses = roots.SelectMany(root => root.GetComponentsInChildren<XRGrabInteractable>(true)).ToArray();
            Require(origins.Length == 1 && origins[0].Camera != null, "One tracked XR Origin with a camera is required.");
            Require(glasses.Length == 1, "The first prototype must contain one grabbable glass.");
            Require(glasses[0].GetComponent<Rigidbody>() != null, "The glass needs a rigidbody.");
            Require(glasses[0].GetComponentsInChildren<Collider>().Length > 0, "The glass needs colliders.");
            Require(EditorBuildSettings.scenes.Count(item => item.enabled) == 1 &&
                    EditorBuildSettings.scenes.Any(item => item.enabled && item.path == ScenePath),
                    "BartenderPrototype must be the enabled build scene.");
            Require(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP,
                    "Quest builds require IL2CPP.");
            Require(PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64,
                    "The prototype targets ARM64.");

            var reportPath = Path.Combine(ProjectRoot, "Builds", "setup-validation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, JsonUtility.ToJson(new SetupReport
            {
                editorVersion = Application.unityVersion,
                scene = ScenePath,
                xrOrigins = origins.Length,
                grabbableGlasses = glasses.Length,
                androidArchitecture = PlayerSettings.Android.targetArchitectures.ToString(),
                packageId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android),
                checkedAtUtc = DateTime.UtcNow.ToString("O")
            }, true));
            Debug.Log("Bartender VR: setup validation passed. " + reportPath);
        }

        [MenuItem("Bartender VR/Build Quest APK")]
        public static void BuildQuest()
        {
            QuestProjectConfiguration.Configure();
            Validate();
            var outputPath = Environment.GetEnvironmentVariable("BSVR_BUILD_OUTPUT");
            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.Combine(ProjectRoot, "Builds", "Android", "BartenderSimVR.apk");
            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });

            var summary = report.summary;
            File.WriteAllText(Path.ChangeExtension(outputPath, ".build-report.json"), JsonUtility.ToJson(new BuildReportData
            {
                result = summary.result.ToString(),
                outputPath = summary.outputPath,
                errors = summary.totalErrors,
                warnings = summary.totalWarnings,
                bytes = summary.totalSize,
                durationSeconds = summary.totalTime.TotalSeconds,
                checkedAtUtc = DateTime.UtcNow.ToString("O")
            }, true));
            if (summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Quest APK build " + summary.result + " with " + summary.totalErrors + " errors.");

            Debug.Log("Bartender VR: Quest APK built at " + outputPath);
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        [Serializable]
        private sealed class SetupReport
        {
            public string editorVersion, scene, androidArchitecture, packageId, checkedAtUtc;
            public int xrOrigins, grabbableGlasses;
        }

        [Serializable]
        private sealed class BuildReportData
        {
            public string result, outputPath, checkedAtUtc;
            public int errors, warnings;
            public ulong bytes;
            public double durationSeconds;
        }
    }
}

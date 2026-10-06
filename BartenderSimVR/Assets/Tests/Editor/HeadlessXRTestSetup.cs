using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Management;

namespace BartenderSimVR.Tests.Editor
{
    /// <summary>Disables device startup only while the isolated interaction tests run.</summary>
    public sealed class HeadlessXRTestSetup : IPrebuildSetup, IPostBuildCleanup
    {
        const string SnapshotKey = "BartenderSimVR.PrototypeTests.XRStartupSnapshot";

        public void Setup()
        {
            // SessionState survives the PlayMode domain reload. Restore a previous interrupted
            // run first so repeated setup can never save a temporary false flag as the original.
            Cleanup();
            var entries = new List<Entry>();
            foreach (var target in new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android })
            {
                var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(target);
                if (settings == null)
                    continue;
                entries.Add(new Entry
                {
                    objectId = GlobalObjectId.GetGlobalObjectIdSlow(settings).ToString(),
                    initializeOnStart = settings.InitManagerOnStart
                });
            }

            SessionState.SetString(SnapshotKey, JsonUtility.ToJson(new Snapshot { entries = entries.ToArray() }));
            try
            {
                foreach (var entry in entries)
                {
                    var settings = Resolve(entry);
                    settings.InitManagerOnStart = false;
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssetIfDirty(settings);
                }
            }
            catch
            {
                Cleanup();
                throw;
            }
            Debug.Log("Interaction tests: automatic XR device startup temporarily disabled.");
        }

        public void Cleanup()
        {
            var json = SessionState.GetString(SnapshotKey, string.Empty);
            if (string.IsNullOrEmpty(json))
                return;

            var snapshot = JsonUtility.FromJson<Snapshot>(json);
            foreach (var entry in snapshot.entries)
            {
                var settings = Resolve(entry);
                settings.InitManagerOnStart = entry.initializeOnStart;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssetIfDirty(settings);
            }
            SessionState.EraseString(SnapshotKey);
            Debug.Log("Interaction tests: original automatic XR device startup settings restored.");
        }

        static XRGeneralSettings Resolve(Entry entry)
        {
            if (!GlobalObjectId.TryParse(entry.objectId, out var objectId) ||
                !(GlobalObjectId.GlobalObjectIdentifierToObjectSlow(objectId) is XRGeneralSettings settings))
                throw new InvalidOperationException("Cannot restore the XR settings saved before interaction tests: " + entry.objectId);
            return settings;
        }

        [Serializable]
        sealed class Snapshot
        {
            public Entry[] entries;
        }

        [Serializable]
        sealed class Entry
        {
            public string objectId;
            public bool initializeOnStart;
        }
    }
}

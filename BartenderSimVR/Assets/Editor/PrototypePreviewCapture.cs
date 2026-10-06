using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BartenderSimVR.Editor
{
    /// <summary>A desktop composition check of the saved scene, without starting Play mode or XR.</summary>
    public static class PrototypePreviewCapture
    {
        [MenuItem("Bartender VR/Capture Prototype Preview")]
        public static void Capture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play mode before capturing the saved prototype.");
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                throw new InvalidOperationException("Preview capture needs graphics. Launch batch Unity without -nographics.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (!File.Exists(Path.Combine(projectRoot, PrototypeSceneBuilder.ScenePath)))
                throw new InvalidOperationException("Create the prototype scene before capturing its preview.");
            var scene = EditorSceneManager.OpenScene(PrototypeSceneBuilder.ScenePath, OpenSceneMode.Single);
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset))
                throw new InvalidOperationException("The prototype preview requires its configured Universal Render Pipeline.");

            GameObject cameraObject = null;
            RenderTexture target = null;
            Texture2D pixels = null;
            var previousTarget = RenderTexture.active;
            try
            {
                // HideAndDontSave keeps the temporary view out of scene serialization.
                cameraObject = new GameObject("Prototype Preview Camera") { hideFlags = HideFlags.HideAndDontSave };
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                camera.transform.position = new Vector3(0f, 1.65f, -0.70f);
                camera.transform.LookAt(new Vector3(0f, 1.50f, 1f));
                camera.fieldOfView = 75f;
                camera.aspect = 16f / 9f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 30f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.13f, 0.19f, 0.22f);
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.stereoTargetEye = StereoTargetEyeMask.None;
                var additionalData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
                additionalData.renderType = CameraRenderType.Base;
                additionalData.renderPostProcessing = false;
                additionalData.allowXRRendering = false;
                additionalData.requiresDepthOption = CameraOverrideOption.Off;
                additionalData.requiresColorOption = CameraOverrideOption.Off;

                var textMeshes = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<TextMesh>(true)).ToArray();
                foreach (var text in textMeshes)
                    if (text.font != null)
                        text.font.RequestCharactersInTexture(text.text, text.fontSize, text.fontStyle);

                target = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                {
                    name = "Prototype Preview Target",
                    hideFlags = HideFlags.HideAndDontSave,
                    antiAliasing = 1
                };
                if (!target.Create())
                    throw new InvalidOperationException("Could not create the preview render texture.");
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                if (!RenderPipeline.SupportsRenderRequest(camera, request))
                    throw new InvalidOperationException("The active render pipeline does not support a URP SingleCameraRequest.");
                RenderPipeline.SubmitRenderRequest(camera, request);

                RenderTexture.active = target;
                pixels = new Texture2D(target.width, target.height, TextureFormat.RGB24, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0, false);
                pixels.Apply(false, false);
                var output = Path.Combine(projectRoot, "Builds", "prototype-preview.png");
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, pixels.EncodeToPNG());
                LogInstructionBounds(scene.GetRootGameObjects(), textMeshes);
                Debug.Log("Saved desktop prototype preview: " + output + ". This checks composition only; headset readability and comfort are unverified.");
            }
            finally
            {
                RenderTexture.active = previousTarget;
                if (pixels != null)
                    UnityEngine.Object.DestroyImmediate(pixels);
                if (target != null)
                {
                    target.Release();
                    UnityEngine.Object.DestroyImmediate(target);
                }
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        private static void LogInstructionBounds(GameObject[] roots, TextMesh[] textMeshes)
        {
            var board = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
                .FirstOrDefault(renderer => renderer.name == "Instructions Board");
            if (board == null)
            {
                Debug.LogWarning("Preview bounds check: Instructions Board was not found.");
                return;
            }
            Debug.Log("Instructions Board MeshRenderer world bounds: " + board.bounds.ToString("F3"));
            foreach (var text in textMeshes)
            {
                var renderer = text.GetComponent<MeshRenderer>();
                if (renderer == null)
                    continue;
                var bounds = renderer.bounds;
                Debug.Log(text.name + " TextMesh MeshRenderer world bounds: " + bounds.ToString("F3"));
                if (text.name != "Headset Instructions")
                    continue;
                // The generated board and TextMesh share an unrotated XY plane; ignore the
                // intended text offset in Z while detecting overflow past the sign edges.
                var boardBounds = board.bounds;
                const float tolerance = 0.005f;
                var fits = bounds.min.x >= boardBounds.min.x - tolerance && bounds.max.x <= boardBounds.max.x + tolerance &&
                    bounds.min.y >= boardBounds.min.y - tolerance && bounds.max.y <= boardBounds.max.y + tolerance;
                if (bounds.size.sqrMagnitude < 0.000001f)
                    Debug.LogWarning("Instruction text bounds are empty; inspect the PNG before accepting the preview.");
                else if (!fits)
                    Debug.LogWarning("Instruction text extends beyond the board in X or Y; resize the text or board before headset testing.");
                else
                    Debug.Log("Instruction text fits inside the board's XY bounds.");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BartenderSimVR.Prototype;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace BartenderSimVR.Editor
{
    /// <summary>Explicit, repeatable generation of the first headset interaction scene.</summary>
    public static class PrototypeSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/BartenderPrototype.unity";
        public const string InputPath = "Assets/Prototype/PrototypeXR.inputactions";

        [MenuItem("Bartender VR/Create First Grab Scene")]
        public static void CreateScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Prototype");
            EnsureFolder("Assets/Prototype/Materials");
            var actions = LoadOrCreateActions();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.fog = false;

            var floor = MaterialAsset("Floor", new Color(0.16f, 0.22f, 0.25f));
            var counter = MaterialAsset("Counter", new Color(0.27f, 0.36f, 0.38f));
            var top = MaterialAsset("Counter Top", new Color(0.60f, 0.66f, 0.59f));
            var glassIdle = MaterialAsset("Glass Idle", new Color(0.38f, 0.78f, 0.86f));
            var glassHover = MaterialAsset("Glass Hover", new Color(0.95f, 0.76f, 0.35f));
            var glassHeld = MaterialAsset("Glass Held", new Color(0.52f, 0.88f, 0.65f));
            var dark = MaterialAsset("Sign", new Color(0.11f, 0.16f, 0.19f));
            var marker = MaterialAsset("Return Pad", new Color(0.82f, 0.83f, 0.63f));
            var leftMaterial = MaterialAsset("Left Controller", new Color(0.86f, 0.70f, 0.46f));
            var rightMaterial = MaterialAsset("Right Controller", new Color(0.40f, 0.72f, 0.81f));

            var environment = new GameObject("Bar Blockout").transform;
            Box("Floor", environment, new Vector3(0f, -0.05f, 0f), new Vector3(8f, 0.1f, 8f), floor);
            CounterSection("Front", environment, new Vector3(0f, 0f, 0.92f), new Vector3(2.6f, 1f, 0.60f), counter, top);
            CounterSection("Left", environment, new Vector3(-1.15f, 0f, -0.36f), new Vector3(0.30f, 1f, 1.96f), counter, top);
            CounterSection("Right", environment, new Vector3(1.15f, 0f, -0.36f), new Vector3(0.30f, 1f, 1.96f), counter, top);

            var manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            CreateRig(actions, manager, leftMaterial, rightMaterial);

            var returnPoint = new GameObject("Glass Return Point").transform;
            returnPoint.position = new Vector3(0.24f, 1.103f, 0.70f);
            Box("Return Pad", environment, new Vector3(0.24f, 1.003f, 0.70f), new Vector3(0.18f, 0.006f, 0.18f), marker);
            var glass = CreateGlass(returnPoint, manager, glassIdle, glassHover, glassHeld);
            glass.GetComponent<PrototypeGlassRecovery>().ResetAction = ActionReference(actions, "Prototype/Reset Glass");

            Box("Instructions Board", environment, new Vector3(0f, 1.73f, 1.28f), new Vector3(1.65f, 0.66f, 0.04f), dark);
            var sign = new GameObject("Headset Instructions").AddComponent<TextMesh>();
            sign.transform.SetParent(environment, false);
            sign.transform.position = new Vector3(0f, 1.73f, 1.25f);
            sign.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sign.GetComponent<MeshRenderer>().sharedMaterial = sign.font.material;
            sign.text = "BARTENDER VR\nReach into the blue glass.\nHold GRIP to pick it up; release to drop.\nGold = in reach. Green = held.\nRelease, then B / Y to return it.\nFloor drops return automatically.";
            sign.anchor = TextAnchor.MiddleCenter;
            sign.alignment = TextAlignment.Center;
            sign.fontSize = 52;
            sign.characterSize = 0.013f;
            sign.lineSpacing = 1.1f;
            sign.color = new Color(0.90f, 0.93f, 0.86f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = glass;
            Debug.Log("Created " + ScenePath + ". Grip grabs; B/Y (R in editor) returns an unheld glass. SampleScene was preserved.");
        }

        static void CreateRig(InputActionAsset actions, XRInteractionManager manager, Material left, Material right)
        {
            var rig = new GameObject("XR Origin (Floor)");
            var origin = rig.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Floor Offset");
            offset.transform.SetParent(rig.transform, false);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.13f, 0.19f, 0.22f);
            cameraObject.AddComponent<AudioListener>();
            AddTracking(cameraObject, actions, "Head");
            origin.Origin = rig;
            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset;
            origin.CameraYOffset = 1.6f;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            rig.AddComponent<InputActionManager>().actionAssets = new List<InputActionAsset> { actions };
            CreateController("Left", offset.transform, actions, manager, left);
            CreateController("Right", offset.transform, actions, manager, right);
        }

        static void CreateController(string hand, Transform parent, InputActionAsset actions, XRInteractionManager manager, Material material)
        {
            var controller = new GameObject(hand + " Direct Controller");
            controller.transform.SetParent(parent, false);
            controller.transform.localPosition = new Vector3(hand == "Left" ? -0.25f : 0.25f, 1.2f, 0.35f);
            AddTracking(controller, actions, hand);
            var trigger = controller.AddComponent<SphereCollider>();
            trigger.radius = 0.07f;
            trigger.isTrigger = true;
            // XRI 3.6 supports per-frame sphere overlap without a Rigidbody for faster close contact.
            var interactor = controller.AddComponent<XRDirectInteractor>();
            interactor.interactionManager = manager;
            interactor.handedness = hand == "Left" ? InteractorHandedness.Left : InteractorHandedness.Right;
            interactor.improveAccuracyWithSphereCollider = true;
            interactor.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.StateChange;
            interactor.selectInput = new XRInputButtonReader
            {
                inputSourceMode = XRInputButtonReader.InputSourceMode.InputActionReference,
                inputActionReferencePerformed = ActionReference(actions, hand + "/Grip Pressed"),
                inputActionReferenceValue = ActionReference(actions, hand + "/Grip Value")
            };
            interactor.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.Unused;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = hand + " Hand Marker";
            visual.transform.SetParent(controller.transform, false);
            visual.transform.localScale = Vector3.one * 0.065f;
            UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            visual.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        static void AddTracking(GameObject target, InputActionAsset actions, string map)
        {
            var driver = target.AddComponent<TrackedPoseDriver>();
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.positionInput = new InputActionProperty(ActionReference(actions, map + "/Position"));
            driver.rotationInput = new InputActionProperty(ActionReference(actions, map + "/Rotation"));
            driver.trackingStateInput = new InputActionProperty(ActionReference(actions, map + "/Tracking State"));
        }

        static GameObject CreateGlass(Transform returnPoint, XRInteractionManager manager, Material idle, Material hover, Material held)
        {
            var glass = new GameObject("Grabbable Test Glass");
            glass.transform.SetPositionAndRotation(returnPoint.position, returnPoint.rotation);
            var visuals = new GameObject("Glass Visuals");
            visuals.transform.SetParent(glass.transform, false);
            visuals.AddComponent<MeshFilter>().sharedMesh = GlassMesh();
            var renderer = visuals.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = idle;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // A rounded convex collider keeps the hollow-looking placeholder physically stable.
            var collider = glass.AddComponent<CapsuleCollider>();
            collider.radius = 0.045f;
            collider.height = 0.20f;
            var body = glass.AddComponent<Rigidbody>();
            body.mass = 0.18f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var grab = glass.AddComponent<XRGrabInteractable>();
            grab.interactionManager = manager;
            grab.colliders.Add(collider);
            grab.selectMode = InteractableSelectMode.Single;
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.useDynamicAttach = true;
            grab.matchAttachPosition = true;
            grab.matchAttachRotation = true;
            grab.predictedVisualsTransform = visuals.transform;
            glass.AddComponent<PrototypeGlassRecovery>().ReturnPoint = returnPoint;
            glass.AddComponent<PrototypeGrabFeedback>().Configure(renderer, idle, hover, held);
            return glass;
        }

        static InputActionAsset LoadOrCreateActions()
        {
            var existing = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (existing != null)
                return existing;

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "Prototype XR";
            AddPoseMap(asset, "Head", "<XRHMD>", "centerEyePosition", "centerEyeRotation");
            foreach (var hand in new[] { "Left", "Right" })
            {
                var device = "<XRController>{" + hand + "Hand}";
                var map = AddPoseMap(asset, hand, device, "devicePosition", "deviceRotation");
                // These usage-based paths match XRI 3.6.1 Starter Assets and the OpenXR Touch profile.
                map.AddAction("Grip Pressed", InputActionType.Button, device + "/{GripButton}", expectedControlLayout: "Button");
                map.AddAction("Grip Value", InputActionType.Value, device + "/{Grip}", expectedControlLayout: "Axis");
            }
            var reset = asset.AddActionMap("Prototype").AddAction("Reset Glass", InputActionType.Button, expectedControlLayout: "Button");
            reset.AddBinding("<XRController>{LeftHand}/secondaryButton");
            reset.AddBinding("<XRController>{RightHand}/secondaryButton");
            reset.AddBinding("<Keyboard>/r");
            File.WriteAllText(InputPath, asset.ToJson());
            UnityEngine.Object.DestroyImmediate(asset);
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
        }

        static InputActionMap AddPoseMap(InputActionAsset asset, string name, string device, string position, string rotation)
        {
            var map = asset.AddActionMap(name);
            map.AddAction("Position", InputActionType.Value, device + "/" + position, expectedControlLayout: "Vector3");
            map.AddAction("Rotation", InputActionType.Value, device + "/" + rotation, expectedControlLayout: "Quaternion");
            map.AddAction("Tracking State", InputActionType.Value, device + "/trackingState", expectedControlLayout: "Integer");
            return map;
        }

        static InputActionReference ActionReference(InputActionAsset asset, string name)
        {
            var action = asset.FindAction(name, true);
            return AssetDatabase.LoadAllAssetsAtPath(InputPath).OfType<InputActionReference>()
                .First(reference => reference.action.id == action.id);
        }

        static void CounterSection(string name, Transform parent, Vector3 location, Vector3 dimensions, Material body, Material top)
        {
            Box(name + " Counter", parent, location + new Vector3(0f, 0.47f, 0f), new Vector3(dimensions.x, 0.94f, dimensions.z), body);
            Box(name + " Worktop (1 m)", parent, location + new Vector3(0f, 0.97f, 0f), new Vector3(dimensions.x, 0.06f, dimensions.z), top);
        }

        static void Box(string name, Transform parent, Vector3 position, Vector3 dimensions, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = dimensions;
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Material MaterialAsset(string name, Color color)
        {
            var path = "Assets/Prototype/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader is required for the prototype.");
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static Mesh GlassMesh()
        {
            const string path = "Assets/Prototype/TestGlass.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                return existing;
            const int segments = 24;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            // Cross-section: outside bottom, outside lip, inside lip, inside bottom, base center.
            var radii = new[] { 0.035f, 0.045f, 0.037f, 0.027f, 0f };
            var heights = new[] { -0.10f, 0.10f, 0.10f, -0.085f, -0.085f };
            for (var ring = 0; ring < radii.Length; ring++)
                for (var segment = 0; segment < segments; segment++)
                {
                    var angle = segment * Mathf.PI * 2f / segments;
                    vertices.Add(new Vector3(Mathf.Sin(angle) * radii[ring], heights[ring], Mathf.Cos(angle) * radii[ring]));
                }
            for (var ring = 0; ring < radii.Length - 1; ring++)
                for (var segment = 0; segment < segments; segment++)
                {
                    var next = (segment + 1) % segments;
                    var a = ring * segments + segment;
                    var b = ring * segments + next;
                    var c = (ring + 1) * segments + segment;
                    var d = (ring + 1) * segments + next;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            var baseCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -0.10f, 0f));
            for (var segment = 0; segment < segments; segment++)
                triangles.AddRange(new[] { segment, baseCenter, (segment + 1) % segments });
            var mesh = new Mesh { name = "Hollow Placeholder Glass" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

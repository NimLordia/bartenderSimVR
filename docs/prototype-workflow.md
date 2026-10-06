# Prototype workflow

Unity project: `BartenderSimVR/`; editor: 6000.6.4f1. Scope: first headset grab interaction, not the beer-service gameplay slice.

## Open and run

- Open the existing project through Unity Hub. Let package import and compilation finish.
- If Unity was open during setup, save your work and reopen the project to load the saved Quest settings.
- Open `Assets/Scenes/BartenderPrototype.unity`.
- The floor-height XR Origin tracks the headset and Touch controllers. Reach into the glass, hold grip to grab, and release to drop.
- Gold highlights contact; green marks a held glass. Release, then press B/Y to return it. Floor/out-of-bounds drops return automatically after 0.75 seconds.
- The prototype uses physical tracked movement only. Reach, station height, instructions, and controller feel require headset evaluation.
- Editor Play requires an OpenXR runtime/headset, such as Quest Link. Keyboard R resets the glass; a keyboard/mouse VR simulator is not included.

## Editor commands

- `Bartender VR/Set Up First Prototype`: configure XR/Android and regenerate the prototype scene. Explicit regeneration replaces this scene, preserving SampleScene.
- `Bartender VR/Validate Setup`: check XR settings, required rig/glass components, and Android settings.
- `Bartender VR/Build Quest APK`: build a development APK to `BartenderSimVR/Builds/Android/BartenderSimVR.apk`.
- `Bartender VR/Capture Prototype Preview`: save a desktop composition check to `Builds/prototype-preview.png` without entering Play mode.
- Change the active build profile to Android before building or using OpenXR project validation.
- Headless equivalents: Unity `-batchmode -quit -projectPath <project> -buildTarget Android -executeMethod BartenderSimVR.Editor.PrototypeSetup.Setup` or `.BuildQuest`; use `-logFile <path>`.

## Deploy and verify

- Enable developer mode on Quest 2, connect USB, and accept the headset's USB debugging prompt.
- Use the editor's bundled ADB: `adb devices` then `adb install -r <apk>` and `adb shell am start -n com.nimlordia.bartendersimvr/com.unity3d.player.UnityPlayerGameActivity`.
- Repeat grab/release with both controllers, drop onto the counter/floor, retrieve or reset, and check tracking after headset removal/resume.
- Confirm comfortable reach and readable instructions. Measure CPU/GPU frame timing at the chosen headset refresh rate; no comfort or performance claim follows from desktop checks.

## Verification record

- Installation: SDK, NDK, and OpenJDK present; ADB, Java, and NDK compiler execute.
- Unity 6000.6.4f1: scripts compiled and saved scene/XR/Android configuration validation passed in an isolated copy; verified files were copied back to the project.
- Editor Play Mode: 5/5 tests passed, including 20 alternating-hand grab/release/recovery cycles, held-object protection, floor recovery, velocity clearing, and release-frame reset with real XRI throwing. Tests use a scoped 72 Hz frame duration; this does not select the headset refresh rate.
- Android: development ARM64 APK built with 0 errors and 5 warnings (49.7 MB). Manifest/native-library inspection confirms OpenXR, Quest 2 support, minimum API 32, target API 36, and GameActivity. Reports are in `BartenderSimVR/Builds/`.
- Desktop preview: counter/glass/instructions rendered; instruction text fits its board. No keyboard/mouse XR simulator was tested.
- Headset: no ADB device connected during verification, so installation, tracking, controller feel, reach, comfort, and frame timing remain untested.
- Re-run editor tests with Unity `-batchmode -projectPath <project> -buildTarget Android -runTests -testPlatform PlayMode -testFilter BartenderSimVR.Tests.PrototypeGlassInteractionTests -testResults <xml> -logFile <log>`; omit `-quit` so the runner can finish.

## Repository

- One repository at the workspace root, with `https://github.com/NimLordia/bartenderSimVR.git` as origin.
- Unity's original nested Git history and metadata are preserved under root `.git/preserved-repositories/`; no project files were removed.
- Commit Assets (including `.meta`), Packages, ProjectSettings, and notes. Caches, validation copies, logs, and APKs are ignored.

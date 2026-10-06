# Technical direction

Last reviewed: 2026-10-06. Unity, standalone Quest, and Quest 2 as the minimum supported headset are confirmed. The initial interaction scaffold now has a pinned prototype stack; device acceptance remains pending.

## Engine decision and proposed supporting stack

| Layer | Candidate and purpose |
| --- | --- |
| Engine/code | Prototype: Unity 6.6 (6000.6.4f1), C#. Editor version is recorded in the project. |
| Rendering | Prototype: URP 17.6.0, simple unlit materials, Mobile quality with 4x MSAA and no shadows. Final palette and silhouette treatment remain open. |
| Headset interface | Prototype: OpenXR 1.18.0, XR Plug-in Management 4.7.0; Android Meta Quest support and Oculus Touch profile. ARM64 APK built; device acceptance pending. |
| Interactions | Prototype: XR Interaction Toolkit 3.6.1 + Input System 1.20.0; tracked controller direct grabbing. Custom drink behavior will sit above these. |
| Assets/workflow | Placeholder geometry first; Blender or licensed assets later. Git; use Git LFS when large binary assets appear. |
| Quest deployment | Android tools installed. Prototype: IL2CPP, ARM64, Vulkan, GameActivity, minimum API 32. Headset developer mode is still required; Meta Quest Developer Hub remains optional. |

Confirmed decision: use Unity. Rationale discussed: simple graphics and custom arcade interactions suit the proposed C# component workflow. The comparison assumed equal ability to learn/use either engine. See [engine comparison](engine-comparison.md) only when the rationale or alternatives are relevant.

## Platform and hardware

- Standalone VR runs on the headset with a mobile-class performance budget; PC VR runs on a connected computer. A Quest can be used in either workflow.
- Required hardware baseline: the user's Oculus/Meta Quest 2 with Touch controllers. The game must run natively and be testable on this headset. A strong development PC is also available.
- Confirmed release direction: standalone Quest. PC-connected iteration remains a development option; a PC VR release is not committed. See [platform comparison](platform-comparison.md) for rationale.
- Set graphics, physics, memory, and frame-time budgets against Quest 2. Additional headset support needs validation; OpenXR does not guarantee identical behavior across devices.
- PC-connected iteration can help development; validate native Quest 2 builds from the first playable slice and use this device for performance and interaction acceptance.
- Suggested first controls: tracked controllers. Hand tracking is a separate design choice.
- Decide refresh rate on the target device and measure frame timing there; 90 Hz allows about 11.1 ms per frame.

## Proposed first playable slice

The implemented scaffold covers a U-shaped counter, one grabbable glass, and dropped-object recovery. Setup/build instructions and verification are in [prototype workflow](prototype-workflow.md). Beer service below remains proposed.

1. Deploy a basic scene; verify head/controller tracking and grabbing.
2. Block out the U-shaped bar with simple geometry; activate only beer service.
3. Add two glass types, two taps, an order display, and a serving zone.
4. Support choose glass -> fill from tap -> serve -> accuracy/time feedback -> next order.
5. Repeat in the headset; tune reach, grab stability, error recovery, and frame timing.

Completion: repeated orders work without stuck objects or manual scene resets; correct/wrong glass and tap choices produce predictable feedback; filling is controllable; mistakes are recoverable; stations are reachable; measured frame timing meets the chosen target.

## Proposed implementation boundaries

- Pixel-liquid proposal: store equal-volume liquid units/cells and render a pixelated fill/stream inside a 3D glass. Count stored units, not headset-image pixels, whose count changes with viewpoint and resolution. Keep glass capacity, ingredient amounts, and order validity as gameplay data.
- A true 2D cell simulation is an optional experiment: arbitrary 3D tilt needs an approximation, and equal-area cells need volume calibration for different glass shapes. First test upright filling, then deliberate tilt/pour/spill rules.
- Keep order/recipe evaluation separate from XR input and visual effects so rules can be checked independently.
- For long-session comfort, test readable orders, real reach, repeated movement, and workload pacing in the headset. Consider adjustable station height and comfortable turning/access.
- Expand to shots/cocktails after beer service works, then add a small silhouette customer/story example. Crowd animation and glass/liquid effects still need performance checks.

Next: validate the first native Quest 2 grab/release, recovery, reach, and frame timing. Scoring, movement beyond tracked physical motion, palette, and liquid treatment remain open. Learn C#, Unity components/physics, tracked input, and profiling progressively through this prototype.

## Official references

- [Unity release support](https://unity.com/releases/unity-6/support), [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.6/manual/index.html), [Meta Quest setup](https://docs.unity.com/en-us/engine/6000.6/manual/xr/configuring-project-for/meta-quest-develop).

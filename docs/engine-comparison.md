# Engine comparison

Reviewed: 2026-10-06. Unity selected by the user. Comparison retained for reference; it assumed equal ability to learn/use both engines.

## Unity

- Strengths: C# components suit readable order/recipe/state rules; XR Interaction Toolkit supplies basic grab/UI/haptic interactions; URP supports graphics across mobile and PC targets.
- Tradeoffs: XR and rendering are assembled from packages whose compatibility must be managed. Custom drink interactions and character/quest logic still require implementation; toolkit samples are starting points.
- Workflow fit: attractive when much of the game is small, custom gameplay systems maintained as text code. Scene/prefab edits still need editor validation.

## Unreal

- Strengths: Blueprints support visual behavior iteration; C++ supports core systems and lower-level control. Integrated material, animation, and cinematic workflows are attractive for elaborate character performances and environments. Engine source is available under its license.
- Tradeoffs: C++ introduces a native build workflow; large Blueprint graphs can become hard to maintain and merge. VR/mobile rendering requires explicit tuning; expensive visual features consume the same limited frame budget as gameplay.
- Workflow fit: attractive when visual authoring and integrated production tools are central. A common approach is C++ rules with Blueprint-authored content and behavior.

## Fit for this game

- Decision: Unity, following discussion of its fit for simple graphics and custom arcade interactions. URP, OpenXR, and XR Interaction Toolkit remain supporting-stack recommendations in technical direction. The rationale is a project-fit judgment, not a measured performance comparison.
- Unreal was a viable alternative for the same style, pixel liquid, and service loop, particularly for its visual authoring and character/environment tools.
- Both need custom liquid/order logic and target-device profiling. A strong development PC does not expand the budget of a standalone build.

## Commercial model (recheck before release)

- Unity Personal eligibility currently uses a USD 200,000 revenue/funding threshold over the last 12 months; paid subscriptions apply when ineligible. See [Unity Personal](https://unity.com/products/unity-personal).
- Unreal's standard game model generally applies a 5% royalty to lifetime gross product revenue above the first USD 1 million, with exclusions/programs affecting the result. See [Unreal licensing](https://www.unrealengine.com/license).

## Technical references

- [Unity URP](https://docs.unity3d.com/6000.3/Documentation/Manual/universal-render-pipeline.html), [XR Interaction Toolkit](https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/index.html).
- [Unreal Blueprint/C++](https://dev.epicgames.com/documentation/en-us/unreal-engine/coding-in-unreal-engine-blueprint-vs-cplusplus), [XR best practices](https://dev.epicgames.com/documentation/unreal-engine/xr-best-practices-in-unreal-engine).

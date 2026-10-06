# Platform comparison

Reviewed: 2026-10-06. Standalone Quest is selected, with Quest 2 as the minimum supported headset, confirmed by the user.

| Option | Main strengths | Main costs |
| --- | --- | --- |
| Standalone Quest | No gaming PC required; direct headset launch; untethered physical service. | Limited rendering, physics, memory, and sustained performance budgets; regular native headset builds and profiling. |
| PC VR | More capacity for crowds/effects/physics; convenient editor iteration. | Player needs a suitable PC and connection/runtime setup; wider hardware variability and minimum-spec testing. Wireless streaming is possible but depends on connection quality. |
| Both | Another distribution path; much of the gameplay can be shared. | Separate builds, quality settings, controller/runtime coverage, store integration, and ongoing regression testing. |

## Selected direction and proposed workflow

- Confirmed decision: standalone Quest as the first release target. The simple visual direction and silhouette background suit the proposed approach. PC VR is an optional future release, with no commitment.
- Use PC-connected Unity iteration for convenience, and deploy native headset builds early. PC frame timing cannot establish standalone performance.
- Keep drink/recipe/order rules shared and XR input through the proposed OpenXR/toolkit path. A second release is additional work, even when gameplay code is shared.
- Prefer compact liquid data, batched effects, and simple silhouette customers. Glass/liquid effects and physics still need native performance checks.
- Quest 2 is the required development/test and minimum release baseline. The game must run on the user's own headset; validate additional models separately.

Validation milestone: test the beer-service prototype natively on Quest 2. Use measured frame timing, readability, and interaction comfort to tune scope before expanding content or committing to a second release.

## Industry context

- There is no mandatory choice between standalone and PC VR. Meta Horizon Store and Steam are established distribution paths; OpenXR is an interoperability standard, not a guarantee of identical builds or controller behavior.
- GDC's January 2026 report, page 35: among 94 recent VR/AR/MR developer respondents, 82% used Meta Quest/Horizon Store and 37% Steam VR. Multiple answers were allowed. This is a small developer-activity sample, not player market share or a sales forecast.

## Open decisions

- Graphics/performance and comfort targets within the Quest 2 baseline; validation of additional headset models.
- Whether and when a second platform is worth its validation and maintenance cost.

## References

- [GDC 2026 report, mirrored PDF](https://investgame.net/wp-content/uploads/2026/01/2026-01-29-dec052f4_d88e_48ce_9f83_a18ce2f2a6e5_541400_GDC26_PDF_SOTI_Report.pdf).
- [Meta native performance workflow](https://developers.meta.com/vr/documentation/unity/po-perf-opt-mobile/), [Link development workflow](https://developers.meta.com/vr/documentation/unity/unity-env-device-setup/).
- [Steam VR distribution](https://partner.steamgames.com/doc/features/steamvr), [OpenXR controller requirements on Steam](https://partner.steamgames.com/doc/features/steamvr/settings).

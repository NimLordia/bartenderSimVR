# Agent guide

## Load only relevant context

- Read `docs/game-brief.md` for the shared game concept.
- Read relevant sections of `docs/technical-direction.md` for technical or prototype work.
- Unity project: `BartenderSimVR/`. Inspect actual files before choosing patterns; see `docs/prototype-workflow.md` for setup, builds, and validation.

## Task context

| Task | Focus |
| --- | --- |
| XR interactions | Headset input, grabbing, taps, pouring, reach, and comfort. |
| Gameplay | Orders, glass/drink matching, fill rules, timing, and feedback. |
| Environment/art | U-shaped station layout, readable props, and rendering cost. |
| Characters/narrative | Background customers, recurring characters, stories, and sidequests. |
| QA/performance | Rule correctness, repeated service, recovery, headset comfort, and frame timing. |

Use the shared notes plus the files relevant to the assigned task. Add a short domain guide only when implementation creates durable context that is missing here.

## Keep notes slim

- Separate user-confirmed requirements, recommendations, and open questions.
- Do not silently turn a proposed stack, milestone, or mechanic into a confirmed decision.
- Update the canonical note when discussion settles a material decision; replace superseded proposals.
- Keep gameplay facts in the brief and technical choices in technical direction; link instead of duplicating.
- Aim for under 60 lines per topic document and 40 lines here. Split by topic only when needed.
- Preserve decisions and their short rationale; omit transcripts, research dumps, and speculative backlogs.
- For delegated work, provide the exact task, relevant files, constraints, and completion criteria.

## Verification

Use focused checks for gameplay rules and repeated interaction. Clearly report what was tested in the editor, in a desktop simulator, and on a headset. Comfort and controller feel require human headset playtesting; do not claim those from desktop checks.

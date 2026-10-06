# Game brief

Last updated: 2026-10-06.

## Confirmed concept

- A VR bartender simulator with an arcade core: fulfill orders as quickly and accurately as possible.
- The player works inside a U-shaped bar with different activity sections.
- Beer service includes different beer glasses/cups that must match different beer taps.
- Other sections include shot glasses, different drinks, and cocktail preparation.
- Customers populate the background; some are recurring characters with their own stories and sidequests.
- Confirmed visual direction: light, playful, simple graphics; background customers appear as shadow-like silhouettes, with no realistic lighting treatment.
- Presentation goal: easy on the eyes and brain during long gameplay sessions.

Core loop: receive an order -> choose glass and station -> prepare drink -> serve -> receive the next order.

## Visual exploration

Proposed treatment: flat/unlit shading, a restrained palette, quiet background motion, clear interactive prop shapes, and recognizable silhouettes for story characters. These implementation details remain proposals; long-session comfort needs headset playtesting.

User idea: depict liquid using 2D pixels and count them to measure the contents of a glass. This is an exploration, not a selected simulation. See technical direction for the distinction between stored liquid cells and rendered screen pixels.

## Open design questions

- How accuracy and speed affect scoring, failure, and progression.
- Arcade simplification versus realism for pouring, spilling, and recipes.
- How the player reaches different sections: physical movement, turning, or virtual movement.
- Session structure and how character stories/sidequests fit around order pressure.
- Palette, silhouette treatment, audience, and single-player or multiplayer scope.

## Current stage

The first interaction scaffold now includes a U-shaped counter, one grabbable glass, and dropped-object recovery. See [prototype workflow](prototype-workflow.md) for verification. The fuller beer-service slice remains a recommendation in `technical-direction.md`, not an approved production scope.

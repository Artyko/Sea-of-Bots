# Performance v225

Latest accessible Galleon/Galleon battle is v223, approximately 21-34 FPS across listed reports. ShoreMotion remains expensive (roughly 2.7-13.9 ms per sampled frame in the recent rows), tactical ShoreNavigation sometimes spikes, crew is roughly 2.9-6.5 ms with calls up to 16.6 ms. SupportQuery is usually below 1 ms in these reports. Sparse inclusive samples are not a full native physics/GPU profile and must not be summed.

This pass removes planner allocation overhead found in source:
- Around returns the existing route when a crate/flight does not require a detour; no full list copy for each nonintersecting obstacle.
- Graph search reuses its node scratch list and distance/parent/visited arrays. All live endpoint state and visited flags are reset on every synchronous search. These workspaces are for main-thread synchronous planning only, not concurrent/reentrant calls. Returned routes are separate owned lists, never workspace aliases.

No changes to path geometry, collision tests, AI priorities, walking or rendering. Native shore query overhead and engine/GPU costs remain candidates; no measured post-v225 FPS gain is claimed.

Validation: full compile, 3514 task routes, 656 cargo routes, 656 exact point-for-point comparisons to the previous reference algorithm (including repeated requests to exercise workspace reuse), logged stair recovery regressions. Requires restart/new battle for live FPS verification.

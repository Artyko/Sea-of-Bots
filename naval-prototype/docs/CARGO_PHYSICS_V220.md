# Cargo physics and passage graphs v220

All ship profiles and both teams use the same implementation. No cargo meshes, materials, stocks, crate positions, crew job priorities or walking speed changed.

## Compact collision

Each intact crate now has five wooden-panel box proxies and one content envelope (at most six enabled shapes). Decorative voxel subdivision is retained for rendering, damage and connectivity, with individual colliders disabled and excluded from the general hull merger. `NavalVoxelHit` maps proxy contacts to the nearest remaining real voxel; existing direct-hit damage guarantees that voxel receives the impact. Shore classification excludes cargo proxies as ship shapes.

The compact shape deliberately fills small slat/content gaps within the crate envelope. Hull, decks, stairs, island collision and their holes are not approximated. Any external missing/moved/detached cargo voxel immediately disables the old proxies and restores exact remaining source colliders. This conservative fallback persists on the damaged crate, preserving physical holes; undamaged crates remain compact. Resource depletion internally hides the original content meshes and updates only the content envelope, without activating hundreds of colliders. Empty plank crates disable all proxies. Explicit inventory reset can compact again if sources are attached and shell intact.

Diagnostics report actual source-cell counts and currently enabled cargo shapes every 15 seconds in the battle log. This replaces estimates from counting construction calls: AddDecorationBox subdivides wood into multiple cells.

## Shared graph

Fixed crate and stair-corner nodes, edge visibility and distances are built once per ship definition and floor. Construction warms maintenance-floor graphs. Live requests connect their current start/goal to those fixed nodes and retain the existing shortest-path search. Physical walking/clearance, stairs, damage and job ownership are not cached. Graphs retain the original conservative layout envelopes even after crates empty or disappear, so disappearing crates do not create unverified shortcuts; physical recovery checks remain live. Additional floor keys are bounded to 16 per definition.

## Validation

Full mod compile. 3514 directed task routes, 656 cargo routes and stock tests; stair capsule approximation and logged underflight recovery. New reference test compares all 656 supply routes point-for-point against the previous uncached algorithm and verifies graph reuse. New collision test compiles actual NavalCargoCollision against mocked Unity geometry for scales 2/3/4: source suppression, six proxies, contact-to-voxel resolution, depletion shrink, external damage/detachment fallback, reset reuse and empty-crate disappearance.

These are not native PhysX/gameplay tests and do not establish an FPS gain. A restarted game and new ship spawns are required. Compare Galleon/Galleon logs, cargo active_shapes, ShoreMotion/ShoreNavigation and frame time. Damaged-crate fallback intentionally raises shape count locally.

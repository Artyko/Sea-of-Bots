# v197 — underflight recovery and hull pose publication

## Stair stall
The v195 logged sailor at (0.22,-0.71,-0.33), aiming at tread (0.18,0.34,-0.34), was over three world metres below the next waypoint. Arrival tolerance cannot safely solve this.

Shared FollowRoute now detects a sailor displaced below a flight. It tries both lateral exits outside the staircase footprint, at the current height. The exit requires a clear destination capsule and a clear swept capsule segment. Then the controller descends outside the flight and follows a fresh supply-aware route from that floor to the original job destination, via the normal lower stair entry. This does not relocate the character or disable collision. Requests are bounded to once per two seconds; if neither exit is clear, normal bounded blocked-task recovery still applies. Lower and upper galleon flights select their own floor. An active ledge recovery is not overridden.

## Physics
NavalShipPhysics.SetPose now assigns the existing kinematic Rigidbody.position/rotation directly, avoiding compound root Transform movement. It skips exact unchanged position or rotation writes. It does not use deferred MovePosition/MoveRotation or interpolation. Explicit physics synchronization after hull/rider updates, before crew/player movements and queries, remains intact. Damage chunk flush, instant source-collider fallback, detailed hit resolution, and shore sweeps remain unchanged. Character stepOffset is no longer reassigned each movement tick if already correct.

This targets the dominant PhysicsSync cost seen in v195 (median interval ~19.6 ms/frame), without globally skipping synchronization or caching potentially stale collisions. No measured FPS gain is claimed before native testing.

## Verification
Full mod builds against installed game assemblies. UnderflightRecoveryChecks reproduces the exact log case, both galleon flights, both lateral choices, original destinations, and normal-contact exclusions. Existing stair arrival/contact simulations, stair traffic, 3321 task routes and furnishing-clearance routes, shore sweeps and retreat checks pass. ShipPoseChecks executes actual NavalShipPhysics code with headless Unity substitutes; ColliderLifecycleChecks executes the actual collider manager through hole/repair/debris/restoration. These cannot validate native PhysX scene-query publication or collision response.

## Next live check
Restart the game to load v197. Compare PhysicsSync interval timings and FPS on the same ships. Watch for Crew stair underflight recovery followed by task completion, absence of repeated route-stalled logs, and no new player/deck or cannon-hit collision regressions.

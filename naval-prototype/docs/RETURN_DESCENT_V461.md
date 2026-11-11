# v461: repeated return and descending tread stalls

Reviewed v459 log, repeated ironclad Returning above descending treads and reissued blocked stations. After three return failures, cache that home post for 15 seconds. Same post assignments respect the cooldown; dispatch still tries damage control first, but does not replay the blocked home path if nothing available. Changed/distant posts are not suppressed. Logs record Home route deferred. No teleport to home.

Descending stair waypoint completion accepts a supported capsule up to radius+0.5 m above the current pin when horizontally within 0.45 m and the next pin is lower. Only the route index advances; actual movement remains native. Vertical ladders and final destinations retain their own rules.

Build, StairTrafficChecks, RecoveryFairnessChecks and diff check passed. Full gameplay access and all prior unavailable repair targets are not certified resolved.

# Performance v219

Baseline: latest v218 log, Galleon/Galleon 9.3-16.4 FPS. Shore 4.67-31.30 ms per sampled frame, Crew 3.78-16.31, SupportQuery 0.28-10.52 with individual calls up to 22.90 ms. Samples are sparse (2-4 frames per interval), inclusive and do not include all native physics/rendering. No measured FPS gain yet.

Changes shared by all ship profiles:
- Tactical shore look-ahead memo: up to 32 probes, 0.15 s TTL, less than 0.25 m displacement and 1 degree direction change, matching ship and range. Returned clearance subtracts position/angular drift as a precaution. This is a navigation heuristic only, not authoritative collision detection. CanAdvance, island SAT, translation/rotation sweeps, penetration policy and native movement overlap queries stay exact and uncached.
- Separate ShoreNavigation/ShoreMotion scopes to distinguish costly navigation probes from actual movement checks in next logs. These are nested in Shore and must not be added to it.
- HasSupport returns false after a nearest ray/sphere query reports no hit, eliminating the redundant full identical query. Foreign or side-face nearest hits still use the full growing buffer fallback. Exact within-frame support memos remain generation-bound; publishing a changed ship pose now invalidates them immediately, in addition to sync/damage invalidations.
- CanBearOn exact frame-local memo checks station, target point, muzzle position, ship rotation and damage revision. Functional state is checked before reuse. No temporal approximation of combat bearing or firing.
- SupportTravelCost reads the ship scale once per estimate and sums scaled segment lengths without a TransformVector call per waypoint; scale changes also expire its existing bounded ranking cache. Actual walking routes are not memoized or throttled.

Validation: full v219 build; 3514 directed task routes, 656 cargo routes/stock checks; capsule ascent/descent approximation, logged underflight recovery; 30000 SAT box-pair comparisons, shore sweep spacing and escape policy; query-buffer saturation tests. Added SupportFastPathChecks compiles the actual NavalPhysicsQueries method against mocked Unity casts and verifies miss fast path, exact memo, invalidation, foreign-hit fallback, and frame rollover for ray/sphere casts.

No native gameplay, GPU or PhysX benchmark was performed. Large compound-collider overhead and native movement-query cost may remain; the new per-area timings should guide the next step rather than weakening collision checks.

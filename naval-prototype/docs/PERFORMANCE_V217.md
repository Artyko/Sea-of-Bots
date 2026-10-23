# v217 performance audit

Latest accessible fight was v214 Galleon/Galleon. It shows 12–14 FPS intervals with Shore reaching tens of inclusive milliseconds, SupportQuery occasional 10–20+ ms intervals, Crew varying by task activity; Flood and PhysicsSync much smaller. No current native profiler/GPU trace is available, so not all frame time can be assigned.

Changes:
- Cache negative land classification only for NavalHullVoxel/NavalMergedCollider identities. Hull/debris shapes of these types remain excluded by the original rule; characters still checked dynamically. No cached clearance verdict is reused for actual movement.
- Filter native motion-overlap candidates once, compute their starting penetration once per motion query, then reuse it for each sweep substep. Target penetration is still freshly checked at every substep. Indexed island barrier remains unchanged.
- Dispatch travel-cost estimates cache for up to 0.25 seconds and 0.5m local movement, at most 64 entries per worker. These are scheduling estimates only, never actual movement/repair reach/physics decisions. The cache is not used to execute routes.
- Distant horizon mesh refreshes at 10Hz; nearby collision, ocean height and swimming calculations retain existing rates. Cosmetic far-water normals/bounds rebuilds no longer run every render frame.

Full mod build passes; unchanged SAT random-pair, swept shore and cargo route regression suites pass. These do not execute Unity native ComputePenetration or measure live FPS. No measured improvement or claim that every bottleneck is fixed. Next run should compare same-ship Shore/SupportQuery/Crew intervals and total frame time; remaining likely native physics/render cost needs engine profiling.

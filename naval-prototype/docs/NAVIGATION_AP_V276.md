# v276: navigation, AP fuzes and damage-query cost

Inspected the latest v275 Dreadnought/Dreadnought battle log: repeated eight-second stalls near lower stores and stair sides; frame samples fell to 23–37 FPS. Logged Crew samples reached roughly 4.8 ms average/frame, with individual calls above 9 ms. These timings do not account for every engine/render cost.

- Stair bypass envelopes now allow 1 m outside physical edges instead of 0.5 m; graph corners retain an extra 0.05 m. Actual stair lanes remain unchanged.
- A sailor above a route target on a rack/trim initiates a collision-checked walk-off instead of repeatedly pushing downward.
- Missing deck-cell shapes are captured on hull revision and indexed in local space. Exact overlap semantics remain unchanged. No repeated native Transform scale reads inside per-step hole scans.
- Failed deck-gap escape probes are retried at most every 0.75 s per sailor, with 12 distant probes per retry rather than 96 on every frame; the incremental local BFS remains available.
- Unarmed AP passes through perforated wooden cells without arming or impact burst, continuing the same ordered sweep. Metal/armour/machinery may arm it; an already armed shell retains its next-obstacle / two-second fuze behavior.

Build and pure regression tests are available; actual collision recovery and FPS require a new live battle log. No live FPS improvement is claimed from these tests.

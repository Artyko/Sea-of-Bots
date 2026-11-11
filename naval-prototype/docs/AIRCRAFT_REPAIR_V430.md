# v430: repair radius and aircraft crashes

- Sailor repair patch radius is 2 world metres (4 m diameter), independent of ship scale. Reach/visibility and repair claims remain required per voxel.
- Aircraft parts check initial overlaps near environment as well as swept movement, catching rotation into decks. Sweep voxel resolution retries on the same collider surface. Both paths share the existing 50 kg bomb damage, fragments and visual effect, and consume the aircraft on first impact.
- Falling aircraft and debris consume the full frame delta in steps of at most 0.05 s. Previously a 0.05 s frame cap slowed falling at less than 20 FPS. Live aircraft flight tuning is unchanged.

Validation: build, RepairRadiusChecks, AirRaidChecks, git diff --check. Native physics/visual behaviour to be verified in the user's next gameplay run.

# Ammo and repair review v229

Latest v228 Brigantine/Brigantine log: 19 allied and 9 enemy Repair finished reports, 243 restored cells, 10 Planks fetched and zero Plank trip failed reports. Repair now demonstrably completes, though return/ledge/stair stalls remain in the same battle.

Ammo diagnosis is limited by missing per-stock telemetry. LogShip used Root.GetComponent<NavalStations>, but station components live on controller objects, so it returned without emitting inventory/combat-state diagnostics. Fixed to NavalStations.ForShip. Future logs include hold ammo, each gun ammo and runner ID; actual hold stock at the earlier failure cannot be proved retroactively.

Code blockers corrected:
- Deck ammo pickup still used 1.30 m offset from crate centre, less than outer half-width ~0.675 m plus actual 0.80 m sailor radius. Now 1.65 m inward, matching hold access clearance. Meshes/stocks unchanged.
- ReturningWithoutWork was Busy and excluded from new ammo tasks. Idle return can now be interrupted by a permitted supply task.
- Any HasRepairDemand previously cancelled/blocked all ammo runs. A spare runner can supply when another alive/non-stranded/non-rescuing sailor has the leak repair assignment. Uncovered leaks, active fire, critical flooding and urgent trim still block/interrupt supply. Assignment coverage is not proof all leak routes are reachable; existing repair/obstruction rules continue.
- Added ammo assigned/fetched/delivered diagnostics. Existing nearest-source selection, donor reserve, 5-round carry and finite stock conservation remain.

Validation: full build, pure survival-priority cases, 656 cargo routes/stock tests and 3514 directed crew routes. No native validation of the changed deck pickup, role allocation or actual deliveries yet. Restart/new battle required.

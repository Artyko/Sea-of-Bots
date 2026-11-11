# v431: destroyer bottom repairs and underflight recovery

Reviewed active Player.log, battle version v428. Novik repair completions exist, but repeated repair-access waits and returning/underflight blocks remain. Code confirms an inaccessible outer bottom 1.2 m beneath a continuous inner floor, with no access hatch.

- Consolidated Novik bottom and inner floor into one 0.2 m watertight, destructible structural floor at the existing hold walking level. Shortened side/end shell down to the same bottom edge. Interior floor height, machinery and station positions are preserved; the exterior underside is now 1.2 m higher. This is a geometry change for newly spawned ships, not a retroactive repair of the running v428 battle.
- Destroyer repair aiming now uses its actual 0.2 m slabs instead of the legacy 0.25 local-unit thickness. Side holes away from deck seams retain their true height; bottom/top deck faces can be reached from the appropriate side.
- Underflight exit starts on the real hold floor rather than an elevated waypoint at the sailor's old unsupported height. Collision clearance is retained, with two protected exit/clear-aisle waypoints.

Validation: build; DestroyerRepairAccessChecks (slab faces, side targets, grounded exits on both sides); StairTrafficChecks (24 routes); git diff --check. Native gameplay access and post-change flooding require a new battle run.

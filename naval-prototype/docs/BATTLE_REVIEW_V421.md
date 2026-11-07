# v421 — repair verification, routes, bridge headroom and larger raids

Reviewed the latest v420 battle log: repairs do complete with restored>0, including a 59-voxel Novik patch, but access stalls remain at gun approaches, stair points and returning routes. Repair grouping used .85 local units, scaling its physical radius with ship size. Claim, restore and blocked-patch grouping now share a 1.2 m world-space radius, and each restored cell must still be reachable/visible. This reduces the amount repaired per plank on large ships and prevents restoring neighbouring inaccessible cells through intervening structures.

A successful waypoint arrival now resets the movement watchdog before its timeout result can discard the just-completed movement. Novik rear-approach recognition now uses the shared gun service distance instead of stale hardcoded 2.3 m geometry.

Raised the bridge's upper window beams and roof by .6 m and extended supporting posts to meet them. Balcony-to-helm head clearance increases from about 2.6 to 3.2 m.

Air waves are now 5/6/6 aircraft: 17 planes, 68 bombs. Existing spacing and avoidance remain active.

Validation: build; RepairPatchScaleChecks, AirRaidChecks, CrewMovementChecks. Physical access, remaining battle congestion and raised headroom need gameplay confirmation.

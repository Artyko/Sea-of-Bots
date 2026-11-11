# v432: small repair sites, bilge enclosure, ballistic aircraft and AA supports

Reviewed v431 Player.log. Ironclad repair-access failures include (2.12,-0.01,3.88), (-2.35,-0.14,-4.63), (2.35,0.11,-4.63), and stern edge holes near z=-6. Repair completions also occur; failures are local access issues, not a total repair stop.

- Steam repair candidates include x=+/-1.70 tank lid centre, with actual tank surface height. Added a fallback search in 1.0/1.8/2.6/3.4 m rings on supported deck edges. Support, capsule clearance, sight and reach remain required. Sailing candidate count is unchanged.
- Corrected v431 bilge enclosure omission: destructible lower casing covers Novik's existing below-floor ballast reservoirs and pipes. The accessible structural floor remains the watertight boundary; damage to the protective casing alone does not create an unreachable occupied-hold leak. No machinery moved into traffic lanes.
- Flying biplanes retain airspeed with a maximum 55-degree dive / 30-degree climb. Shot-down aircraft inherit the last actual flight velocity, with gravity integrated in short steps; horizontal velocity is not reset. Logs now record inherited velocity.
- AA anchors are collected at the foot rather than the elevated gun pivot (the old radius could yield an empty list). Mounts additionally require an intact deck/platform support below. Lost support disables and drops the mount via the shared debris system. Installation logs include both support counts.

Validation: build; CrashAndRepairV432Checks; LowerDeckSideRepairChecks (154 lower armour cases); RepairWorkSiteChecks; AirRaidChecks; DestroyerRepairAccessChecks; git diff --check. Native support/drop behaviour, visuals and live route clearance need the next user gameplay run. No measured FPS gain claimed.

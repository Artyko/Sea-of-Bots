# v275: hollow turrets, real hatch access and v274 battle audit

The last inspected Player.log is v274 Dreadnought versus Ironclad Corvette:
- No exceptions.
- Eight stalled-route events on the Dreadnought, five on the corvette.
- Three route-block events on the Dreadnought, ten on the corvette.
- Recovery/crew work reached 132.24 ms in one call; this is old-build telemetry,
  not a measurement of the new implementation.
- The boarding exit obstruction at (+/-2.10,1.81,-.82) matches a physical
  ready-use ammunition locker beside the Dreadnought ladder.

## Turrets

The old solid armour block and outside control pedestals are removed. Each
305-mm mounting now has a hollow steel chamber, floor, roof, split front gun
openings, full-size doorway, armoured swinging door and internal chair.
The seat has standing-controller clearance too: sitting animation changes the
model, not the collision capsule. The existing native MechLegs_Emote_Sit_Idle
clip was present in the inspected game log and is reused.

One operator starts inside each mounting. Its local seat follows turret training.
Entry/replacement and emergency departure use an explicit sequence: go to the
external hatch approach, return the turret to its access angle, open the door,
walk the physical treads and doorway, then sit. Raised X has its own fixed access
staircase. Exit reverses that route before releasing the sailor for other work.
A failed entry backs out through reached treads rather than teleporting through
armour. Reserved/transiting crew cannot be stolen by support allocation; final
attack and sinking also request departure. Access status is logged explicitly.

Door movement pauses when the proposed plate pose would overlap a live character
controller. The leaf swings outward. Player proximity opens access automatically;
a player visiting the chamber keeps access open and the mounting stopped.
During hatch access the turret does not fire. Interior chair, armour and hatch
remain voxel objects and can be damaged.

Five separate armoured main magazines are retained, with 80 shells per mount.
No outside control console replaces the internal operator. External service
points remain available for the existing one-use / 60-second gun repair.

## Log-driven collision / recovery changes

- Ready-use lockers are placed away from each ship's boarding exit. Existing
  logical ammunition stocks and replenishment remain unchanged.
- Steam cargo and magazine routing has larger capsule-clearance margins.
- Native clearance/sweep checks use the real controller bounds/offset, matching
  the shape that Move() actually moves. Route-block logs include collision flags,
  controller centre and foot offset for the next battle audit.
- Local escape search keeps its bounded graph between frames, cancels stale
  damage/start snapshots, and shares one roughly 4-ms search slice per frame
  across ships. The slice is checked between graph nodes, not a hard cap on an
  individual native physics query. Accepted adjacent links are retained instead
  of quadratically recasting every possible route-simplification chord.

Validation: compilation, turret aperture/headroom/stair/seat checks, 49 major-duty
routes, steam profile/refit checks and existing pump/stair/hull regressions.
No native gameplay/visual test has been performed after these changes. New
v275 logs are needed to verify dynamic doorway clearance, the remaining stalled
routes and the actual frame-time improvement.

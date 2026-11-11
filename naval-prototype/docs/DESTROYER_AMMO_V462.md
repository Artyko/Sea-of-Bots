# v462: protected destroyer ammo locations

Log records Ready ammunition explosion events near bow/stern guns, so reserve-rack relocation alone did not address those explosions. Destroyer ready lockers moved from exposed gun-deck positions to hold floor at x=-2.5 m, z=30/6/-10 m, in boiler gaps. Their logical per-gun stock association retained. Remote locker explosion removes associated ammo, but only calls direct permanent gun destruction within 6 physical metres; actual area blast remains.

First four reserve racks moved to centreline z=30/-32/6/-10 m, retaining fore/aft balance and counts; other resource racks retain previous positions. New ready lockers included in destroyer navigation blockers. Existing shared cargo layout supplies rack positions to physical construction/routing and pickup selection.

Build, DestroyerAmmoPlacementChecks (reserve margins, boiler/turbine overlap and side aisle clearance), StairTrafficChecks and diff check passed. Native route/repair access and explosion propagation need gameplay verification; no blanket guarantee all routes clear.

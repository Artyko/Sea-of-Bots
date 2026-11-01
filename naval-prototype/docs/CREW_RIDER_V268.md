# v268 crew passage, repair and rider review

Latest log shows repeated Returning/Going to pump/Going to fire stalls around local (0.4..1.5,0.85,-2.3..-3.9) on player ironclad, plus enemy firefighting-water stall. Empty nearby diagnostics do not prove the absence of transient character contacts.

Changes: friendly crew controller passage now covers native child body colliders as well as the other controller. Shared capsule clearance queries disregard only registered friendly workers on this hull. Player/enemy/hull collision exclusions are not expanded. Ignore pairs restore through the same helper.

Repair selection now includes nearby missing deck/floor-height cells even when dry (low fallback priority); they consume normal planks and work time. A newly considered patch can be deferred to a nearer available, live worker with a valid work point and no retry cooldown; accepted jobs are not stolen. Ready/bearing crew guns are protected from voluntary repair while flooding is noncritical. This is synchronous selection, not a full global assignment rewrite.

Player support frame cache now requires unchanged sole position and ship position/rotation, avoiding cached results after movement in the same frame. Physical support queries remain authoritative; no forced airborne carry added.

Validation: full build, SteamCombatVitalityChecks, 154 LowerDeckSideRepairChecks, PumpRouteRegressionChecks and 12 opposing StairTrafficChecks routes passed. These are not native movement/contact tests and do not verify all new behavior live. Restart/new battle required; unresolved static equipment/path problems may remain. No claim of complete elimination of slipping or jams without live confirmation.

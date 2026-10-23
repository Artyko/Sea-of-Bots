# v215 repair movement review

Latest Player.log is v214 Galleon/Galleon. Confirmed eight-second stall: feet (-0.17,2.50,0.72), target (-0.14,0.83,0.80), Going to pump. Fresh pure planning from those coordinates begins at upper landing y=2.475 and follows upper flight; the logged execution target was inconsistent with current deck. Exact origin of stale waypoint state is not proven by the log.

FollowRoute now rebuilds from actual feet to the retained job destination if the next nearby horizontal waypoint differs vertically by over one world metre. Replanning is bounded to once per two seconds and excludes intentional ledge recovery. Movement still uses CharacterController and full collision. No direct snapping between decks.

Repair work-site selection now requires Supported(point) as well as reach, visibility, hull bounds and body clearance. This excludes geometrically reachable but physically unsupported points over missing deck/tank cells. Alternate sites/decks and existing rejected-site retry remain.

Full build passes. Existing exact underflight regression and cargo routes pass. No native live test of repair-site raycasts, latest crew failure or task completion yet; no claim of eliminating every observed stall.

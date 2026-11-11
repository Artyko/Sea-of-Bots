# v437: logged crew route stalls

Reviewed latest Player.log v436. Blocked-route records: Novik Returning 83, player Dreadnought Returning 39, allied ironclad Returning 26; smaller counts on repair and plank supply. These are records, not distinct sailors. Repeated cases include Novik below the forecastle lip (feet y=.83, target y=.97 around z=3.46), repeated underflight exits near x=.62, and ordinary deck waypoints with feet ~0.4 m above their ideal plane.

Changes:
- Novik forecastle routing checks actual height as well as longitudinal position. A sailor below the raised deck is routed to the real steps instead of being treated as already upstairs.
- Ordinary intermediate waypoint vertical tolerance accommodates up to .65 m controller/roll height error; horizontal tolerance and final/door tolerance remain strict. A 1.2 m raised-deck difference is still rejected.
- Flat supported movement projects the requested step onto the deck plane when target and actual feet differ by less than .65 m, outside stairs/swimming/jumps. Native collision and normal gravity remain active.
- Route-floor normalization and same-level obstacle search use the same height envelope, preventing recovery from rejecting the exact floor error being corrected.
- Underflight exit accepts a 0.35 m physically reached landing rather than requiring 0.12 m pin precision.
- A blocked returning sailor carrying planks may take an accessible unclaimed local walkway repair before retrying the blocked return. Existing reach, footing, repair claims and plank consumption checks are retained.

Validation: build; LoggedCrewRoutesV437Checks (logged forecastle cases on both sides, deck skin error, wrong-height and remote-arrival rejection); StairTrafficChecks; RouteCornerArrivalChecks; StationArrivalChecks; DestroyerRepairAccessChecks; git diff --check. Native gameplay remains for the next user battle log. No claim that all possible route stalls are eliminated.

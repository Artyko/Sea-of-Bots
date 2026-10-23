# Remaining stair blocks v226

Latest accessible battle still runs v224 (not installed v225). The v224 stair fix is therefore exercised: repeated recovery loops persist at feet near (-0.1,-0.81,0.5) in both Galleons, particularly Carrying repair planks. Prior changes were insufficient. The exact physical blocker is not recorded in that log.

Source mismatch corrected: ClearBody and NavigationClear used fixed radii 0.38 m while native movement uses the sailor CharacterController; escape offsets assumed radius 0.45 m. They now use the actual controller radius in world space (with fallback if absent), matching capsule query endpoints to that radius/height. Lateral recovery exits keep actual radius plus 0.30 m beyond the flight edge. A failed recovery prefers the opposite side on the next attempt. Successful completion clears that preference.

The movement watchdog no longer resets simply because a new stair/stale-deck route was generated. Bounded physical-block reports include sailor ID, task, position, target, actual radius/height and up to four nearby collider names/positions. These are nearby candidates, not conclusive contact-normal evidence. Hull/crew/player collision policy and teleport-free walking are retained.

Validation: full build; actual-radius exit bounds for radii 0.35/0.5/0.8 at scales 2/3/4, both sides; logged recovery cases, 3514 task routes, 656 cargo routes and eight opposing stair routes pass. Native controller queries and executed escape were not gameplay-tested. Next battle must run v226; if blocks persist its added diagnostics are needed rather than claiming the problem solved.

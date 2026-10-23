# Stair recovery loops v224

Latest Player.log runs v223, including Galleon/Galleon. Enemy crew repeatedly logs underflight recovery while carrying repair planks at feet near (-0.05,-0.80,0.50), targeting side exit (-0.71,-0.80,0.50). Both teams also show recovery on the middle landing near (-0.20,0.85,0.53). This confirms repeated recovery attempts; it does not identify every sailor individually or prove all six are immobile.

Two shared defects addressed:
1. Stacked-flight classification tested upper underflight before recognizing normal lower-flight traffic/landing. Needed now exempts actual NavalStairTraffic passages on either flight.
2. Lateral recovery used generic local-space arrival radius 0.28. On Galleon this is 1.12 m, allowing removal of the side exit before the full capsule clears the flight. Dedicated recovery arrival requires horizontal error below 0.12 world m and vertical error below 0.35 m. Two explicit exit waypoints remain protected until completed; repeated stair/stale-deck replans and generic obstacle-waypoint insertion cannot replace them mid-exit. Existing blocked/timeout handling still replans, without teleporting or disabling hull collisions. Release/new plans reset recovery state.

Validation: full build; exact logged middle-landing false-positive tests, Galleon premature-exit regression, existing underflight cases for all profiles/both galleon flights; 3514 directed task routes, 656 cargo routes and stock checks, eight opposing stair routes and capsule ascent/descent approximation pass. No native Unity movement test. Fresh battle log is needed to establish that the repeated loop is gone and reveal any remaining physical obstruction.

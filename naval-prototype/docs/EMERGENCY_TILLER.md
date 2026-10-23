# Emergency tiller — v202

All ship profiles build a voxel wooden crossbar and iron stock in the aft upper maintenance hold, x=-1.2 z=-4.9, forward of the stern. This uses normal hull decoration voxels, attachment/damage batching and repairable mechanism rules. A damaged stock disables emergency steering; minor damage is repairable, severe destruction is not, as for the helm.

When the main helm fails, station zero moves to the emergency tiller post; players approach and press E, and crew route to the new post through existing supply-aware routes. Switching mode exits a player's current station rather than teleporting to the other post. Returning main-helm functionality restores normal operation. Both steering systems damaged means no steering. Yaw rate with the emergency station is one third, for player and AI ships. Sail propulsion and inertia remain independent.

Full mod compilation against game assemblies passed. Position clearance, native crew movement and visual damage require live testing; no native playtest performed.

## v203
Helmsman post retargeting happens before worker movement in the same tick. Readiness is checked against the current station location, so a stale previous-frame helm flag cannot steer remotely during the switch. Tiller hardware moved 0.9 world metres behind the operator instead of 0.55. AllCrewRouteChecks now includes the emergency post on each profile: 3514 directed routes pass, alongside stair traffic and underflight recovery. Native game navigation still requires playtesting.

## v204
Latest log was v202 and repeatedly shows ledge recovery from (-1.20,1.05,-4.90), the tiller post, about 0.9 m above the middle deck: operator was climbing the tiller hardware. Hardware rear clearance increased to 1.3 world metres. Return detours now use the actual upper/middle/lower deck, recognize both stair flights, and require support, body clearance and swept path clearance before insertion. Native confirmation still needed.

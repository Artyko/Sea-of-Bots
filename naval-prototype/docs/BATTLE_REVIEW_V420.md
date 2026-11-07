# v420 — end-of-battle route recovery and aircraft spacing

Latest log showed repeated A/Y turret exits after the sailor had already reached supported exterior deck, plus repeated firefighting-water and maintenance route failures across classes. Exterior exit completion now accepts a nearby supported endpoint and releases the worker at their actual feet position. It does not snap them to an unreachable outboard pin.

All routed crew duties share an eight-entry, ten-second failure memory. Replaying the same blocked segment from the same one-metre neighbourhood immediately returns the normal Blocked result, letting the existing duty handler choose another approach, source or work point. Changed hull revision, a different segment/start or expiry permits retry. The memory never disables collision or teleports a sailor.

Removed duplicate Dreadnought exhaust risers and middle-deck crosspieces. Two vertical funnel trunks remain; their connections to the fireboxes now stay in the bottom hold. Steam propulsion/pump mains remain part of their existing functional systems.

Aircraft spawn intervals increased to 125 m laterally and 160 m longitudinally, with 25 m altitude spacing. Separate 45 m attack lanes remain during the dive, and proximity steering within 75 m reduces convergence. These are avoidance heuristics, not a guarantee against collisions after combat damage.

Validation: build; RouteFailureMemoryChecks, TurretDoorChecks, AirRaidChecks. Dynamic movement and geometry still require player gameplay validation.

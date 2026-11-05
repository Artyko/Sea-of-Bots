# v302 analytical machinery-route audit

SteamAllRoutesChecks enumerates 2592 directed trips between helm, secondary guns, hold pumps, all cargo pickups and crew standby positions on the ironclad and Dreadnought. Each generated segment is checked against the machinery exclusion model. Additional checks cover overhead steam-main clearance and shaft protrusion aft of the engine exclusion. Existing pump/stair and steam service-route regressions also pass.

Corrections: Dreadnought boiler uptake risers crossing the middle deck are included in static avoidance; shaft output starts within the engine bed and the screw height is profile-relative, preventing the smaller steam profile's shaft lying above its deeper hold floor.

Scope: named intact-profile service routes and modeled solid machinery envelopes. This is not exhaustive native collider sampling, moving turret/door sweep verification or proof that every damaged/tilted configuration is navigable. Boiler repair work points, arbitrarily chosen fire sources and destruction-generated gaps are dynamic queries and still need gameplay-log verification. No live FPS/collision result is claimed.

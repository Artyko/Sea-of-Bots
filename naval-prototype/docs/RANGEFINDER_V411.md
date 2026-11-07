# v411 — manual rangefinding and convoy escort

Manual steam gun and main-battery operators see an approximate target range sampled every 2 seconds, rounded to 10 m, with ±max(10 m, 2%) error. It requires an active allied helmsman, a functional main helm and at least 65% of the bridge structure. Targets are chosen within 25 degrees of barrel bearing, excluding friendly convoy members. No automatic laying is applied. The adjacent sight range solves the existing 9.81 m/s² ballistic equation to local sea height with actual muzzle position, world barrel direction and firing platform velocity; spread, future target motion and wave variation can still change the impact.

Escort stations rotate around the current flagship heading instead of remaining fixed in world coordinates. The dedicated screening escort retains its existing threat-response role. Both player-side choices retain their fleet affiliation.

Validation: build, RangefinderChecks (ballistics and rotating station geometry). Gameplay validation is performed by the player.

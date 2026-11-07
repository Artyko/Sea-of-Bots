# v426 — paired echelon air attacks

Waves remain 5/6/6. Aircraft are grouped by stable pair IDs; each pair shares a target selected at spawn, and surviving members retarget together after target loss. The odd fifth aircraft acts as the final element's single leader.

A wingman starts 85 m aft, 28 m offset and 4 m above its leader. Successive pairs are 280 m apart longitudinally, with alternating lateral echelon offsets and 18 m altitude steps. During distant approach the wingman follows its leader's relative station; near the attack run it follows its own bombing trajectory. Different pairs retain their spacing from the staggered approach. Proximity steering now acts inside 30 m rather than 75 m, so it does not tear the intended pairs apart.

The former 45 m-per-plane cross-track bombing lane was removed. Bomb aim offsets are only ±3 m across the target and up to ±8 m along it, transformed by the target ship's heading. Existing random bomb dispersion remains; this removes formation-induced systematic misses rather than guaranteeing hits.

Validation: build, AirPairFormationChecks, AirRaidChecks. Dynamic pair following, damaged aircraft and FPS require gameplay verification.

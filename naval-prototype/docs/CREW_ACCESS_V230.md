# Crew workplace access v230

No sailor model/controller size is changed here. Latest logs report radius 0.80 m and height 2.50 m; old access/layout rules assumed smaller capsules.

Corrections shared across ship types:
- Standby X is now +/-1.65 on larger ships, leaving full capsule clearance before ballast inner wall; sloop waits in central aisle at +/-0.1 instead of pinching between crates and ballast. Runtime callers use profile-aware overload.
- Sail cleat moves from 0.95 to 1.25 world m from work post. The 0.6 m crossbar no longer overlaps a 0.8 m radius capsule; work reach remains 1.85 m.
- Pump approach advances 0.65 m rather than 0.4 m beyond the nominal post, clearing the scaled pump front face. All pump decks share it.
- Low transverse bilge pipes remain voxelized/damageable but use trigger colliders, avoiding physical stepping onto the raised pipe at the emergency tiller. Projectile and G queries include triggers; ordinary controller/clearance queries ignore them. No hull/deck/rim collision removed.
- Sloop directional stair lane offset is capped by actual 0.8 m radius plus 0.05 m margin to the stair edge. Friendly passage still handles meeting sailors because the flight cannot fit two full capsules side-by-side. Brigantine/Galleon retain their fitting lane spacing.

Native audit added after spawn for both teams, every profile: uses a live crew controller radius/height, checks one target every 0.15 s, then disables itself. Targets include main/emergency steering, all guns, sail station, pumps on each maintenance deck, every cargo pickup (ammo/boards/parts), standby positions, sea-ladder deck exits and sampled maintenance points. Logs Access audit blocked with target/support/near obstacle and a final checked/blocked count. Characters are excluded from fixed-geometry obstruction reporting. This is a diagnostic point audit, not proof of full-path clearance or all future damaged-hull repair positions, and it does not automatically relocate targets.

Validation: full compile; profile-specific capsule-spacing tests, 3514 directed routes, 656 cargo routes/stock checks, eight opposing stair routes, underflight and ammo-priority tests pass. The native audit has not yet been executed here; a restarted game/new battle must provide its results before claiming universal accessibility. Dynamic damage, recoil and future leaks still use live movement clearance/recovery.

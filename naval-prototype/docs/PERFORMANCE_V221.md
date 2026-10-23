# Performance v221

No post-v220 battle exists in the currently accessible Player.log: latest measurements remain v218. This pass removes code-identified redundant warehouse work; it does not claim measured FPS improvement.

- CargoCrate damage validation is invalidated by its own source cells through CargoCollision.SourceChanged, including after exact-collider fallback. Hull damage elsewhere, sail movement and consumption in other crates no longer trigger a full shell/content scan in this crate. Internal inventory visibility changes do not mark external damage dirty. Missing/detached shell or visible contents still destroys stock on the next refresh. The 0.15 s background scan and synchronous Count/Take/Put refresh remain.
- Direct array lookup for deck gun crates replaces full warehouse scans. EmptyGun visits deck crates in the same original gun order. Logistics candidate selection reads target ammo once per selection rather than for each source. No priority, stock thresholds or dispatch timing changes.
- Cannon parts interactions reject enemy, inactive/dead player and far supplies before scanning neighbouring supplies. E-key is checked before proximity/kit queries in Update. OnGUI computes prompt only on Repaint. Within-range nearest-supply comparisons and original distance tests are retained.

All profiles/both teams share these improvements. No hull/deck/island colliders or executed routes changed in this pass.

Validation: full build; 3514 task routes, 656 cargo routes/stock tests, 656 graph/reference equivalence comparisons and graph reuse, logged underflight cases. Actual CargoCollision mocked-Unity tests additionally verify external damage notification and suppression of that notification during internal stock depletion; existing damage/detachment fallback, hit resolution, empty-crate disappearance and scale checks pass. No native gameplay/PhysX/FPS benchmark performed.

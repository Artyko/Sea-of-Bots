# v463: main battery target following

Manned combat-ready turrets no longer auto-request access for unrelated passing crew. Explicit assigned-operator access requests and player access remain. This prevents incidental NPC traffic from recalling a loaded/reloading turret to base access yaw.

Convoy capital battery selects nearest opposing non-sinking hull inside main battery range and uses its actual fleet velocity. Existing traverse sectors, physical obstruction checks, ballistic solution, loading and per-operator access remain. Reload still does not reset yaw. Outside convoy existing target input retained.

Build, TurretDoorChecks (updated obsolete deck-only ready-ammo assumption for below-deck destroyer lockers) and diff check passed. Native target following requires battle verification.

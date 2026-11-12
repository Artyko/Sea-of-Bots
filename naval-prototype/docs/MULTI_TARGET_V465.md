# v465: independent weapon targets

Convoy battles: each capital turret independently chooses the nearest living opposing non-sinking hull in its bearing sector and main battery range. Each ordinary gun independently chooses nearest opposing hull in its own CanBearOnShip sector/range. Target-specific relative velocity and aim reset maintained per weapon. Reload tracking and physical shot/obstruction checks retained. Outside convoy original target behaviour retained.

Crew bearing/opportunity checks can recognize another target for a gun rather than treating only the fleet's primary target as an opportunity. Player-controlled weapons remain under player control. Single ironclad main mount retains its existing shared target input; ordinary guns and capital mounts support multi-target selection.

Build, TurretDoorChecks, diff check passed; native concurrent engagements need gameplay verification.

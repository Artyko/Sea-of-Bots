# v443: floor-only navigation rejection

Reviewed latest battle movement failure records across all ships. Repeated dreadnought Returning/plank supply blocks report Below only, plus short station alignment failures. NavigationClear now projects sweeps onto the deck plane only when both endpoints have actual local support, are outside stairs and differ by <0.65 m. Endpoint support/body checks in route recovery remain active. This mirrors actual supported flat movement and avoids downward casts into the supporting deck. Stair/ladder/different-deck sweeps remain 3D.

Supported station alignment accepts horizontal error <=0.65 m rather than 0.45 m; the general remote-anchor limit, turret seat checks and vertical bound are unchanged. Actual support is still required by AtStation.

Build, StationArrivalChecks and StairTrafficChecks passed; git diff --check clean. Native floor-sweep correction requires gameplay verification; not all possible blocked/destroyed routes are certified fixed.

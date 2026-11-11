# v435: destroyers may engage convoy escorts

Removed the ConvoyTarget early return that forced slots 4–6 to attack the living flagship exclusively. Interceptors now select among living opposing convoy ships using squared distance, a modest flagship preference (0.85) and broadside aspect preference (up to 0.85). Existing 0.8 incumbent hysteresis, faction and sunk-target filters remain. Guns and torpedo tactics consume the same selected target; friendly torpedo safety checks remain intact. Convoy defensive targeting is unchanged.

Validation: build, InterceptorTargetChecks (near escort beats distant flagship, equal-distance mission preference, broadside preference, hysteresis), git diff --check.

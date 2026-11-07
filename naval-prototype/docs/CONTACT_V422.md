# v422 — vertical separation from deep wrecks

The latest available log was v420 and did not record contact pair/depth details. Code inspection found the generic hull check accepted vertical root separation up to 9.5 * max(scale/2), about 40.5 m for Dreadnought/Novik. A seabed destroyer therefore remained a planar collision obstacle long after its hull was below the passing ship.

Known hull pairs now compare world-space hull-only vertical intervals from hold bottom to rim, with each ship's actual scale, heel and pitch. Own start/end intervals include a conservative intermediate rotation allowance. Vertically disjoint hulls skip planar contact, damage and impulse handling. Separate mast/rig contact remains intact; shallow wrecks can still physically obstruct a ship.

Validation: build, HullVerticalContactChecks and HullContactGeometryChecks. Full runtime crossing of a wreck remains for player verification.

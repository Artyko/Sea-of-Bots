# v418 — convoy spacing, contact footprint and bridge doorway

Convoy starting offsets changed from ±105/50 m to ±140/70 m for the side escorts, and from 150 to 200 m ahead for the lead escort. Following offsets are derived from the same spawn positions.

The old generic contact OBB used 6.45/12.75 nominal half-extents scaled by ship scale /2, larger than the steam hull's 2.35/6 local half-extents. It remains a conservative broad-phase only. Close candidates now use a cached convex contour sampled from each ship definition in absolute metres, with translation/rotation sweep and binary contact refinement. Ram damage contact points use that contour instead of old box corners. This is an intact-hull footprint, not a per-damage voxel collision replacement.

Reduced doorway lintel depth from .4 to .1 world metres, adding 30 cm head clearance below the bridge upper floor.

Validation: build, HullContactGeometryChecks and ShipContactChecks. In-game contacts, especially tilted/damaged wrecks, need runtime confirmation.

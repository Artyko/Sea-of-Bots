# v445: hull patch area versus per-cell hand reach

Latest v444 log: ironclad repair at (-2.35,.66,-4.63) restored 7 cells, followed by unavailable flowing neighbours at y=.99 and1.11. Radius alone did not solve per-cell reach filtering.

An accepted hull repair can restore claimed hydraulic-shell cells within 3 m on the same face (same thin dimension axis, plane separation <=0.35 m), even when individually outside hand reach. The work centre still requires real access/reach. Other surfaces, equipment and rigging retain individual reach checks. Original radius, claims and CanRestoreCell logic remain. This implements a patch area rather than a separate arm-reach job per voxel.

Build, RepairSurfaceChecks, RepairRadiusChecks, diff check passed. Native gameplay result remains to verify.

# v460: mission counts sinking ships as lost

Mission fleet counters exclude both Sunk and SinkingDanger, matching HUD status. Final lost counts derive from those counters. Flagship loss and surviving-interceptor outcome checks use the same predicate; physical sinking simulation is unchanged. A sinking flagship can now end the mission before physical submersion. Counts are based on current state before result capture (not a permanent per-hull loss latch).

Reviewed v459 log: 144 completed repairs, 211 blocked route records, 141 unavailable work-site records. Most repeated blocks are ironclad/Novik Returning; dry steam mechanism failures appear among unavailable work sites. These records are not unique sailors. No movement fixes claimed in this release.

Build, MissionShipCountChecks, diff check passed; installed hash verified.

# v455: fair recovery search and submerged grid links

Latest log retains repeated crew blocks and active inaccessible hull repairs. Found global recovery budget gave one turn to the first caller every frame, potentially starving other pending workers. Replaced with rotating worker queue, stale caller expiry after two frames, four grants per frame, 0.5 ms slice and four expanded nodes per turn (bounded total work). Pure fairness test covers twenty concurrent workers and stale removal.

Removed remaining unconditional deck-gap rejection in grid node eligibility and target reconnection when the point is already recognized as fully submerged swimming navigation. Native body/path clearance remains. This completes the v450 correction inside the grid itself, not just actual movement.

Build, RecoveryFairnessChecks, StairTrafficChecks and diff checks passed. Native full-battle access and individual dry equipment obstructions still need verification; no claim every obstruction eliminated.

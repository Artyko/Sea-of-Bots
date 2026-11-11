# v447: refresh live repair patch

Reviewed v446 log: adjacent hull jobs and completed patches of 1–10 cells coexist. Completion still read only the claim snapshot created before supply/navigation, so later damage in the same 3 m area could not participate.

Completion now snapshots the current damaged-voxel list in the 3 m repair sphere, accepting own or unclaimed cells and retaining CanRestoreCell. Other workers' claims are not stolen. Mutations happen only after snapshot collection. Reservation exclusion around another repair job now matches the 3 m radius instead of 2 m. Completion logs include initial_claims and patch_candidates to distinguish access/ownership/eligibility from actual restoration. Build, RepairRadiusChecks and diff check passed. Native effectiveness is not yet verified.

# v438: repair patch self-occlusion

Latest log contains completed patches of 1–2 cells alongside 13–26-cell repairs, and inaccessible Novik floor holes near y=.13,z=4.19..4.43.

Repair completion now snapshots all reachable claimed cells before restoring any, preventing newly restored colliders from occluding remaining cells in the same operation. Three-metre radius, original sight/reach checks, claims and restore eligibility remain. Added a fallback radial search (0.8–4 m, 12 directions) for supported Novik repair sites on its three real deck elevations; runs only after existing candidate searches fail. No collision bypass or remote repair.

Validation: build, RepairRadiusChecks, DestroyerRepairAccessChecks, git diff --check. Native collision self-occlusion and site access need gameplay verification.

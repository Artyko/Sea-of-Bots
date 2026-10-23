# v199 — gun station arrival, firing passes, taller smoke

Latest Player.log ends 22 October 2026 16:34:23; performance records identify Galleon/Brigantine, not two galleons. Guns fired repeatedly. The log does not record each sailor's gun-station approach and cannot prove every observed rotation's cause.

## Crew
A concrete large-ship defect exists: final route arrival allowed horizontal distance 0.14 local units (0.56 world metres on galleon) and vertical difference 0.3 local, while station anchoring requires total world distance <=0.4 m. A returning gunner could repeatedly finish, fail anchoring, then route back to the same post. FollowRoute now uses AtStation(target) on the final changing-post segment. The same condition ends walking and permits station anchoring on every profile; maintenance reach and stair transitions retain their separate checks. No enlarged teleport radius.

## Helm
Successful broadside passes used to end after 16 seconds regardless of shots, and a fixed bearing anchor could lose a moving enemy. Pass heading now follows the current broadside bearing with a 12-degree/second command limit. Successful passes can continue up to 45 seconds. No shots for 12 seconds, loss of sector for 6 seconds, sustained unproductive turning, destroyed battery, or range loss still causes recovery/separation. This reduces unnecessary close manoeuvre cycling without suppressing retreat, shore recovery, or ramming priority.

## Smoke
Fire cube rise speed 2.4 -> 3.6 m/s. Twelve-second lifetime and 384 cube cap unchanged. Base puff size 0.8 -> 1 m keeps 0.9 m vertical spacing connected. Height-dependent charcoal-to-light-grey gradient follows the new rise speed; no white tint or extra particles.

## Verification
Full mod build; exact large-ship station threshold mismatch regression; successful 25-second pass and dry-pass separation; nine simplified two-ship battles, 637 total shots, inertia/rudder and surviving-battery variants; 3321 task routes; stair traffic; smoke continuity and fixed capacity checks pass. The battle test permits sustained productive fire instead of requiring a needless separation in every scenario. These are CPU/headless tests, not a native game playtest or visual smoke review.

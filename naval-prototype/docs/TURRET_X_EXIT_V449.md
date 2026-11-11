# v449: X turret ladder transition

Reviewed v448 battle. Repair completion now logs patches of 19,21,27 restored cells, including patch_candidates=27 versus initial_claims=22; refreshed area repair works in those cases. Remaining route blocks persist across all classes; no claim all resolved.

X exit logic accepted a broad descending landing but subsequent vertical movement required <=0.4 m axis error, while vertical arrival required <=0.12 m. Added strict axis alignment before advancing to a vertical segment and widened the continuous ladder grip motion envelope to capsule radius+0.4 m. Only X scripted access path is affected. Build, station/radius checks, turret door geometry checks performed. Native Leaving turret exit must be verified next run.

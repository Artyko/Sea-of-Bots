# v434: committed underwater hull patch

User explicitly requested finishing an already-started hull repair regardless of air or estimated escape route, even at the cost of sailor life.

CanFinishPatch now accepts every active hull patch with a positive remaining duration of up to four seconds. ActiveHullPatchSeconds remains nonzero only after repair actually started, while standing at the job with no route remaining. Drowning damage/death remain active; this does not make the sailor invulnerable or finish repair after death. The general 22-second escape threshold and the separate budgeted approach exception remain unchanged. Existing sunk-vessel evacuation policy remains.

Validation: build, HoldSafetyChecks (started repair at zero air/long escape, no active patch still rejected), git diff --check.

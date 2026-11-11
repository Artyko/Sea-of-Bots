# v433: underwater approach and repair air budget

Reviewed latest Player.log, v431: Dreadnought lower-hold repair assignments recur near side holes around (1.90,0.22,4.13), while Hold evacuation records report 22.0 seconds of air. Existing records omit ship/sailor/task on evacuation, preventing definitive attribution of each departure.

Code issue confirmed: the repair exception required route.Count == 0 AND work > 0. A sailor approaching a nearby patch or on the arrival frame before the first repair tick could lose the job at the 22-second threshold.

- Retained the 22-second general escape threshold and existing active-patch completion exception.
- Added a nearby-hull-patch exception only when already carrying boards, the target remains damaged, approach <=3 seconds, remaining repair <=4 seconds, and air exceeds actual estimated exit +2 seconds contingency + approach + repair (minimum escape reserve 8 seconds).
- Blocked approaches (stuck >0.8 seconds), waiting/returning states, stock collection and distant routes do not defer evacuation.
- Added throttled continuation records and evacuation decision records with ship, sailor, prior task, actual air, approach, work and exit estimates.

Validation: build; HoldSafetyChecks including approach/arrival grace, insufficient air, blocked target and long exit rejection; git diff --check. Gameplay confirmation requires the next run.

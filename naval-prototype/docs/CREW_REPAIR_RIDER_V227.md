# Crew repair and rider follow-up v227

Latest v226 log: repeated repair assignments and repeated blocked Returning sailor -1426108 at Enemy Galleon emergency tiller. Reported feet Y roughly 1.03-1.05 vs requested middle floor 0.825 (0.8-0.9 world m). Block report radius 0.80, height 2.50, nearby list empty. This suggests a reference-height/contact discrepancy; the exact cause is not established by the log. Older logs did not explicitly record completed patches, so assignment count is not proof of repair completion.

Changes:
- Crew deck carry rechecks root-to-actual-controller-sole offset when it differs by more than 0.05 m from stored spawn measurement. Reconstruct previous local feet from that physical sole before rigid carry; significant corrections log sailor ID and both offsets. This is recalibration from current bounds, not teleport through an obstacle or a new raised-post tolerance.
- A final repair approach cannot finish on proximity alone while the assigned hole is unreachable. The carried-plank delivery leg uses that same reach-gated arrival. Existing bounded obstruction/replan policy remains. Completed repair reports restored cell count and whether a board was used; a zero-cell result is visible rather than labelled successful by inference.
- If the player sphere support probe fails at trim/seams, five bounded vertical rays sample centre and four points at 70% of actual capsule radius. They still require actual native ship hits with upward normal. No temporal support grace/airborne latch added. Battle telemetry logs cumulative fallback hits, sole and radius to check whether edge probes are being used.

Validation: full build; existing pure route/stock/underflight/station-bound regressions pass (3514 task routes and 656 cargo routes). These do not simulate native capsule bounds recalibration, physical repair reach or rider edge contacts. Fresh live v227 battle is required; do not claim all reported problems are fixed without it.

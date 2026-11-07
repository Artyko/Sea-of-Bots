# v407 — Dreadnought crew access

Reviewed v406 Player.log before the next game launch replaced it. Repeated entries included aft AA3/4 approach blocks at x=±1.10..1.30, y=1.64, z≈-2.51; bridge approach targets x≈±.87; aft platform support contact near (1.15,1.87,-3.26); and central-stair stalls while carrying supplies.

## Corrections

- Separated the ladder rung plane from the capsule trajectory by 1.26 m, moved the approach and landing together, extended the upper connector, and gave both endpoints precise arrival tests.
- Moved rear platform columns between transit lanes and front balcony supports to the outside edge. Their widths retain physical support while clearing 0.8 m capsules in established lanes.
- Updated the capital weather-deck navigation graph to the actual bridge width. A displaced start can leave the planning box that contains it before continuing around other obstacles.
- Stair repair sites use real tread elevations. Both ends of each flight have explicit routes to a repair tread. A worker can select and repair from a supported step; nearby access repair also includes missing treads.
- Obstacle recovery permits retreat from an initial contact with a convex primitive only when moving away and the destination capsule is clear. Hull collision remains handled by the native controller.

## Validation

Build and checks: CapitalAADeckChecks, StairRepairAccessChecks, StairTrafficChecks, RepairDeckSeamChecks, RepairRoutesChecks, CrewMovementChecks, TurretDoorChecks. These validate geometry and route calculations; the next gameplay run must confirm dynamic collision behaviour.

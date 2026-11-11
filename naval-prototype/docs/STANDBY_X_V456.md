# v456: standby dispersion and X ladder alignment

Steam standby points now span ten longitudinal rows rather than four repeated local pockets; dreadnought lanes moved to +/-1.15. SafeStandby checks dry original positions for support, body clearance and 2.2 m spacing from other assigned home points. Relocation search expanded to forty distributed positions per deck and also checks separation, avoiding all idle sailors selecting the same nearest safe point. Existing wet-floor policy retained.

X ladder movement now handles both vertical travel and horizontal alignment to the actual ladder axis within a bounded 2.4 m transfer zone. Previously purely horizontal alignment fell back to native walking and could collide with ladder structure before scripted climbing began. Axis calculated without per-frame list allocation. Door readiness and ordinary non-ladder movement retained.

Build, TurretDoorChecks and RecoveryFairnessChecks passed. Removed obsolete single-global-slice assertion from turret geometry test; fairness behaviour has dedicated coverage. Native entry/exit and occupied standby layout remain to verify.

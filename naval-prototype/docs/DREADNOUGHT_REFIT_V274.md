# v274: Dreadnought geometry and last-battle audit

Inspected Player.log: v272 Dreadnought versus Ironclad Corvette. No exceptions.
Seven stalled-route events on the Dreadnought, six on the corvette, plus five
route-block messages. Dreadnought fired 84 main-calibre shells: A=22, P=22,
X=20, Y=20. Q not firing is consistent with the port-side engagement and wing
sector limits. Its battery remained 5/5 functional and 5/5 staffed. FPS telemetry:
22 samples, minimum 49.5, median 59.0, maximum 59.8; these are the old build's
observations, not a v274 performance claim.

Changes:
- Dreadnought slabs now have the corvette's 1 m world thickness. Hold floor is
  adjusted to retain 5.6 m actual lower-hold headroom; middle-deck usable height
  becomes 3.8 m, matching the corvette. Walking surfaces and guardrail fittings
  stay consistent in world units.
- Ground-level standing operator consoles replace chair animation beside closed
  turrets. Posts and panels sit outside the full rotating roof's swept circle.
  The earlier .42 local navigation half-width missed diagonal roof corners and
  explains the logged port-battery detours; use conservative .58 radius instead.
- Rebuilt bridge at z=3.20 with full-height rear/side entrances, observation slits,
  roof and corvette-height wheel. Shift forward leaves a real passage ahead of
  wing turrets. Compact signal towers replace cross-shaped yards/tripods.
- Inner stern turret X is raised .55 local / 3.3 m. Its -6 degree depression stop
  keeps both tubes above the aftmost Y roof; own-ship line-of-fire checks remain.
  Ground service console remains the cannon repair access point.
- Player start / F8 return moves amidships, offset from the open companionway.
- Gun ammunition supply endpoints use the gunner's real station rather than the
  offset virtual ready crate beside a funnel. Boarding ladders move to the free
  midship bay; steam hull/rail openings now use the actual narrow outline.
- Close stalled intermediate corners may advance only when the next entire
  capsule path is supported and clear. Strict ordinary arrival remains unchanged.
- Steam main height stays 3.6 m in world units. Real feed elbows join detached
  pipe islands flagged by the old structure audit, rather than letting them be
  removed as unsupported on spawn.

Validation: full compilation; DreadnoughtChecks (49 duty routes plus swept-console,
roof-clearance, headroom and spawn checks); DreadnoughtRefitChecks (logged crate
escape, controls and pipes); SteamV232Checks; SteamRefitChecks; SteamHullFitChecks;
StairTrafficChecks; RouteCornerArrivalChecks; PumpRouteRegressionChecks.
No native game battle or visual validation was performed after these changes.
The next v274 log is required to verify the dynamic stalls and physical audit.

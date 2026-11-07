# v412 — crew route regressions from latest v410 convoy log

Observed repeated Dreadnought bridge wall targets (x=.92,y=1.63,z=3), premature upper-stair exits, an aft-AA route replaced by an elevated waypoint under the platform, and Novik operators obstructed near their gun shields. The captured log is v410; v411 rangefinder/escort changes remain included.

Corrections:
- Bridge flight classification checks its actual sloping surface rather than the entire enclosing volume, so weather-deck poses beside cabin walls are no longer treated as stair poses.
- Bridge treads use tighter vertical arrival, preventing lateral departure below the upper floor.
- Raised AA/bridge routes retain their dedicated access paths during initial planning and return replanning.
- Ledge recovery uses the real navigation capsule and clearance rules; its old 0.38 m radius was smaller than the logged 0.80 m crew radius.
- Novik gun service posts moved from 2.3 to 3.0 m behind the mount to clear the physical shields.

Validation: build; BridgeFlightRegressionChecks reproduces the logged wall/landing cases; CapitalAADeckChecks and StairRepairAccessChecks pass. Dynamic path failures at machinery and damaged cargo still require a new gameplay log; local checks cannot establish that every battle blockage is eliminated.

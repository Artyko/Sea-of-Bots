# v269 review

Latest battle shows repeated enemy Returning stalls at (-1.52,2.05,-2.84), player lower-hold repair stalls near (-1.22,-0.74,2.70), and aft firefighting stalls. These remain unresolved by this visual patch; empty nearby reports do not establish the blocker.

Player main battery destruction is explicitly linked to Main battery magazine detonation and permanently destroyed cannon. Log does not establish the original ignition trigger; no explosion probability changed.

Steam ocean clipping previously retained generic 3.0 half-width and sailing end planes despite corvette beam 2.35 and curved ends. Side distances now use actual steam hull outline, with bracketed bisection at edge intersections; old sailing end planes are excluded for steam. This remains triangle/polygon-based mesh clipping, not an exact curved-surface Boolean. Visual fitting boxes are constrained to minimum beam at both Z ends, preventing decorative runs from protruding on taper. Geometry outside that lateral envelope is trimmed; physical armor/equipment is not changed.

Full build and SteamHullFitChecks pass. No native rendering/playtest performed; need a fresh battle to inspect water seams and identify any remaining unidentified black protrusion. Crew stalls require further native diagnostics and are not claimed resolved.

# Crew access audit v284–285

Evidence: latest available battle is v282, not v284/v285. It contains a raised-X-turret exit stall, repeated return/plank routes and long raw frames (up to 934 ms). No live verification has been performed after the changes.

Changes: descending capsule tolerances follow actual stair overlap; nearby intermediate stair cells qualify as board repair targets; unsuccessful leak-access holding is bounded to 2 s with a 12 s retry interval. Repair-site exclusion now uses a 2 m world-space radius instead of 1.2 local units (7.2 m on Dreadnought). Relief main-battery selection cannot conscript the active helmsman or another worker in turret transit. Access homing now raises barrels and tests their volume before rotating, as combat training already does.

Audited helm navigator: its existing recovery already demands actual backward distance and checks stern clearance; no additional speculative steering changes. Crew emergency release retains hysteresis and preserves accepted work. Unreachable tasks retain existing retry behavior; destroyed stair repairs still require reachable work position and supplies.

Limitations: live PhysX contacts, fire-destroyed stair recovery, player/body-part damage CPU spikes and final FPS must be validated in a new battle. Current log cannot prove outcomes of later builds. The 15 ms damage/spall timers and raw-frame warnings are retained for this purpose.

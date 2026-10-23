# Sail control — v145

Approach the wooden cleat by the main mast and press E. Hold W to increase the signed sail setting, S to reduce it through zero into reverse. E leaves controls; C returns the sails to crew automation. At the wheel only A/D turns the rudder. Hull cards show signed sail deployment / surviving sail percentage.

Target setting changes at 40% per second; cloth and actual deployment follow at 30% per second. The setting persists when nobody mans the helm or the wheel breaks. Broken wheels force rudder command to zero, but deployed sail drive, coasting and collision drift remain. Existing hull flooding and sail damage still reduce actual speed. Crew AI sets the same sail target during normal helming/navigation, including reverse recovery; explicit manual control takes precedence until C restores automation. All ship profiles share this implementation.

Cloth voxels gather toward their upper yard and compress vertically to a 6% bundle when furled. Render batch vertices and real voxel transforms follow the same deformation. The support graph retains the original structural connectivity for damage decisions. No extra per-voxel draw calls are added.

Validation: full build and pure sail-state, reverse, damage/speed and interaction checks. No live Unity visual or collision test was run.


## v146 — physical crew sail handling

Crew helmsmen now issue a requested sail setting only. Automatic cloth deployment changes exclusively when the reserved sailor is physically at the sail post; without that sailor, current deployment is held. The ship coordinator sends an eligible nearby spare sailor (or a gunner when needed), excluding the helmsman, active maintenance workers and boarders. One sailor owns the post until the requested setting settles for 0.8 seconds, then uses common next-task dispatch. Repair/fire emergencies release sail handling to higher-priority maintenance. Blocked paths use the shared recovery and retry logic; falls enter rescue. Final ramming attacks retain a sail handler before allowing that sailor to board. Manual player control and C automation remain available. Tests verify requests without an operator cannot alter setting and interrupted automatic handling freezes safely.


## v147 — access and rope hauling

Player.log showed repeated raised-position recovery near the previous mast/cleat post. Sail controls moved to the starboard inner passage (local x=1.35,z=2.4); the crossbar sits 0.95 world metres away from the default stand point instead of 0.6 m, exceeding full-size body plus crossbar clearance. Sail workers choose among nine supported, body-clear stand points with unobstructed hand-to-rope reach. Actual operation requires reach within 1.85 m and deck height within 0.3 m, not exact-point arrival. Blocked approaches try three alternate stand points, with a 45-second task bound and retry delay. At controls, a manual upper-body animation alternates pulling arms/hands and renders a rope through the hand grips; release restores the prior animation mode and hides the rope. Static layout and reach tests pass; live animation and collision testing remains pending.

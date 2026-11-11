# v427 — stern flooding and propulsion drag

No battle telemetry was available in current Player.log, Player-prev.log or output_log.txt during this review (empty/startup logs). No new runtime FPS peak or crew blockage can be attributed from these files.

Inspection found the common translation formula divided forward speed by 1 + additional draft in metres. This turns several metres of added draft into a very large direct speed cut even with intact propulsion. Steam ship translation now uses a first-order game approximation of added displacement resistance: retained speed = (1 + interior water / retained dry displacement)^(-2/9). Dry displacement comes from the existing stability model, in water-volume mass units. No extra structural damage or boiler flooding is inferred from stern water alone; existing branch pressure/integrity and auxiliary emergency throttle still apply. Sail-era translation and steering were not changed.

Cached oil shaft connectivity by damage revision; pressure and control values stay live. Added flood_speed_retention, draft_water_m3 and dry_displacement_m3 to oil plant logs. This removes redundant scans but is not a measured FPS gain.

Validation: build, FloodDragChecks, TorpedoStateChecks and HoldSafetyChecks. Record a new battle to verify stern-flood motion, remaining FPS hotspots and crew paths.

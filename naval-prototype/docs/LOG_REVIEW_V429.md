# v429: battle log review

Observed in v428 telemetry: 20–24 FPS, Crew averages about 8–9 ms/frame; FleetContact intermittent peak 14.8 ms. TorpedoMotion about 0.2 ms/frame. Repeated destroyer Returning stalls occur near stair-to-corridor intermediate points.

Changes:
- Removed the player torpedo warning text; AI evasion remains enabled.
- Contact damage calculates closest hull surface points only when its existing three-second damage cooldown permits an impact. Boarding contact timestamps still update each contact.
- Reused swept-contact polygon buffers, including separate buffers for ships of the same type. Actual poses and collision geometry still recalculate.
- A stalled sailor within 0.4 m of a stair-exit waypoint may advance to the next waypoint only with compatible height, body clearance and a walkable route. No teleport or collision bypass.

Validation: build; HullContactGeometryChecks; ShipContactChecks; StairTrafficChecks; git diff --check. Gameplay/FPS improvement requires a new battle measurement; no measured gain claimed.

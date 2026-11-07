# v416 — flooded standby and air-defence allocation

In the latest v414 convoy log all four functional Dreadnought AA mounts repeatedly reported assigned=0 during the raid. AssignAirDefense returned immediately on the combined urgent-pump/fire/combat emergency flag. It now allocates up to two mounts in emergency and the normal count otherwise, using available hands and preserving active repair/fire jobs and the reserved helm/plant hands.

Idle standby destinations are now reviewed once per second against local water height. More than .60 m of water rejects the lower station; supported, capsule-clear dry points on the same or higher decks are searched. The original station must remain below .20 m water for 20 seconds before return, avoiding wave-driven oscillation. Existing maintenance tasks are not redirected by standby selection.

Validation: build, StandbyWaterChecks and AmmoPriorityChecks. Dynamic staffing and physical access require the next gameplay log.

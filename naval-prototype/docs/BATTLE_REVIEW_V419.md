# v419 — air deadline, X ladder exit and convoy reattacks

Each air wave gains one aircraft: 3/4/4, eleven aircraft and 44 bombs total. The existing random support delay remains 60–240 game seconds; an explicit 600-second maximum is enforced. Aircraft still start 3 km away. This bounds the reinforcement call; extreme stalls or pausing can extend wall-clock travel time.

Raised turret X used generic descending-stair arrival tolerances, accepting a point up to 1.2 m before completing its vertical descent. X vertical segments now require .12 m horizontal and .08 m vertical completion; ladder movement continues down to .02 m error before ordinary walking resumes.

Late battle telemetry showed an interceptor at almost 3 km and increasing separation in repair mode. Convoy interceptors now target the living flagship, keep repair withdrawal within a bounded operating area (turn back beyond 900 m), and resume attacks after 35 seconds when they retain weapons and are not critically flooded or suffering a propulsion flood emergency. A subsequent torpedo launch resets that repair cycle. Other mission types retain the existing repair-retreat hysteresis.

Validation: build, AirRaidChecks, TurretDoorChecks and RetreatChecks. Runtime targeting/exit still needs the next gameplay log.

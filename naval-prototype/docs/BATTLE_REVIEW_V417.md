# v417 — untimed convoy, hidden air support and terminal flooding

Removed the convoy mission deadline and delayed-convoy defeat. Elapsed time is retained only for statistics. Removed destroyer-loss and exit-distance air-support triggers. One hidden random delay (60–240 game seconds) is selected for each convoy battle; it calls the existing three waves from the fixed 3 km approach. The random delay is not displayed or logged in advance.

A ship is additionally marked lost after eight sustained simulation seconds at >=92% flood capacity with >.35 m rim immersion and inflow >1.2 times nominal pumping, or >=75 degrees heel with >=65% flood capacity. Evidence decays three times faster on recovery. Existing full submersion / destroyer capsize rules remain.

Latest log: repeated unavailable repair sites in damaged Novik bow/stern and one central-stair capsule stalled beside a tread. Added farther-inboard end-wall sites and air-budgeted submerged repair options for destroyers. Normal tread arrival remains strict; a stalled capsule may rejoin the next waypoint only on the same flight after live support and capsule-path checks.

Validation: build, AirRaidChecks, LossPolicyChecks, StairTrafficChecks and StairRepairAccessChecks. In-game repair access and dynamic collisions still require the next run.

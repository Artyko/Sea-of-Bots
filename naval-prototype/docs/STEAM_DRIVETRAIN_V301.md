# v301 connected steam machinery

The main steam plant now has physical drum-to-header feeds, a cylinder/turbine feed, connected pump branch down to the actual pump body, and uptake crosspieces from the boiler exhaust collector to both capital funnels. Steam exhaust uses pure white input material; funnel combustion smoke is unchanged.

Smaller steam profiles use a three-cylinder casing above a common crankcase; the capital profile has a staged turbine casing in the existing engine footprint. A continuous direct shaft runs from the engine output to the existing screw location, with an identifiable thrust bearing. This is a gameplay-scale installation; it is not an exact historical machinery-room replica or a thermodynamic model with condensate/feedwater circulation.

The shaft is voxel damage geometry: loss of any shaft segment interrupts drive. The real screw hub and four blades are damageable moving voxels; losing the hub interrupts drive, losing blades reduces output. Bearing damage reduces transmission efficiency. Existing boiler, engine, funnel and main integrity/leak functions remain active.

Shaft and screw mechanisms explicitly disallow field reconstruction. Small bearing, machinery and pipe damage retains the existing 25% repair eligibility policy; major machinery destruction remains unrecoverable during battle. Live collision, visual fidelity and native damage behavior require in-game validation.

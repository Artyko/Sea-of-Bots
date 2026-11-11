# v452: afloat repair sites and non-finite flood inputs

Latest log confirms active unreachable ironclad stern/side holes and a Novik unavailable target with non-finite flow. Radial swimming repair search now accepts water 1.2 m above feet instead of requiring 2.6 m head immersion. Special repair routing uses the same depth condition. Actual clearance, reach, air checks remain; not every logged dry obstruction is solved.

FloodGrid.Add rejects non-finite input volumes before contaminating the water grid. Near-water rendered-height sampling falls back to analytic waves if interpolation is non-finite. These are containment measures; original non-finite flow source is not proven resolved.

Build, HoldSafetyChecks, WaterSurfaceChecks and diff check passed. Native repair access and remaining route failures need next gameplay verification.

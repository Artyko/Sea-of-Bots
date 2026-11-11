# v451: gun departure and shared swimming repair sites

v450 repeated Novik failures near foredeck guns show operators moving sideways from rear seats into shield structure. Added a dedicated radial rear departure segment before normal routing for starts near the rear station envelope. Entry/exit still execute through native movement. A profile test checks all three gun departures.

Enabled the existing radial swimming-repair-site search for non-destroyer hulls too, before the older fixed-lane swimming fallback. Thus ironclad/dreadnought can select fully submerged reachable work points without requiring floor support. Air budget, inside hull, body clearance and sight checks remain; initial supported searches remain first.

Build, GunDepartureChecks, StairTrafficChecks and diff check passed. Not a certification that every dry/destroyed route in the prior log is resolved; verify in next gameplay run.

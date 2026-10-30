# Steam combat vitality roadmap

This checklist is the implementation contract for making `Steam Ocean` as eventful as the sail battles without turning armour into a simple health multiplier.

## 1. Armour, ricochet and spall

- [x] Resolve impact angle before applying armour wear.
- [x] Let oblique hits ricochet with reduced plate wear.
- [x] Make a stopped high-energy hit create a bounded internal spall event.
- [x] Spall may damage nearby crew and machinery, but cannot open a hydraulic leak through intact armour.
- [x] Preserve cumulative local plate wear and distance/material-based penetration.

Acceptance: stopped hits have visible and mechanical consequences; only a destroyed/perforated shell cell admits water.

## 2. Steam mains and leaks

- [x] Add propulsion, pump and auxiliary steam-main integrity.
- [x] Build the mains as visible, repairable metal equipment in the machinery spaces.
- [x] A damaged main drains pressure, emits a narrow steam jet and makes its consumer weaker.
- [x] Steam exposure harms crew only near a live leak; there is no spontaneous damage.
- [x] Boiler flooding continues to extinguish combustion and funnel smoke.

Acceptance: machinery hits can change speed, pumping and ammunition handling before the whole boiler or engine is destroyed.

## 3. Steam-powered and hand pumping

- [x] A steam ship's manned pump uses steam assistance while the pump main and pressure are available.
- [x] Assisted pumping is substantially faster and consumes pressure.
- [x] With low pressure, a flooded boiler or a broken pump main, the same station falls back to hand-pump output.
- [x] Propulsion and pumping therefore compete for one pressure reserve.

Acceptance: emergency pumping can save the hull but visibly costs pressure, smoke density and speed.

## 4. Heel, counter-flooding and casemate availability

- [x] Severe heel inhibits the guns on the low side and slows marginal-side loading before complete inhibition.
- [x] Automatic ballast transfer remains the counter-flooding response.
- [x] Counter-flooding/transfer preserves the existing cost: more carried water, draft and reduced manoeuvrability.
- [x] HUD/telemetry reports inhibited guns and steam-pump demand.

Acceptance: list changes the available broadside and crew priorities instead of being cosmetic.

## 5. Ammunition incidents

- [x] Ready-use lockers retain three impact outcomes: no reaction, warning fuse, immediate local explosion.
- [x] Enclosed magazines keep a longer visible/audible fuse and lower immediate-blast probability.
- [x] Main-magazine detonation remains catastrophic.
- [x] No store may ignite without impact, a nearby real fire or heat propagation.

Acceptance: ammunition emergencies are dangerous but telegraphed and extinguishable; there is no spontaneous ignition.

## 6. Crew overload and priorities

- [x] Critical ammunition fire outranks routine gunnery.
- [x] Critical flooding assigns pumps and repairs in parallel when hands exist.
- [x] A live steam leak reserves a repair hand before ordinary cannon restoration.
- [x] Helm and one usable weapon are retained until a genuine emergency requires their crew.
- [x] Assignments remain sticky enough to avoid route oscillation.

Acceptance: the ship temporarily stops firing during a real crisis, stabilises, then returns hands to combat.

## 7. Multi-phase steam tactics

- [x] Approach bow-on at useful speed.
- [x] Align and fire the main battery.
- [x] Roll into a casemate broadside pass.
- [x] Separate after the pass instead of holding one parallel course forever.
- [x] Turn the damaged/low/flooded side away during a recovery window.
- [x] Re-enter using the healthier battery when the emergency clears.

Acceptance: local simulations produce repeated main-gun and broadside opportunities, separation/re-entry phases and recovery windows without deadlocks.

### v237 runtime acceptance follow-up

- [x] Split idle universal hands evenly between the hold and middle deck instead of crowding the weather-deck battery.
- [x] Recover a routed sailor if a missed moving-deck carry leaves the body behind the ship; genuine water-level overboard cases still use rescue logic.
- [x] Give rifled batteries an explicit below-waterline aiming band while preserving sail-era targeting.
- [x] Give ready-use ammunition, enclosed magazines and the main magazine distinct escalating blast profiles.
- [x] Add bounded charred-fragment flight, rolling base smoke and a taller smoke core to ammunition detonations.

## 8. Feedback and regression safety

- [x] Log ricochet, spall, live steam-main leaks, pump mode and tactical phase changes.
- [x] Extend HUD steam telemetry without changing game graphics settings.
- [x] Add deterministic local tests for every new rule.
- [x] Preserve sail-era balance and routes.
- [x] Build and install without launching the game.

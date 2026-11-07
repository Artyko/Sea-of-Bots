# v413 — latest v412 convoy battle

The active unavailable Dreadnought repair cluster was at (-2.35,-.01.. .07,-.13), repeatedly reporting approximately 4.7 m³/s and no valid work position. Repeated traffic stalls also included bridge treads with feet .36–.54 m above their waypoint, ironclad ledge descent .32 m above a landing, Novik aft gun posts touching shield geometry, and ironclad sailors outside the armour belt below the weather deck.

Changes: repair-site fallback samples inward and longitudinal offsets in absolute metres on every maintenance deck, using actual tank surfaces and existing reach, support, capsule and line-of-sight checks. Tread/ledge arrival accepts the observed positive support height while retaining tight rejection below the tread. Gun service distance increased to 3.6 m. Outside-shell, below-deck sailors enter ladder rescue even in shallow wave troughs instead of attempting to walk through armour.

Validation: build and regression checks cover bridge support offsets, early below-tread rejection, stair repair and AA access. A new battle is required to verify the full physical repair path at the logged hole and all dynamic congestion cases; local geometry checks are not a guarantee of zero future stalls.

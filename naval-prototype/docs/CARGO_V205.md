# Finite cargo — v205

| Ship | Hold cannonballs | Each gun | Total cannonballs | Hold boards | Spare-part crates |
|---|---:|---:|---:|---:|---:|
| Sloop | 80 | 20 | 120 | 160 | 2 |
| Brigantine | 160 | 20 | 240 | 320 | 4 |
| Galleon | 320 | 20 | 440 | 640 | 6 |

Cannonball crates contain 20, plank crates 40, spare-part crates one kit. Shared NavalCargoLayout distributes hold stock along the sides, alternating maintenance levels on galleons; pickup positions are one world metre inboard. Existing ballast ramp routes serve raised stocks. Old decorative low casks/cargo occupying these locations are omitted. Extra wall lanterns and navigation charts furnish the hold without occupying corridors.

NavalCargoStock owns count/capacity/destruction. NavalCargoCrate owns damageable shell and visible contents; contents decrease with count. Empty ammo/parts crates remain visibly empty, plank crates disappear. Shell damage or detached/missing visible stock destroys remaining stock permanently until explicit debug reset. Already transported resources are independent. These are solid voxel representations, including stacked dark voxel cannonballs and a protruding cannon component.

Cannon Fire consumes one from its local box, then uses existing reload timing; no local ammo means no shot. Unassigned available crew replenish a functional gun below five rounds, carrying five per trip through the existing controller/route system. One logistics runner per ship prevents duplicated fetching; fire/leak/critical survival tasks prevent new logistics dispatch. The existing carried-parts visual is reused for the ammunition bundle (a dedicated cannonball carrying model is not yet provided). Carried ammo is included in the total while in transit. Interrupted/failed deliveries lose carried ammunition rather than creating stock remotely. Main helm/active combat assignments are preserved; an empty gun does not reserve a gunner, allowing that sailor to carry supplies. Physics movement and pickup/delivery delays remain real.

J at a hold ammunition crate takes five rounds; J at gun crate deposits carried rounds. J at a plank crate takes up to four boards. Player hull patches require a carried board, consumed only after the repair timer. Sailors fetch existing four-board bundles; finite crates debit at pickup. E still fetches one cannon repair kit, now debiting its own crate. Cannon repairs remain once per gun and sixty seconds. The closest kit crate owns the interaction to avoid multiple pickups.

Both ship cards display total cannonballs, boards remaining in crates, and spare kits remaining in crates. Existing hull repair/debug reset restores cargo. The model does not yet weigh resource units in stability, add explosive behaviour to cannonballs, or change existing repair duration.

Verification: full game-assembly compilation; finite stock/transfer/destruction/reset tests; exact totals for all profiles; 68 supply routes checked against every cargo crate with full crew capsule clearance; existing ship-route, stair, smoke and combat handoff tests. Native gameplay, new cargo voxel attachment, damage visibility, operator interaction and actual logistics require live testing.

## v206
Delivery now yields immediately to a new repair demand, fire, critical flooding or urgent trim. Already carried rounds survive ordinary task cancellation and count across all living crew; falling overboard loses the carried bundle. Pickup/delivery approach considers five supported, body-clear positions, trying another after a blocked route. One runner per ship remains reserved only during actual logistics. Native movement and event interruption require a live fight; headless route geometry remains covered by CargoChecks.

## v207 visuals
Open slatted 1.3m square crates, contrasting rim/corners. Twenty individual low-poly sphere meshes replace merged black voxel cubes; source hit shapes and finite stock remain damageable. Tall visible boards, hollow cannon-tube sections and carriage timbers distinguish stock kinds. Larger clearance uses 1.3m pickup offset with actual ramp/floor surface rather than crate base height. Cargo spacing increased and moved inboard; all 68 updated stock routes pass full-body clearance. Appearance not verified in native game.

## v208 cargo route audit
Plank/parts alternative approaches now move along the aisle and recalculate ballast/floor surface height, rather than moving laterally toward cargo with a stale height. Blocked uncollected plank/parts stocks receive a ten-second per-worker exclusion so retries can select a different crate. Hold-ammo approaches use the same surface adjustment. CargoChecks expands to 656 bidirectional stock/cannon routes on all profiles with large-crate full-body clearance. Native PhysX and gun-carriage obstruction remain live-test requirements.

## v209 inboard storage
Hold cargo now occupies two central rows at local x=+/-0.45, beginning z=1.1 ahead of the staircase, 1.5 world metres between rows along the ship. Galleon cargo alternates maintenance decks. Pickup is outward toward the walking corridors, 1.3 world metres from each crate, on the actual floor. NavalSupplyRoutes routes around all current cargo footprints rather than obsolete central supply placeholders. Gun-local ammo boxes remain beside the guns. 656 supply routes and 3514 general routes pass CPU checks; native visual clearance still needs playtesting.

## v210 idle maintenance fix
Reviewed Player.log running v208. Global pump demand and quiet-period gates could veto refurbishment despite an existing pump operator and idle spare hands. Spare hands and ongoing cannon repairers now sustain permission when pump demand is covered, no leak/fire/critical/trim exists. Up to two separate guns may be claimed; mechanism ownership prevents duplicate work. Ordinary single-worker maintenance gate yields for spare cannon refurbishment. Inflight repairs keep eligibility across assignment passes. Ammo resupply begins at 15 or fewer rounds. Full game build passed; native allocation still needs a battle check.

## v211 grouped stock
Cargo is grouped in four-crate islands, with 1.5m extra longitudinal spacing after each two rows. Layout spans forward/aft holds instead of a solid forward rack. Maintenance-deck crossing chooses a shorter available gap outside staircase footprint. Colliding crate-detour segments are repaired using a bounded corner visibility graph, only when needed at route creation. One normal footprint pass replaces the former three repeated passes. Tiller post moved aft to z=-5.3 to preserve approach clearance from the nearest group. Updated 656 stock routes and 3514 task routes checked against current cargo footprints pass. Game playtesting still required; no FPS improvement measured.

# v272: Dreadnought steam capital ship

Reference: https://ru.wikipedia.org/wiki/HMS_Dreadnought_(1906)
Historical inspiration: five twin 305-mm mounts, one forward, two wing mounts,
two centreline aft mounts; eight main barrels on a broadside, two funnels and
tripod/signal masts. This is a gameplay adaptation, not a historical simulator.
The historical 160.74 x 25.01 m hull and 685+ crew are not reproduced literally.

## Playable profile

Select **Dreadnought / Дредноут** for either fleet in the Steam Ocean ship menu.
The existing Ironclad Corvette remains selectable and remains the default.
The larger hull uses scale 6 (72 m local bounding length, 28.2 m maximum beam)
versus the corvette's scale 4. Thus the hull is deliberately shorter and wider
than its reference to support walking, camera clearance and the shared water grid.
Three accessible decks, inherited 5.6 m lower-hold clearance and 4.8 m middle-deck
deck spacing (same world distance as the corvette), new forward command
bridge, five armoured turrets/barbettes, separate magazines and two funnels.

16 sailors, equal for player and enemy: five dedicated heavy operators plus
11 universal hands, including helm, secondary gunners and damage control.
Existing crew allocation, emergency release, replacement after casualties,
repairs, ammunition supply, pumping, ballast, buckets, fire response, rescue,
boarding, island avoidance and reversing are reused. Heavy-station allocation
and spawn/access auditing now use a variable station count instead of two.

## Weapons / balance (game units)

- Five separately damaged twin 305-mm mounts A/P/Q/X/Y, 80 shells per mount.
- Two shots .35 s apart; 30 s loading cycle, slowed by auxiliary steam damage.
- Projectile speed 125 m/s, configured range 850 m, power 7.8, random angular
  dispersion. These are gameplay numbers, not historical muzzle ballistics.
- Sectors prevent a wing mount firing across the opposite wing. A self-ship
  obstruction query prevents a shot through the bridge, another mount or stack.
- Eight lighter secondary guns, four per side; existing finite deck/hold supply.
- Main belt projectile/blast resistance multiplier 1.6 relative to corvette.
- Each magazine disables its own heavy mount if it detonates; physical blast
  can still damage neighbours. Existing one-use / 60-second gun repair applies.
- Hull, equipment, masts, turrets and stores remain destructible.

## Routes and performance

Cached weather-deck visibility graph covers turrets, open companionway, bridge,
stacks and mast feet. Supply graph additionally covers every magazine trunk on
both enclosed decks. Boiler and secondary magazines have different longitudinal
positions to prevent overlap with the wing barbettes. Cargo uses unique paired
rows on both decks, with 13 repair kits for 8 secondary + 5 heavy mounts.
Cold magazine integrity queries cache by hull damage revision; heavy mounts
share the existing bounded effects batches and physics/simulation machinery.

Tests: DreadnoughtChecks verifies five staffed mounts, broadside sectors,
non-intersecting stores/barbettes and 49 continuous routes to the major duties.
Corvette, fire, pump/stair and hull regression checks also run.
No live game battle or visual QA has been performed. Native controller clearance,
heavy-turret damage balance, appearance and FPS must be checked on the next log;
pure route tests do not guarantee every dynamic crowd/damage case.

# v271: steam hold, crew and fire refit

Last inspected battle: Player.log reports v270 Ironclad Corvette versus Ironclad Corvette.
Repeated stops included middle-deck firefighting-water trips at (.53,.85,-3.38),
(.96,.85,-.84), and weather-deck trips near (+/-1.43,2.05,-3.82).
Main-gun emergency release switched demand 2 -> 1 -> 2 near the end of battle.

Changes:
- Wire world-distance route arrival into the actual follower (the v270 helper was unused).
- Classify missing deck cells by actual deck surface height. Missing low partition
  pieces no longer masquerade as floor holes. A supported starting hole margin
  may be left during local avoidance, while new holes remain blocked.
- Keep emergency main-gun release until ten continuous quiet seconds and completion
  of accepted work; increasing emergency demand still releases hands immediately.
- Remove ironclad partitions consistently from geometry, flood barriers and routes.
  Four enlarged sealed wing cisterns retain ballast transfer, breaches and repair.
- Fix duplicate lower-hold reserve racks, separate port boiler-adjacent stock,
  retain magazine pickup clearance, and add machinery corners to the cached graph.
- Boiler has a stepped octagonal jacket, saddles and firebox. Engine has floor supports.
  Solid flanged overhead steam mains and uptake stay outside the central aisle;
  boiler/engine shifted inboard to leave an outboard service strip. Starboard
  magazines now have their own solid enclosure instead of relying on partitions.
- Remove sacrificial visual bow cheeks/stem covers. Clip partial end armour cells
  to the actual outline instead of dropping both edge cells: this closes the two
  narrow unarmoured strips. Raised forward bulwarks are physical armour.
- Remove the global fire-count cap (visual smoke/particle budgets remain bounded).
  Sustained local heat accumulates on nearby repairable mechanism cells; eight
  full-intensity heat-seconds destroy a cell, cooling drains heat, water cools it.
  This can disable guns, steering, pumps and steam machinery, in addition to
  burning fuel and ammunition cook-off. Robot lethal exposure is now four
  consecutive seconds within 1.4 m, with cooling away from flame.
- All penetration curves increased by 35%, without changing blast-radius settings.
  AP shells arm on perforation and burst at the next genuine solid contact or
  two seconds later, including after water drag stops them. Entry-plate seam
  duplicates remain excluded; no distance fuze or water-range detonation remains.

Validation: compile plus pure regression tests for steam vitality, hull fit,
stairs/pump routes, lower-hull repair reach, fire/powder and the new refit cases.
These do not replace a live battle: native collision, appearance and observed
crew behaviour need verification after restarting with v271. Unlimited fires
can increase simulation work; indexing/cold-cell skipping and bounded visuals
reduce cost, without changing the requested absence of a fire-count cap.

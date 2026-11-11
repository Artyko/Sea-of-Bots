# v450: swimming across destroyed deck gaps

User screenshot shows damage-control congestion amid flooded broken decks. Latest route failures include below-deck vertical mismatches and repeated plank/leak routes. Confirmed code defect: MoveWithController applied deck-gap blocking even when movingInWater was true, and Supported/NavigationWalkClear required solid floor even at fully submerged swimming nodes.

Deck-gap walking guard now excludes swimmers. Local recovery treats nodes as water-supported only when already in swimming movement and the node is fully submerged (water over feet+2.6 m). Such links still require inside-hull footprint and native capsule clearance; dry/shallow links retain support and gap checks. No phase-through equipment, no free boards or invulnerability. Existing air handling retained.

Build, HoldSafetyChecks, StairTrafficChecks and diff checks passed. Native battle navigation not yet verified; this fix does not certify every dry-deck/turret obstruction resolved.

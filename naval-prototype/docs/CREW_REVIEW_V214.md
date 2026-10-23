# v214 crew and ammunition review

Latest reviewed Player.log ran v212. It contains repeated sail-hand assignment and a plank carrier stalled at (0.06,-0.82,-1.34) targeting the bottom tread (0.14,-0.79,-2.96). The exact cannon-abandonment reason was not logged; user clarified its local ammo crate was destroyed.

- Ordinary sail assignment cannot borrow an allocated cannon currently bearing on the enemy. Rescue/reverse/final-attack overrides remain.
- Nearby same-team crew controllers temporarily ignore each other when either sailor has an active movement/task, extending the staircase passage fallback to close equipment corridors. Player/enemy and hull collisions remain. This permits friendly passage rather than physical queuing.
- Underflight recovery recognizes low entry treads, including the exact logged plank-carrier stall. Same-height target alone no longer prevents leaving underneath the staircase.
- Destroying a gun crate still loses its contents permanently. Fresh deliveries can load the intact gun directly into a separate twenty-round local reserve; they do not restore the crate or lost cargo. Fire consumes these delivered rounds. Player J delivery follows the same rule. Total ammunition includes direct reserves.
- Runner sources can include adjacent gun crates, retaining one shot in the donor. Neighbour sourcing is used only when target ammunition is below five to prevent endless reciprocal top-up; ordinary stocking uses hold reserves. Source choice uses route cost. No resources are remotely teleported.

Full build, exact stair regression, 656 cargo routes and 3514 general routes pass. Native deadlock recovery, neighbouring deck-crate carry routes and live duty transitions need gameplay confirmation.

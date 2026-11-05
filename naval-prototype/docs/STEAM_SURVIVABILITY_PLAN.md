# Steam survivability implementation

Authorized scope: raised protected fireboxes, local boiler-room flooding, independent Dreadnought boilers, finite stored-steam escape, engineering pump priority, economical emergency helm orders, ballast decision diagnostics, and crew access regression checks.

Physical rules: all retained water remains in the shared flood grid and contributes mass/heel; protective sills must correspond to destructible geometry; openings admit water; no minimum engine power after steam is exhausted. Service access must be possible from existing machinery-room aisles, with no narrow stair-only dependency. At least one boiler can survive damage to the other on capital ships. Hand pumping remains usable without steam. Ballast is not added simply to preserve propulsion.

Validation must cover intact protection / breached protection / service openings, asymmetric boiler loss, stored-pressure depletion, pumping mass conservation, all steam-profile routes and inherited sail-profile behavior. Pure tests cannot verify native PhysX or certify live FPS.

## Implemented v298

Access choice: an open-topped, 1.1 m steel boiler cofferdam with aisle-side servicing, rather than a full-height room requiring another doorway/ladder chokepoint. Grid-aligned panels and a capital-ship transverse divider are real solid destructible structure. Breaking a panel opens its corresponding flow edge; repairing it restores the sill. No water mass is stored outside the flood grid. No new blocking door or mandatory climb is added.

Dreadnought has two visibly separated boiler housings, independently damaged cell groups, independent steam states and local flood samples. Shared propulsion takes their mean output. Smaller steam profiles retain one boiler. Runtime reserve mode retains pressure after firebox flooding, consumes it under demand and eventually loses all power. A flooded boiler remains flood-sensitive; intact coaming delays incoming bilge water, not external direct hits. The firebox flood datum is raised 0.5 m to match its elevated structure.

Boiler flooding activates early retreat / economy orders, priority local sump draining and urgent pump assignment. Under reserve economy the steam pump assist is limited to conserve propulsion pressure; hand pumping remains available. Ballast keeps its effectiveness-based behavior; reasons for transfer/drain/inactivity are logged every ten seconds during pump operation. Boiler-side flowing leaks receive greater repair priority. Protective panels are repair targets.

Port lower reserve racks were relocated clear of the new boiler protection. Static machinery exclusions and graph service nodes were expanded. Failed plank deliveries postpone the same repair target for 15 seconds; inaccessible-site retries use 3/6 seconds for flowing/dry damage. These changes reduce repeated failed requests but do not guarantee that every damaged native scene remains traversable.

Verification: finite-reserve/economy/depletion, independent state loss and flood-sill mass conservation tests; existing steam, cargo, Dreadnought, pump and stair-route regressions. Live gameplay, camera clearance and PhysX contacts remain unverified. This is a coarse flood-grid protection model, not a fluid simulation of a fully enclosed room.

# v459: distributed ammo, magazine blast and transient route memory

Novik reserve layout now alternates bow/stern positions so first ammo racks are balanced across both ends. Original rack positions/counts and resource budgets retained; later plank/parts racks are reordered consistently through shared layout.

Capital-ship main-magazine hull blast radius reduced from default 15 m to absolute 10 m, leaving power, other ammunition explosions and crew danger radius unchanged. This is reduced radial damage, not physical armour occlusion; machinery within 10 m remains vulnerable.

Cached route failure now revalidates actual walk clearance before rejecting the segment. A route reopened by moved crew/equipment no longer remains denied solely by the old cooldown; genuine gaps/walls still reject. Extra probes occur only for cached failures.

Build, DistributedDestroyerAmmoChecks, StairTrafficChecks and diff check passed. Full combat movement and blast survivability require gameplay verification. Zero accepted rim flow is not forcibly overridden; full local capacity/backpressure can legitimately stop acceptance, and not all such records have been individually resolved.

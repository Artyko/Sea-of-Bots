# v305 friendly passage yield

A stalled navigating sailor can ask an idle friendly worker on the same ship/deck to yield. Safe side candidates are tested for hull bounds, intact support, capsule clearance, full supported traversal and separation from the approaching route. The worker yields for up to 1.8 s, with a 5 s request cooldown, then resumes its existing home/post route. The command post, turret transit, accepted work and critical emergencies are protected. Active yielding is busy/navigation state, so station anchoring and support reassignment do not override it.

The existing friendly-only narrow-passage fallback remains. Its collider snapshots refresh while active, accounting for body-part changes after damage. Ship, equipment, player and enemy collision rules remain unchanged. Live interaction remains unverified; log messages identify successful yielding decisions.

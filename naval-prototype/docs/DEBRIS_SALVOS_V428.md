# v428 — aircraft debris inertia and varied torpedo attacks

Debris and aircraft now share the same bounded .05 s step; previously debris advanced by full deltaTime while aircraft advanced at most .05 s, producing disproportionate falling at low FPS. Debris inherits actual crashing velocity or current flight velocity and experiences lightweight linear air resistance plus gravity. The crashing aircraft also retains its pre-crash forward speed rather than instantly dropping to 40 m/s. No per-piece rigidbodies/colliders or new proximity scans are added.

Removed the per-formation forced salvo styles/distances that overrode random attack planning. Distant approaches may choose a long single shot (1250–1400 m), a long pair (1000–1250 m), ordinary close shots/pairs/fans or sequential four-shot fans. Long pairs schedule the second tube with existing delayed-pair safety checks and block other mounts from turning the pair into an unintended salvo. Existing friendly corridor checks, reattack cycles and offset approach lanes remain.

Validation: build, DebrisAndSalvoChecks and TorpedoStateChecks. Visual trailing behaviour and tactical variety need runtime verification.

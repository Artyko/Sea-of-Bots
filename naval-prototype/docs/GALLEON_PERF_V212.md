# v212 spawn fragments, cargo cost and muzzle light

Latest reviewed log ran v210 Galleon/Galleon, including 12.3 FPS intervals; PhysicsSync ~0.07 ms, SupportQuery ~7.4 ms, Shore ~14.7 ms inclusive per sampled frame. These do not establish GPU or complete Unity physics costs.

The intact attachment graph already identified disconnected geometry on its first refresh, but converted it to dynamic continuous-collision rigidbody debris. Initial disconnected components now disappear with a diagnostic of ship, count and hull coordinates; later combat fragmentation still drops. This is spawn cleanup rather than a rebuilt bow topology. Diagnostics are needed to identify exact decorative pieces and confirm no stock items were affected.

Cannonball mesh uses 4 rings/6 sides (35 vertices) instead of 6/8 (63 vertices), reducing vertices ~44% without reducing individual ammunition counts. The fixed 24-entry support memo reuses exact identical query arguments within the same render frame and synchronization generation; damage marks and explicit physics sync invalidate it. It does not share samples across frames or skip collision synchronization. Native environmental changes outside these hooks can require additional invalidation; no live performance gain measured.

Muzzle flash now includes a 0.07-second warm point light, intensity 0.85, range 4m, no shadows, plus the existing smaller short puff. No persistent light.

Full game build and existing 656 cargo/3514 general route tests passed. Native rendering, collision behaviour, spawn diagnostics and FPS require a new fight.

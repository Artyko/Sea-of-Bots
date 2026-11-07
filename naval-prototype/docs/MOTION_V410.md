# v410 — variable-frame forward motion

The v409 convoy log contained 40–109 ms frames. Translation used min(deltaTime, 50 ms), producing frame-dependent forward distance whenever a frame exceeded 50 ms. Player and AI ships now integrate planar displacement with elapsed game time capped at 200 ms to avoid catch-up teleports after a long pause. Existing bounded buoyancy/crew/AI updates remain unchanged. Speed reporting and ram velocity use the same elapsed interval; shore and ship collision constraints still check the full proposed displacement.

MotionClockChecks validates equal distance at 10–120 FPS, mixed frame durations and the long-stall bound. Build and diff whitespace checks pass. This corrects lost translation time, not the low rendering rate observed in the convoy log; visual stepping at low FPS can remain.

# v448: rendered ocean level

Near ocean uses a 3 m triangulated grid while Height previously sampled continuous analytical waves. These disagree between vertices, and cached render frames can also differ in time. Height now interpolates the last rendered grid's exact triangle (including wake displacement) inside its footprint, using the renderer's actual diagonal and cached origin. Flooding, buoyancy and water-level callers therefore agree with visible near-water geometry. Outside the grid or before its first render the analytic sampler remains. No flood volumes are set artificially to sea height; inflow and spreading remain physical. Constant-time array lookup, no new raycasts or allocations.

Build, WaterSurfaceChecks and diff check passed. Verify visual overtopping in gameplay; other causes of absent inflow remain possible.

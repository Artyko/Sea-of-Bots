# v458: tapered-rim overflow receiver

ObserveRimPressure previously returned immediately if Flood.Index at the exact outer lip was inactive. It now searches up to 4.2 physical metres along the inward direction for an active receiving column. Detached/nonexistent sections with no active interior still reject overflow. Ocean sampler, lip geometry and pressure-based flow retained.

Added throttled per-hull Rim overflow telemetry with sea_y, lip_y, inside_y, requested and accepted m3/s and destination index. This separates visible-water mismatch, wrong lip height, backpressure and capacity rejection in the next log. Build, WaterSurfaceChecks and diff check passed. No native proof that this covers the reported case yet.

# v414 — AA access and player sights

- Removed projecting aft AA bridges. Rungs sit flush at the circular rim. Separate outside crest and supported inside landing provide reversible climb/transfer paths for crew and player.
- Manual AA hides only the controlling player's renderers, preserving and restoring their forceRenderingOff states on exit/disable. Mouse-up raises the barrel.
- Manual main-battery view shows a small recharge bar and remaining reload seconds.
- Logged Dreadnought secondary posts at x=±.92,z=3 occupied the enlarged bridge side wall. Forward secondary operator posts now sit at x=±1.20.
- Hold membership now checks the actual tapered hull width. An outside-shell sailor standing on exterior trim no longer bypasses ladder rescue simply because a support ray hits that trim.
- Moving AA parts were excluded from the fixed structural graph. AA now records pedestal attachment cells and checks them only after hull revisions. Lost attachment drops surviving equipment through shared structural debris physics, releases crew and prevents firing; full structural restore resets this state.

Build and AA platform / bridge flight / stair repair route regressions are local checks. Dynamic congestion and destruction require the next gameplay run.

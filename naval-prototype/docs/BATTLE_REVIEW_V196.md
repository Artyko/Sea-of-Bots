# Review of the latest v195 battle (22 October 2026)

Source: Player.log, ending 15:41:40. Two brigantines.

- One eight-second route stall remains: EnemyWoodenShip, Going to sails, feet (0.22,-0.71,-0.33), target (0.18,0.34,-0.34). This is an upward obstruction, not the previous small descending tread height mismatch. It needs separate native collision investigation; increasing arrival tolerance would incorrectly skip over three metres of height.
- Island recovery did log forward recovery followed by completion. Subsequent shooting continued.
- Zero-gun decisions were not logged by v195. The existing rule MustBoard requires both no functional guns and no restorable guns with parts. Thus zero guns alone deliberately does not trigger ramming. The log alone cannot prove which gate prevented a particular observed decision. v196 logs gun count, usable repair kits, canRestore, mustBoard, approach, rescue, sunk every 15 seconds.
- PhysicsSync dominates measured mod CPU time (typically 19–21 ms in slow sections); SupportQuery is another candidate. These are inclusive timings: do not sum scopes. Maximum is per individual invocation, average is per sampled frame, so average can exceed max. These are not GPU or total engine profiling measurements.
- No speculative physics synchronization removal in this HUD update: collision correctness must be preserved.

## HUD
Both ship cards now show available cannon repair kits. This is the count of remaining usable one-time cannon repairs, including already carried/delivered kits; a destroyed crate removes unfetched availability. There is no independent finite crate stock counter in the current model. Cannon one-time repair rules are unchanged. Cards and following help content move down one row, and screen scaling accounts for the extra row.

Measured summary: 50 periodic samples; FPS min/median/max 16.1/20.7/37.7; PhysicsSync average-per-sampled-frame median 19.565 ms, worst interval 22.53 ms; SupportQuery median 3.42 ms, worst interval 9.19 ms; one route stall, ten shore-block reports, one completed recovery, zero cannon restorations, zero Exception/NullReference matches. Full game collision reproduction was not performed.

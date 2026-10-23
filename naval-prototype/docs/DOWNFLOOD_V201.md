# v201 — downflood pressure at the opening

The latest v200 Player.log identifies Galleon/Galleon, but has no separate downflood rates or lip immersion samples. It cannot establish whether a particular visible edge was submerged while actual overflow was zero.

Code review found opening backpressure calculated at the centre of a flood-grid cell instead of the actual opening X/Z. With pitch/roll the inward cell centre can be above the lip, producing an artificial seal before the interior water reaches the opening. Gunport code also treated a dry floor as a water surface and used a separate weak linear intake law.

Rim and intact gunport/ladder sources now evaluate interior pressure at the opening's horizontal coordinates: lipWorldY + (floor + depth - lipLocalY) * upY * scale. Dry columns provide no backpressure. For upY <= .05, height columns do not reliably represent an inverted free surface, so artificial backpressure is disabled while volume capacity remains enforced. This is still a height-column model, not full 3D fluid simulation.

Ports use the same weir law as intact rim overflow, with their actual 2.2-local-unit span; ladder openings use 1.3. Existing exclusion of overlapping rim strips remains. All accepted water still goes through Flood.Add capacity limits, spreading and pumping. Wash events and crew opening-pressure emergency remain connected. No global multiplier was added to hull holes.

Every periodic combat telemetry report now logs Downflood: ship, total water, accepted_m3s (intact openings and rim), pressure_m3s (potential flow before cell capacity clamp), rim_head_m (maximum sampled rim immersion), roll and pitch. A zero accepted rate can be legitimate when the receiver cell is full or inside/outside pressures equalize. These samples are instantaneous every 15 seconds, so brief wave splashes can occur between them.

Tests: exact false-pressure regression at positive/negative 35/60/80 degree tilt, each profile, rim and gunport; dry/inverted columns; equal levels; actual configured galleon storage acceptance; existing rim, grid, hole, downflood-emergency and flooded-stability checks. Full game-assembly build passes. No native playtest or actual overflow-rate measurement performed.

# Battle review v222

Latest available battle: v221 Brigantine/Brigantine, not Galleon. Seven 8-second route stalls: standby/returning positions near x=-1.9 and emergency tiller (-1.2, floor, -5.3). Tiller feet are approximately 0.6 m above the nominal post, already horizontally aligned; standby feet can be 0.42 m horizontally from the nominal post. Frequent stale-route and underflight recoveries also appear; a log alone does not prove every recovery succeeds or identify the exact obstructing object. No NullReferenceException, MissingReferenceException or IndexOutOfRangeException found in this log.

Gunnery: 66 shots recorded: 38 allied and 28 enemy. Targets 45 hull/deck, 19 cannon, 2 wheel. Enemy fires both sides, allied shots use port in this battle. This does not establish a gunnery fault without full pose/visibility/crew context; no firing policy was changed in this patch.

Changes:
- Player support casts on own/enemy ship now use actual controller radius in world space instead of the fixed 0.12 m probe. Pre-movement water-contact check uses the same footprint. Cast length is shortened by the radius increase to retain previous bottom reach, avoiding extra vertical pickup while airborne. Native hit normals, full-query fallback and ship membership filtering remain.
- Final returning routes can accept a nearby station on actual supported footing up to 0.8 m above the nominal post, with horizontal separation at most 0.45 m. A floor below a station is not accepted by this new rule. No elevated correction for intermediate steps or repair reach. Anchoring preserves a supported raised footing instead of snapping it through trim into the nominal floor.

Validation: full build, exact logged raised-post bounds and unchanged cast bottom extent tests, 3514 task routes, 656 cargo routes/stock checks and underflight regression tests. No native moving-ship/railing playtest performed. Fresh battle logs are needed to verify remaining recovery loops and actual player support on each trim mesh.

# v446: restore patch-level repair semantics

Compared current code with commit 0f3d85b. Older completion restored all claimed voxels within patch radius; later per-cell reach and same-plane filters reduced patches, particularly at tapered hull seams. Removed these completion filters. Real access/reach to the accepted work anchor remains mandatory. All claimed restorable cells inside the absolute 3 m sphere are restored in one operation, matching older patch-level behaviour. This intentionally permits patch restoration around nearby corners and slab seams; it is an area repair, not an individual hand reach simulation.

If a hydraulic target has no work point, try up to eight unclaimed damaged hydraulic neighbours within 3 m as alternate accessible patch anchors. Their patches include the original centre. Existing claims, mechanism restoration rules and closer-worker arbitration remain. Radius remains 3 m, not the old scale-dependent radius.

Build, RepairRadiusChecks, RepairWorkSiteChecks and diff check passed. Native battle result not yet measured.

# v466: X ladder outside foundation

Current log pinpoints stage4 at deck level feet=(-.52,1.64,-4.20), target=(-1.15,1.63,-4.20), repeated. Thus this case is not waiting for door or descending: operator cannot walk laterally away at the bottom. Previous X ladder coordinate edge+.15=.58 was too close to the turret foundation footprint.

Shared XLadderOut=.95 moves physical rungs/rails and all entry/exit axis points outside foundation. Added solid upper landing between side hatch and ladder axis. Scripted movement references the same constant. New XLadderClearanceChecks covers aligned bottom/middle/top, deck height and conservative foundation+capsule spacing. Build and TurretDoorChecks passed; native full entrance/exit still to confirm in a newly spawned ship.

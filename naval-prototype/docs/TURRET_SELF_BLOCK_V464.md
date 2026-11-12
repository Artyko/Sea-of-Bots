# v464: turret operator hatch self-block

Assigned operator can no longer block their own requested turret hatch opening in CapitalDoorPoseClear. Other actors still block the hatch. Exposed AssignedCapitalSlot for the exact ownership check. Ordinary supported-deck carry now excludes CapitalReserved actors: their seated and access movement is explicitly handled by the turret path, avoiding competing pose writers during ladder travel.

Build, TurretDoorChecks and diff check passed. Native door/ladder entry and exit still require gameplay confirmation; the log's crew-access hold alone does not prove every case came from hatch self-collision.

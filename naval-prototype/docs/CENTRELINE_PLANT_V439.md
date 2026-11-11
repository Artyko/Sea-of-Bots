# v439: centreline steam machinery

BoilerX and EngineX moved from -1.45 local units to zero for the shared ironclad/dreadnought steam layout. Existing longitudinal positions and heights retained, placing the machinery along the central stair region rather than the port service aisle. Shaft endpoints derive from EngineX and now align with the central stern propeller. Boiler geometry, engine geometry, protection grid, flood sampling and static route obstacles all consume these shared coordinates. Existing funnel locations and connected pipe endpoints retained. Removed port-only emergency repair priority bias around the now central boiler.

Validation: build, StairTrafficChecks, RepairWorkSiteChecks, git diff --check. Pure checks do not certify native collider clearance or furnishings overlap; verify actual passage and service access on freshly spawned ships in the next battle.

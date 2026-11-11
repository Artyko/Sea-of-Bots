public static class NavalCargoLayout {
 public static bool IntegratedReadyUse(NavalShipDefinition ship){return ship.SteamPowered;}
 public static float RouteHalfExtent(NavalShipDefinition ship){return (ship.SteamPowered?1.65f:1.125f)/ship.Scale;}
 // Keep valid diagonal work posts near a rack, but place generated detour
 // corners farther from native bevel and CharacterController skin widths.
 public static float RouteWaypointHalfExtent(NavalShipDefinition ship){return RouteHalfExtent(ship)+(ship.SteamPowered?.45f:.10f)/ship.Scale;}
 // Recovery from a physical rack lid needs extra controller-skin clearance,
 // but applying that margin to every graph corner creates diagonal chords
 // through neighbouring racks. Keep it specific to the first escape step.
 public static float EscapeWaypointHalfExtent(NavalShipDefinition ship){return RouteHalfExtent(ship)+(ship.SteamPowered?.70f:.10f)/ship.Scale;}
 public static NavalWalkPoint Pickup(NavalShipDefinition ship,NavalWalkPoint p){if(ship.Destroyer)return new NavalWalkPoint(System.Math.Min(3.6f/ship.Scale,System.Math.Min(ship.HullHalfWidth(p.Z+1f/ship.Scale),ship.HullHalfWidth(p.Z-1f/ship.Scale))-1f/ship.Scale),ship.HoldFloor,p.Z);float floor=ship.HoldFloor;for(int level=0;level<ship.MaintenanceDeckCount;level++)if(ship.MaintenanceFloor(level)<=p.Y+.001f)floor=ship.MaintenanceFloor(level);float direction=ship.Partitions?-1f:ship.Scale<=2?-1f:1f;if(ship.SteamPowered){bool magazine=System.Math.Abs(p.Z-NavalMainMagazineLayout.Z)<=NavalMainMagazineLayout.RouteHalfDepth(ship)+.2f;float lane=magazine?.68f:System.Math.Abs(p.X)<1f?(ship.CapitalShip?.55f:.50f):.60f;float x=System.Math.Abs(p.X)<.1f?0f:(p.X>0?lane:-lane),z=System.Math.Abs(p.X)<.1f?p.Z+.42f:p.Z;return new NavalWalkPoint(x,NavalBallastLayout.Surface(x,z,floor,ship),z);}float px=p.X+(p.X>0?1.65f:-1.65f)/ship.Scale*direction;return new NavalWalkPoint(px,NavalBallastLayout.Surface(px,p.Z,floor,ship),p.Z);}
 public static int Factor(NavalShipDefinition ship){return ship==NavalShipDefinition.Sloop?1:ship==NavalShipDefinition.Brigantine||ship.SteamPowered?2:4;}
 public static int OriginalBoxes(NavalShipDefinition ship,int kind){return kind==2?ship.GunCount+(ship.CapitalShip?NavalCapitalLayout.Turrets:0):4*Factor(ship);}
 public static int Boxes(NavalShipDefinition ship,int kind){return (OriginalBoxes(ship,kind)+1)/2;}
 public static int Units(NavalShipDefinition ship,int kind,int rack){return System.Math.Min(2,OriginalBoxes(ship,kind)-rack*2);}
 public static NavalWalkPoint Position(NavalShipDefinition ship,int n){
  if(ship.Destroyer){float[] rows={30f,-32f,6f,-10f,44f,-46f,35f,-37f,41f,-43f};return new NavalWalkPoint(0,ship.HoldFloor,rows[n]/ship.Scale); }
  int level=n%ship.MaintenanceDeckCount,slot=n/ship.MaintenanceDeckCount,row=slot/2;
  if(ship.CapitalShip){float[] capitalRows={-5f,-4.5f,-3.9f,.95f,1.55f,2.15f,2.75f,3.35f};float rackZ=capitalRows[row];if(level==0&&slot%2==0&&row==3)rackZ=2.75f;return new NavalWalkPoint(slot%2==0?-.95f:.95f,ship.MaintenanceFloor(level),rackZ);}
  if(ship.SteamPowered){
   // Keep the forward detour corner of the second aft rack behind z=-3.90.
   // At -4.28 its full-body clearance corner was classified as part of the
   // upper stair flight and pulled cross-deck sailors out of the real treads.
   float sx=slot%2==0?-1.10f:1.10f;float[] rows={-5.00f,-4.50f,1.35f,2.10f,2.85f};
   // The forward compartment door is the only route between the central
   // watch space and the wing rooms.  Keep the final three stores inside the
   // rooms instead of placing their collision boxes on the x=.95 doorway
   // approach recorded in v247.
   // The middle-deck copy at z=3.35 overlapped the z=2.85 rack's starboard
   // detour. Native collision repeatedly stopped pump and firefighting trips
   // at (1.56,.83,2.56). Keep this reserve store in the lower wing room.
   if(row==4)return new NavalWalkPoint(sx,ship.HoldFloor,level==0?3.35f:-5.55f);
   // The last parts locker used to sit on the centreline at z=5.10. Once the
   // bow was damaged its compact collider sealed the only approach to end-wall
   // repairs. Keep it in the port wing room and leave the centre passage open.
   // The final two gun-repair lockers used to occupy the middle-deck port
   // work site at (-1.10, .825, 3.90). That is the only full-body position
   // from which the tapered forward armour belt can be patched around the
   // main-magazine trunk. Keep both heavy spare kits below, split between
   // the two wing rooms, so neither deck's access lane is sealed.
   if(row>=5)return new NavalWalkPoint(level==0?-1.10f:1.10f,ship.HoldFloor,3.90f);
   // Port lower racks leave room for the rounded boiler and engine; the
   // forward magazine lane keeps its wider .68 operator clearance.
   if(level==0&&sx<0&&row==2)sx=-.95f;
   float rz=level==0&&row==3?2.30f:rows[row];
   if(level==0&&sx<0&&row==2)rz=-5.55f;
   return new NavalWalkPoint(sx,ship.MaintenanceFloor(level),rz);
  }
  int rearRows=2;int forward=row-rearRows;
  float z=row<rearRows?-4.4f+row*1.5f/ship.Scale:1.5f+(forward*1.5f+(forward/2)*.5f)/ship.Scale;
  float x=(slot%2==0?-1f:1f)*(ship.Scale<=2?.95f:.85f);
  return new NavalWalkPoint(x,ship.MaintenanceFloor(level),z);
 }
 public static float Crossing(float fromZ,float toZ,NavalShipDefinition ship){
  float best=.8f,cost=System.Math.Abs(fromZ-best)+System.Math.Abs(toZ-best);
  int boxes=Boxes(ship,0)+Boxes(ship,1)+Boxes(ship,2);
  for(int i=0;i<boxes;i++){
   var p=Position(ship,i);float z=p.Z+2.25f/ship.Scale;
   // Cross at a group gap only outside the actual staircase footprint.
   if(z>5.6f||z< -5.6f||z> -3.8f&&z<.95f)continue;
   bool clear=true;for(int j=0;j<boxes;j++)if(System.Math.Abs(Position(ship,j).Z-z)*ship.Scale<1.15f){clear=false;break;}
   float next=System.Math.Abs(fromZ-z)+System.Math.Abs(toZ-z);
   if(clear&&next<cost){cost=next;best=z;}
  }return best;
 }
}

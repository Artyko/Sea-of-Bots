public static class NavalCargoLayout {
 public static bool IntegratedReadyUse(NavalShipDefinition ship){return ship.SteamPowered;}
 public static float RouteHalfExtent(NavalShipDefinition ship){return (ship.SteamPowered?1.40f:1.125f)/ship.Scale;}
 // Keep valid diagonal work posts near a rack, but place generated detour
 // corners farther from native bevel and CharacterController skin widths.
 public static float RouteWaypointHalfExtent(NavalShipDefinition ship){return RouteHalfExtent(ship)+(ship.SteamPowered?.25f:.10f)/ship.Scale;}
 public static NavalWalkPoint Pickup(NavalShipDefinition ship,NavalWalkPoint p){float floor=ship.HoldFloor;for(int level=0;level<ship.MaintenanceDeckCount;level++)if(ship.MaintenanceFloor(level)<=p.Y+.001f)floor=ship.MaintenanceFloor(level);float direction=ship.Partitions?-1f:ship.Scale<=2?-1f:1f;if(ship.SteamPowered){float x=System.Math.Abs(p.X)<.1f?0f:(p.X>0?.60f:-.60f),z=System.Math.Abs(p.X)<.1f?p.Z+.42f:p.Z;return new NavalWalkPoint(x,NavalBallastLayout.Surface(x,z,floor,ship),z);}float px=p.X+(p.X>0?1.65f:-1.65f)/ship.Scale*direction;return new NavalWalkPoint(px,NavalBallastLayout.Surface(px,p.Z,floor,ship),p.Z);}
 public static int Factor(NavalShipDefinition ship){return ship==NavalShipDefinition.Sloop?1:ship==NavalShipDefinition.Brigantine||ship.SteamPowered?2:4;}
 public static int Boxes(NavalShipDefinition ship,int kind){return kind==2?ship.GunCount:4*Factor(ship);}
 public static NavalWalkPoint Position(NavalShipDefinition ship,int n){
  int level=n%ship.MaintenanceDeckCount,slot=n/ship.MaintenanceDeckCount,row=slot/2;
  if(ship.SteamPowered){
   float sx=slot%2==0?-1.10f:1.10f;float[] rows={-5.00f,-4.28f,1.35f,2.10f,2.85f};
   // The forward compartment door is the only route between the central
   // watch space and the wing rooms.  Keep the final three stores inside the
   // rooms instead of placing their collision boxes on the x=.95 doorway
   // approach recorded in v247.
   if(row==4)return new NavalWalkPoint(sx,ship.MaintenanceFloor(level),3.35f);
   // The last parts locker used to sit on the centreline at z=5.10. Once the
   // bow was damaged its compact collider sealed the only approach to end-wall
   // repairs. Keep it in the port wing room and leave the centre passage open.
   if(row>=5)return new NavalWalkPoint(-1.10f,ship.MaintenanceFloor(level),3.90f);
   return new NavalWalkPoint(sx,ship.MaintenanceFloor(level),rows[row]);
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

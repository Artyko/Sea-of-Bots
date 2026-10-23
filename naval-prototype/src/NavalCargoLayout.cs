public static class NavalCargoLayout {
 public static NavalWalkPoint Pickup(NavalShipDefinition ship,NavalWalkPoint p){float floor=ship.HoldFloor;for(int level=0;level<ship.MaintenanceDeckCount;level++)if(ship.MaintenanceFloor(level)<=p.Y+.001f)floor=ship.MaintenanceFloor(level);float x=p.X+(p.X>0?1.65f:-1.65f)/ship.Scale*(ship.Scale<=2?-1:1);return new NavalWalkPoint(x,NavalBallastLayout.Surface(x,p.Z,floor,ship.Scale),p.Z);}
 public static int Factor(NavalShipDefinition ship){return ship==NavalShipDefinition.Sloop?1:ship==NavalShipDefinition.Brigantine?2:4;}
 public static int Boxes(NavalShipDefinition ship,int kind){return kind==2?ship.GunCount:4*Factor(ship);}
 public static NavalWalkPoint Position(NavalShipDefinition ship,int n){
  int level=n%ship.MaintenanceDeckCount,slot=n/ship.MaintenanceDeckCount,row=slot/2;
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

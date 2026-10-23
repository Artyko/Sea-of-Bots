using System;
using System.Collections.Generic;
class AllCrewRouteChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception ex){Console.WriteLine(ex.Message);Environment.ExitCode=1;}}
 static void Run(){int routes=0;foreach(var ship in NavalShipDefinition.Choices){
 var posts=new List<NavalWalkPoint>();posts.Add(new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));
 posts.Add(new NavalWalkPoint(-1.2f,ship.MaintenanceFloor(ship.MaintenanceDeckCount-1),-5.3f));
 for(int gun=0;gun<ship.GunCount;gun++)posts.Add(new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun)));
 for(int level=0;level<ship.MaintenanceDeckCount;level++){
  float floor=ship.MaintenanceFloor(level);
  posts.Add(new NavalWalkPoint(0,floor,-4.8f));posts.Add(new NavalWalkPoint(0,floor,NavalPlankSupplyLayout.PickupZ(ship.Scale)));posts.Add(new NavalWalkPoint(0,floor,NavalCannonSupplyLayout.PickupZ(ship.Scale)));
  for(int sailor=0;sailor<ship.CrewCount;sailor++)posts.Add(new NavalWalkPoint(NavalShipDefinition.StandbyX(ship,sailor),floor,NavalShipDefinition.StandbyZ(sailor)));
  foreach(float x in new float[]{-2.35f,2.35f})foreach(float z in new float[]{-4.8f,-1.4f,1.4f,4.8f})posts.Add(new NavalWalkPoint(x,NavalBallastLayout.Surface(x,z,floor,ship.Scale),z));
 }
 posts.Add(new NavalWalkPoint(NavalSailWorkLayout.PostX,ship.WeatherDeck,NavalSailWorkLayout.PostZ));
 for(int side=-1;side<=1;side+=2){posts.Add(new NavalWalkPoint(side*1.35f,ship.WeatherDeck,4.8f));posts.Add(new NavalWalkPoint(side*2.2f,ship.WeatherDeck,ship.LadderZ));}
 foreach(var start in posts)foreach(var goal in posts){
  var path=NavalSupplyRoutes.Plan(start,goal,ship);Check(path.Count>0,"Every task pair produces a route");var previous=start;
  foreach(var point in path){
   Check(!float.IsNaN(point.X)&&!float.IsNaN(point.Y)&&!float.IsNaN(point.Z),"No invalid route coordinates");
   Check(Math.Abs(point.X)<ship.HullHalfWidth(point.Z)&&Math.Abs(point.Z)<6,"Routes remain within hull silhouette "+ship.Name+" point="+point.X+","+point.Y+","+point.Z);
   // A real stair/ballast transition uses short vertical increments, never direct floor teleportation.
   Check(Math.Abs(point.Y-previous.Y)*ship.Scale<.65f,ship.Name+" skips height: ("+previous.X+","+previous.Y+","+previous.Z+") -> ("+point.X+","+point.Y+","+point.Z+") start="+start.X+","+start.Y+","+start.Z+" goal="+goal.X+","+goal.Y+","+goal.Z);
   for(int level=0;level<ship.MaintenanceDeckCount;level++){
    float floor=ship.MaintenanceFloor(level);
    if(Math.Abs(previous.Y-floor)<.001f&&Math.Abs(point.Y-floor)<.001f&&!(previous.Z>=.42f&&point.Z>=.42f))
     Check(!NavalSupplyRoutes.Hits(previous,point,-ship.StairWidth*.5f-.45f/ship.Scale,ship.StairWidth*.5f+.45f/ship.Scale,-3f-.45f/ship.Scale,.5f+.45f/ship.Scale),"Floor route enters staircase underside: "+ship.Name+" from="+previous.X+","+previous.Y+","+previous.Z+" to="+point.X+","+point.Y+","+point.Z);
   }
   int crateCount=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
   for(int crate=0;crate<crateCount;crate++){
    var c=NavalCargoLayout.Position(ship,crate);if(Math.Abs(previous.Y-c.Y)>.08f||Math.Abs(point.Y-c.Y)>.08f)continue;
    float half=1.10f/ship.Scale;
    Check(!NavalSupplyRoutes.Hits(previous,point,c.X-half,c.X+half,c.Z-half,c.Z+half),"Cargo "+ship.Name+" crate="+crate+" from="+previous.X+","+previous.Z+" to="+point.X+","+point.Z+" start="+start.X+","+start.Z+" goal="+goal.X+","+goal.Z);
   }
   previous=point;
  }
  Check(previous.X==goal.X&&previous.Y==goal.Y&&previous.Z==goal.Z,"Every task route preserves endpoint");routes++;
 }
 }
 Console.WriteLine("PASS: "+routes+" directed routes linking every crew standby, helm, all guns, both stock types, pump, ballast repair positions, firefighting, sails and sea-ladder landings across all ships.");
 }
}

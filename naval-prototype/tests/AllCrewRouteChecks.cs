using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
class AllCrewRouteChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){
  AppDomain.CurrentDomain.AssemblyResolve+=delegate(object sender,ResolveEventArgs args){string folder=Environment.GetEnvironmentVariable("NAVAL_GAME_MANAGED");if(String.IsNullOrEmpty(folder))return null;string path=Path.Combine(folder,new AssemblyName(args.Name).Name+".dll");return File.Exists(path)?Assembly.LoadFrom(path):null;};
  try{Run();}catch(Exception ex){Console.WriteLine(ex.Message);Environment.ExitCode=1;}
 }
 static void Run(){int routes=0;var profiles=new List<NavalShipDefinition>(NavalShipDefinition.Choices);profiles.AddRange(NavalShipDefinition.SteamChoices);foreach(var ship in profiles){
 if(ship.Destroyer)continue; // NovikChecks covers the separate centreline layout with actual work posts.
 var posts=new List<NavalWalkPoint>();posts.Add(new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));
 var tiller=ship.EmergencySteeringPost;posts.Add(ship.SteamPowered?new NavalWalkPoint(tiller.x,tiller.y,tiller.z):new NavalWalkPoint(-1.2f,ship.MaintenanceFloor(ship.MaintenanceDeckCount-1),-5.3f));
 for(int gun=0;gun<ship.GunCount;gun++)posts.Add(new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun)));
 for(int level=0;level<ship.MaintenanceDeckCount;level++){
  float floor=ship.MaintenanceFloor(level);
  posts.Add(new NavalWalkPoint(0,floor,-4.8f));posts.Add(new NavalWalkPoint(0,floor,NavalPlankSupplyLayout.PickupZ(ship.Scale)));
  // Steam ships use distributed NavalCargo pickups. The legacy central
  // fallback box is not built there and its old coordinate is now occupied by
  // the sealed automatic main-battery magazine.
  if(!ship.SteamPowered)posts.Add(new NavalWalkPoint(0,floor,NavalCannonSupplyLayout.PickupZ(ship.Scale)));
  for(int sailor=0;sailor<ship.CrewCount;sailor++)posts.Add(new NavalWalkPoint(NavalShipDefinition.StandbyX(ship,sailor),floor,NavalShipDefinition.StandbyZ(ship,sailor)));
  float[] workX=ship.SteamPowered?new float[]{-.68f,.68f}:new float[]{-2.35f,2.35f};
  foreach(float x in workX)foreach(float z in new float[]{-4.8f,-1.4f,1.4f,4.8f})posts.Add(new NavalWalkPoint(x,NavalBallastLayout.Surface(x,z,floor,ship),z));
 }
 posts.Add(new NavalWalkPoint(ship.PropulsionPost.x,ship.PropulsionPost.y,ship.PropulsionPost.z));
 for(int sailor=0;sailor<ship.CrewCount;sailor++){var rest=NavalShipDefinition.StandbyPost(ship,sailor);posts.Add(new NavalWalkPoint(rest.x,rest.y,rest.z));}
 for(int side=-1;side<=1;side+=2){float bow=ship.SteamPowered?ship.HullHalfWidth(4.8f)-.3f:1.35f,ladder=ship.HullHalfWidth(ship.LadderZ)-.35f;posts.Add(new NavalWalkPoint(side*bow,ship.WeatherDeck,4.8f));posts.Add(new NavalWalkPoint(side*ladder,ship.WeatherDeck,ship.LadderZ));}
 // Historical probe positions may now lie inside solid equipment; they are not valid task endpoints.
 if(ship.SteamPowered)posts.RemoveAll(delegate(NavalWalkPoint p){return p.Y<=ship.WeatherDeck+.01f&&!NavalSupplyRoutes.CargoClear(p,p,ship);});
 foreach(var start in posts)foreach(var goal in posts){
  var path=NavalSupplyRoutes.Plan(start,goal,ship);Check(path.Count>0,"Every task pair produces a route");var previous=start;
  foreach(var point in path){
   Check(!float.IsNaN(point.X)&&!float.IsNaN(point.Y)&&!float.IsNaN(point.Z),"No invalid route coordinates");
   if(ship.SteamPowered&&(point.X!=goal.X||point.Y!=goal.Y||point.Z!=goal.Z))Check(NavalCrewDeckSafety.Inside(point.X,point.Z,ship,.85f),"Steam intermediate routes retain full sailor clearance inside tapered hull point="+point.X+","+point.Y+","+point.Z+" start="+start.X+","+start.Y+","+start.Z+" goal="+goal.X+","+goal.Y+","+goal.Z);
   // A real stair/ballast transition uses short vertical increments, never direct floor teleportation.
   Check(Math.Abs(point.Y-previous.Y)*ship.Scale<.65f,ship.Name+" skips height: ("+previous.X+","+previous.Y+","+previous.Z+") -> ("+point.X+","+point.Y+","+point.Z+") start="+start.X+","+start.Y+","+start.Z+" goal="+goal.X+","+goal.Y+","+goal.Z);
   for(int level=0;level<ship.MaintenanceDeckCount;level++){
    float floor=ship.MaintenanceFloor(level);
    if(Math.Abs(previous.Y-floor)<.001f&&Math.Abs(point.Y-floor)<.001f&&!(previous.Z>=.42f&&point.Z>=.42f))
     Check(!NavalSupplyRoutes.Hits(previous,point,-ship.StairWidth*.5f-.45f/ship.Scale,ship.StairWidth*.5f+.45f/ship.Scale,-3f-.45f/ship.Scale,.5f+.45f/ship.Scale),"Floor route enters staircase underside: "+ship.Name+" from="+previous.X+","+previous.Y+","+previous.Z+" to="+point.X+","+point.Y+","+point.Z);
   }
   if(ship.SteamPowered&&!ship.CapitalShip&&ship.MaintenanceFloorAt(previous.Y)&&ship.MaintenanceFloorAt(point.Y)&&Math.Abs(previous.Y-point.Y)<.08f){
    float mx=NavalMainMagazineLayout.RouteHalfWidth(ship);
    Check(!NavalSupplyRoutes.Hits(previous,point,-mx,mx,NavalMainMagazineLayout.RouteAft(ship),NavalMainMagazineLayout.RouteForward(ship)),"Route crosses sealed main magazine: "+previous.X+","+previous.Z+" -> "+point.X+","+point.Z+" start="+start.X+","+start.Y+","+start.Z+" goal="+goal.X+","+goal.Y+","+goal.Z);
   }
   int crateCount=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
   for(int crate=0;crate<crateCount;crate++){
    var c=NavalCargoLayout.Position(ship,crate);if(Math.Abs(previous.Y-c.Y)>.08f||Math.Abs(point.Y-c.Y)>.08f)continue;
    float half=NavalCargoLayout.RouteHalfExtent(ship);
    Check(!NavalSupplyRoutes.Hits(previous,point,c.X-half,c.X+half,c.Z-half,c.Z+half),"Cargo "+ship.Name+" crate="+crate+" from="+previous.X+","+previous.Z+" to="+point.X+","+point.Z+" start="+start.X+","+start.Z+" goal="+goal.X+","+goal.Z);
   }
   previous=point;
  }
  Check(previous.X==goal.X&&previous.Y==goal.Y&&previous.Z==goal.Z,"Every task route preserves endpoint");routes++;
 }
 if(ship.SteamPowered&&!ship.CapitalShip){
  var loggedStart=new NavalWalkPoint(-.31f,.825f,3.40f);var loggedBow=new NavalWalkPoint(-.32f,.825f,5.50f);
  var loggedRoute=NavalSupplyRoutes.Plan(loggedStart,loggedBow,ship);var prior=loggedStart;bool detoured=false;
  foreach(var point in loggedRoute){float mx=NavalMainMagazineLayout.RouteHalfWidth(ship);Check(!NavalSupplyRoutes.Hits(prior,point,-mx,mx,NavalMainMagazineLayout.RouteAft(ship),NavalMainMagazineLayout.RouteForward(ship)),"Logged bow repair route crosses magazine: "+prior.X+","+prior.Z+" -> "+point.X+","+point.Z);if(Math.Abs(point.X)>=mx)detoured=true;prior=point;}
  Check(detoured,"Logged bow repair route receives a magazine-side detour");
 }
 }
 Console.WriteLine("PASS: "+routes+" directed routes linking every crew standby, helm, all guns, both stock types, pump, ballast repair positions, firefighting, propulsion station and sea-ladder landings across all ships.");
 }
}

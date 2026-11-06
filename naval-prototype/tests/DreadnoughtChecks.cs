using System;
using System.Collections.Generic;
using UnityEngine;
class DreadnoughtChecks
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  var ship=NavalShipDefinition.Dreadnought;
  Check(ship.SteamPowered&&ship.CapitalShip&&ship.HeavyCrewCount==5&&ship.CrewCount==16,"Capital profile and five staffed turrets");
  Check(Math.Abs(ship.HoldClearance-NavalShipDefinition.Ironclad.HoldClearance)<.001f&&Math.Abs(ship.UpperDeckOffset*ship.Scale-4.8f)<.001f,"Hold heights preserve repair reach and camera clearance");
  Check(NavalShipDefinition.SteamChoices.Length==3&&NavalShipDefinition.SteamChoices[2]==NavalShipDefinition.Novik,"Three steam ship classes remain selectable");
  Check(NavalCapitalLayout.CalibreMm==305&&NavalCapitalLayout.Power>NavalMainBatteryMath.ProjectilePower,"Distinct heavier calibre");
  Check(NavalCapitalLayout.Bears(0,0)&&NavalCapitalLayout.Bears(3,180)&&!NavalCapitalLayout.Bears(1,90),"Fore/aft/wing arcs prevent cross-deck fire");
  foreach(float bearing in new[]{-90f,90f}){int mounts=0;for(int i=0;i<5;i++)if(NavalCapitalLayout.Bears(i,bearing))mounts++;Check(mounts==4,"Four twin turrets bear on each broadside");}
  NavalSessionSettings.Configure(NavalEra.Steam);NavalSessionSettings.PlayerShip=ship;NavalSessionSettings.EnemyShip=ship;NavalSessionSettings.Configure(NavalEra.Steam);Check(NavalSessionSettings.PlayerShip==ship&&NavalSessionSettings.EnemyShip==ship,"Ship selection survives arena restart");
  var corvette=NavalShipDefinition.Ironclad;
  Check(Math.Abs(ship.DeckThickness*ship.Scale-corvette.DeckThickness*corvette.Scale)<.001f,"Equal physical deck slab thickness");
  Check(Math.Abs((ship.UpperDeckOffset-ship.DeckThickness)*ship.Scale-(corvette.UpperDeckOffset-corvette.DeckThickness)*corvette.Scale)<.001f,"Equal usable middle-deck headroom");
  Check(NavalCapitalLayout.Radius>Math.Sqrt(.43f*.43f+.375f*.375f),"Navigation encloses the rotating turret roof corners");
  float raisedBore=NavalCapitalLayout.TurretRise(3)+NavalTurretAccessLayout.RootLift+.32f;
  float lowerRoof=NavalTurretAccessLayout.RootLift+NavalTurretAccessLayout.RoofTop;
  Check(raisedBore+(float)Math.Sin(NavalCapitalLayout.MinimumPitch(3)*Math.PI/180)*1.6f>lowerRoof+.09f,"Raised stern tubes clear the lower roof at their depression limit");
  var spawn=NavalCapitalLayout.PlayerStart(ship);Check(Math.Abs(spawn.z)<1&&Math.Abs(spawn.x)>.75f&&NavalCapitalLayout.WeatherClear(new NavalWalkPoint(spawn.x,spawn.y,spawn.z),new NavalWalkPoint(spawn.x,spawn.y,spawn.z),ship),"Midship player start has support outside the open stairs");
  foreach(float side in new[]{-1f,1f})Check(!ship.IsOpening(side*ship.HullHalfWidth(ship.LadderZ),ship.WeatherDeck+.1f,ship.LadderZ,.1f,.1f,.1f),"Sea ladder leaves weather-deck armor closed");
  var helm=new NavalWalkPoint(0,ship.WeatherDeck,NavalCapitalLayout.BridgeZ);
  var landing=new NavalWalkPoint(0,ship.WeatherDeck,.8f);
  int routes=0;
  for(int slot=0;slot<5;slot++){
   Vector3 p=NavalTurretAccessLayout.Approach(ship,slot);Check(NavalCrewDeckSafety.Inside(p.x,p.z,ship,.8f),"Hatch approach remains inside the hull");
   CheckRoute(helm,new NavalWalkPoint(p.x,p.y,p.z),ship);CheckRoute(landing,new NavalWalkPoint(p.x,p.y,p.z),ship);routes+=2;
  }
  for(int gun=0;gun<ship.GunCount;gun++){CheckRoute(helm,new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.WeatherDeck,NavalShipDefinition.GunZ(ship,gun)),ship);routes++;}
  int boxes=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
  for(int i=0;i<boxes;i++){
   var c=NavalCargoLayout.Position(ship,i);var goal=NavalCargoLayout.Pickup(ship,c);
   for(int turret=0;turret<5;turret++){var mount=NavalCapitalLayout.Mount(turret);
    Check(Math.Abs(c.X-mount.x)>(NavalMainMagazineLayout.Width*.5f+.525f)/ship.Scale||Math.Abs(c.Z-mount.z)>(NavalMainMagazineLayout.Depth*.5f+.525f)/ship.Scale,"Cargo and barbette do not intersect");
   }
   Check(NavalCrewDeckSafety.Inside(c.X,c.Z,ship,.6f),"Cargo remains inside hull");
   for(int j=0;j<i;j++){var other=NavalCargoLayout.Position(ship,j);Check(Math.Abs(c.Y-other.Y)>.1f||Math.Abs(c.X-other.X)*ship.Scale>1.05f||Math.Abs(c.Z-other.Z)*ship.Scale>1.05f,"Unique non-intersecting stores");}
   Check(!NavalCapitalLayout.MagazineBlocks(goal,goal,ship),"Pickup outside protected magazine");
   CheckRoute(helm,goal,ship);routes++;
  }
  for(int level=0;level<2;level++){CheckRoute(helm,new NavalWalkPoint(0,ship.MaintenanceFloor(level),-4.9f+.65f/ship.Scale),ship);routes++;}
  Console.WriteLine("PASS: Dreadnought five twin 305-mm mounts, 16 crew, sector masks, unique supplies and "+routes+" helm/landing-to-duty routes.");
 }
 static void CheckRoute(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship){
  var route=NavalSupplyRoutes.Plan(start,goal,ship);Check(route.Count>0,"Route exists");var previous=start;
  foreach(var point in route){
   bool bridge=NavalRaisedBridge.Flight(previous,ship)||NavalRaisedBridge.Flight(point,ship)||Math.Abs(previous.Z-NavalCapitalLayout.BridgeZ)<.7f&&Math.Abs(previous.X)<.3f||Math.Abs(point.Z-NavalCapitalLayout.BridgeZ)<.7f&&Math.Abs(point.X)<.3f;
   if(previous.Y>=ship.WeatherDeck-.005f&&point.Y>=ship.WeatherDeck-.005f&&!bridge)
    Check(NavalCapitalLayout.WeatherClear(previous,point,ship),"Weather route intersects turret/stair/stack: "+Fmt(previous)+" -> "+Fmt(point)+" goal="+Fmt(goal));
   Check(!NavalCapitalLayout.MagazineBlocks(previous,point,ship),"Route cuts a protected magazine: "+Fmt(previous)+" -> "+Fmt(point)+" goal="+Fmt(goal));
   Check(Math.Abs(point.Y-previous.Y)*ship.Scale<.7f,"Continuous stair steps: "+Fmt(previous)+" -> "+Fmt(point)+" goal="+Fmt(goal));previous=point;
  }
  Check(Math.Abs(previous.X-goal.X)+Math.Abs(previous.Y-goal.Y)+Math.Abs(previous.Z-goal.Z)<.01f,"Exact destination");
 }
 static string Fmt(NavalWalkPoint p){return p.X+","+p.Y+","+p.Z;}
}

using System;
using System.Collections.Generic;
class CrewBarrelRoutesChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static double Distance(NavalWalkPoint a,NavalWalkPoint b,float x,float z){double dx=b.X-a.X,dz=b.Z-a.Z,t=dx*dx+dz*dz>0?((x-a.X)*dx+(z-a.Z)*dz)/(dx*dx+dz*dz):0;t=Math.Max(0,Math.Min(1,t));return Math.Sqrt(Math.Pow(a.X+t*dx-x,2)+Math.Pow(a.Z+t*dz-z,2));}
 static void Route(NavalWalkPoint start,NavalWalkPoint goal,NavalShipDefinition ship){
 var points=NavalShipRoutes.Plan(start,goal,ship);var previous=start;
 foreach(var point in points){
 for(int barrel=0;barrel<NavalPowderLayout.Count(ship);barrel++){
 float floor=NavalPowderLayout.Floor(ship);
 // Include upright full-size body height during stair ascent, not just endpoints on deck.
 if(Math.Max(previous.Y,point.Y)+2.6f/ship.Scale>floor&&Math.Min(previous.Y,point.Y)<floor+NavalPowderLayout.Height/ship.Scale){
 double clearance=Distance(previous,point,NavalPowderLayout.X(ship,barrel),NavalPowderLayout.Z(ship,barrel))*ship.Scale;
 Check(clearance>NavalPowderLayout.Diameter*.5f+.38f+.08f,ship.Name+" barrel "+barrel+" blocks route segment clearance="+clearance);
 }}previous=point;}
 Check(points.Count>0&&Math.Abs(previous.X-goal.X)+Math.Abs(previous.Y-goal.Y)+Math.Abs(previous.Z-goal.Z)<.001,"Route must reach requested station");
 }
 static void Main(){
 // The former first barrel overlapped the left exit waypoint on the sloop.
 Check(Distance(new NavalWalkPoint(0,.825f,.8f),new NavalWalkPoint(-1.4f,.825f,.8f),-1.15f,.8f)*2<NavalPowderLayout.Diameter*.5f+.38f,"Regression must catch old obstructed stair exit");
 int routes=0;foreach(var ship in NavalShipDefinition.Choices){
 for(int barrel=0;barrel<NavalPowderLayout.Count(ship);barrel++){
 float bx=NavalPowderLayout.X(ship,barrel),bz=NavalPowderLayout.Z(ship,barrel);
 if(ship.GunCount>=4){int first=ship.GunCount>4&&barrel>=2?2:0;Check(Math.Abs(bz-(NavalShipDefinition.GunZ(ship,first)+NavalShipDefinition.GunZ(ship,first+2))*.5f)<.001,"Barrel must stand midway between adjacent guns");}
 foreach(float side in new float[]{-1,1}){
 Check(Distance(new NavalWalkPoint(side*1.4f,ship.WeatherDeck,-5),new NavalWalkPoint(side*1.4f,ship.WeatherDeck,5),bx,bz)*ship.Scale>NavalPowderLayout.Diameter*.5f+.46f,"Main side corridor stays open");
 Check(Distance(new NavalWalkPoint(side*3.7f,ship.WeatherDeck,ship.LadderZ),new NavalWalkPoint(side*2.2f,ship.WeatherDeck,ship.LadderZ),bx,bz)*ship.Scale>NavalPowderLayout.Diameter*.5f+.46f,"Sea ladder landing stays open");
 }}
 var goals=new List<NavalWalkPoint>();
 for(int gun=0;gun<ship.GunCount;gun++)goals.Add(new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun)));
 for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)goals.Add(new NavalWalkPoint(NavalSailWorkLayout.PostX+x*.55f/ship.Scale,ship.WeatherDeck,NavalSailWorkLayout.PostZ+z*.75f/ship.Scale));
 goals.Add(new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));
 var starts=new List<NavalWalkPoint>();for(int sailor=0;sailor<ship.CrewCount;sailor++)starts.Add(new NavalWalkPoint(NavalShipDefinition.StandbyX(sailor),ship.HoldFloor,NavalShipDefinition.StandbyZ(sailor)));
 starts.Add(new NavalWalkPoint(0,ship.HoldFloor,-4.9f));
 foreach(var start in starts)foreach(var goal in goals){Route(start,goal,ship);Route(goal,start,ship);routes+=2;}
 foreach(var start in goals)foreach(var goal in goals){Route(start,goal,ship);routes++;}
 }Console.WriteLine("PASS: "+routes+" routes, full-body barrel clearance on stairs and decks, guns, helm, sail approach variants and reverse trips.");}
}

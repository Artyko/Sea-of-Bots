using System;
using System.Collections.Generic;
class SteamAllRoutesChecks {
 static string F(NavalWalkPoint p){return p.X+","+p.Y+","+p.Z;}
 static void Main(){int routes=0,bad=0;
 foreach(var ship in new[]{NavalShipDefinition.Ironclad,NavalShipDefinition.Dreadnought}){
 // At the first point aft of the engine exclusion, the shaft must be below step height.
 float fraction=.63f/4.69f;
 float shaftY=ship.HoldFloor+.03f+(NavalSteamLayout.ScrewY(ship)-ship.HoldFloor-.03f)*fraction;
 if((shaftY+.05f-ship.HoldFloor)*ship.Scale>.24f)throw new Exception("Shaft protrudes into aft walking lane");
 if(NavalSteamLayout.MainHeight(ship)*ship.Scale-.12f*ship.Scale*.5f<2.5f+.15f)throw new Exception("Steam main lacks head clearance");
 var posts=new List<NavalWalkPoint>();
 posts.Add(new NavalWalkPoint(0,ship.HelmDeck,NavalShipDefinition.HelmZ(ship)));
 for(int g=0;g<ship.GunCount;g++)posts.Add(new NavalWalkPoint(NavalShipDefinition.GunSide(g)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,g)));
 for(int deck=0;deck<ship.MaintenanceDeckCount;deck++)posts.Add(new NavalWalkPoint(0,ship.MaintenanceFloor(deck),-4.9f+.65f/ship.Scale));
 int boxes=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
 for(int i=0;i<boxes;i++)posts.Add(NavalCargoLayout.Pickup(ship,NavalCargoLayout.Position(ship,i)));
 for(int i=0;i<ship.CrewCount;i++)posts.Add(new NavalWalkPoint(NavalShipDefinition.StandbyX(ship,i),NavalShipDefinition.StandbyY(ship,i),NavalShipDefinition.StandbyZ(ship,i)));
 for(int a=0;a<posts.Count;a++)for(int b=0;b<posts.Count;b++)if(a!=b){
 var route=NavalSupplyRoutes.Plan(posts[a],posts[b],ship);var prior=posts[a];bool failed=false;
 foreach(var point in route){if(NavalSteamLayout.BlocksPassage(prior,point,ship)){if(bad<8)Console.WriteLine("BLOCK "+ship.Name+" "+a+"->"+b+" "+F(prior)+" -> "+F(point));failed=true;break;}prior=point;}
 if(failed)bad++;routes++;
 }
 }
 if(bad>0)throw new Exception(bad+" of "+routes+" routes intersect machinery");
 Console.WriteLine("PASS: "+routes+" directed service routes clear the machinery envelopes on both steam profiles.");
 }
}

using System;
class PlankSupplyChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
 var boards=new NavalPlankBundle();Check(!boards.Refill(false),"Broken stock cannot supply boards");
 Check(boards.Refill(true)&&boards.Count==4&&!boards.Refill(true),"One bundle, no duplication");
 for(int i=0;i<4;i++)Check(boards.Consume()&&boards.Count==3-i,"One board per patch, carried reserve survives stock loss");
 Check(!boards.Consume()&&boards.Refill(true),"Exhausted reserve needs new stock");boards.Drop();Check(boards.Count==0,"Lost bundle cleared");
 int checkedRoutes=0;
 foreach(var ship in NavalShipDefinition.Choices){
 Check(NavalPlankSupplyLayout.PickupZ(ship.Scale)>.8f,"Pickup clears stairs");
 Check(1.4f*ship.Scale-NavalPlankSupplyLayout.Width*.5f>.5f,"Side corridors clear square stock");
 Check((NavalCannonSupplyLayout.Z-NavalPlankSupplyLayout.Z)*ship.Scale-(NavalPlankSupplyLayout.Depth+NavalCannonSupplyLayout.Depth)*.5f>1f,"Both stocks have body clearance between them");
 var pickup=new NavalWalkPoint(0,ship.HoldFloor,NavalPlankSupplyLayout.PickupZ(ship.Scale));
 var destinations=new NavalWalkPoint[]{new NavalWalkPoint(0,ship.HoldFloor,NavalCannonSupplyLayout.PickupZ(ship.Scale)),new NavalWalkPoint(0,ship.HoldFloor,-4.9f),new NavalWalkPoint(-1.65f,ship.HoldFloor,5),new NavalWalkPoint(1.65f,ship.HoldFloor,5),new NavalWalkPoint(ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,0))};
 foreach(var goal in destinations){
 foreach(bool reverse in new bool[]{false,true}){
 var previous=reverse?goal:pickup;var target=reverse?pickup:goal;var route=NavalSupplyRoutes.Plan(previous,target,ship);
 foreach(var point in route){
 if(Math.Abs(point.Y-ship.HoldFloor)<.15&&Math.Abs(previous.Y-ship.HoldFloor)<.15){
 foreach(bool cannon in new bool[]{false,true}){
 float width=cannon?NavalCannonSupplyLayout.Width:NavalPlankSupplyLayout.Width,depth=cannon?NavalCannonSupplyLayout.Depth:NavalPlankSupplyLayout.Depth,z=cannon?NavalCannonSupplyLayout.Z:NavalPlankSupplyLayout.Z;
 float half=(width*.5f+.38f)/ship.Scale;
 Check(!NavalSupplyRoutes.Hits(previous,point,-half,half,z-(depth*.5f+.38f)/ship.Scale,z+(depth*.5f+.38f)/ship.Scale),ship.Name+" stock obstructs supply/repair route");
 }}previous=point;}
 Check(previous.X==target.X&&previous.Y==target.Y&&previous.Z==target.Z,"Detour preserves task destination");checkedRoutes++;
 }}
 }
 Console.WriteLine("PASS: four-patch inventory, damaged stock, replenishment, square crate clearance and "+checkedRoutes+" supply/pump/gun/repair routes.");
 }
}

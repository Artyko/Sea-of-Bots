using System;
class CargoGraphReuseChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){int routes=0;
 foreach(var ship in NavalShipDefinition.Choices){NavalSupplyRoutes.Warm(ship);int builds=NavalSupplyRoutes.GraphBuildCount;NavalSupplyRoutes.Warm(ship);Check(builds==NavalSupplyRoutes.GraphBuildCount,"Warm graph must be reused");
 int total=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
 for(int n=0;n<total;n++)for(int gun=0;gun<ship.GunCount;gun++)for(int dir=0;dir<2;dir++){
 var pickup=NavalCargoLayout.Pickup(ship,NavalCargoLayout.Position(ship,n));var post=new NavalWalkPoint(NavalShipDefinition.GunSide(gun)*ship.GunPostX,ship.GunDeck,NavalShipDefinition.GunZ(ship,gun));var start=dir==0?post:pickup;var goal=dir==0?pickup:post;
 var actual=NavalSupplyRoutes.Plan(start,goal,ship);var expected=ReferenceSupplyRoutes.Plan(start,goal,ship);
 Check(actual.Count==expected.Count,"Same number of route points "+ship.Name);
 for(int i=0;i<actual.Count;i++)Check(actual[i].X==expected[i].X&&actual[i].Y==expected[i].Y&&actual[i].Z==expected[i].Z,"Same complete ordered route "+ship.Name);routes++;
 }
 builds=NavalSupplyRoutes.GraphBuildCount;
 for(int n=0;n<total;n++)NavalSupplyRoutes.Plan(new NavalWalkPoint(-2.35f,ship.HoldFloor,4),NavalCargoLayout.Pickup(ship,NavalCargoLayout.Position(ship,n)),ship);
 int warm=NavalSupplyRoutes.GraphBuildCount;
 for(int n=0;n<total;n++)NavalSupplyRoutes.Plan(new NavalWalkPoint(-2.35f,ship.HoldFloor,4),NavalCargoLayout.Pickup(ship,NavalCargoLayout.Position(ship,n)),ship);
 Check(warm==NavalSupplyRoutes.GraphBuildCount,"Repeated requests must reuse nodes and edges");
 }
 Console.WriteLine("PASS: "+routes+" full routes exactly equal uncached reference; repeated ship/deck graph requests reuse nodes and edge checks.");
 }
}

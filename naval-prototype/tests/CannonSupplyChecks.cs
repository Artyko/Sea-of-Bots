using System;
class CannonSupplyChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
 var kit=new NavalCannonKit();Check(!kit.Take(false)&&!kit.Deliver(),"Destroyed crate cannot issue a kit or permit imaginary delivery");
 Check(kit.Take(true)&&kit.Held,"Intact stock issues one carried kit");Check(!kit.Take(true),"Hands cannot duplicate a kit");
 Check(kit.Deliver()&&!kit.Held&&!kit.Deliver(),"Already fetched kit is delivered exactly once even after stock was destroyed");
 kit.Take(true);kit.Drop();Check(!kit.Held&&!kit.Deliver(),"Death or abandonment drops undelivered parts");
 foreach(var ship in NavalShipDefinition.Choices){
 float front=NavalCannonSupplyLayout.Z-NavalCannonSupplyLayout.Depth/(2*ship.Scale);
 Check((front-NavalCannonSupplyLayout.PickupZ(ship.Scale))*ship.Scale>.46f,"Full-size body fits in front of crate");
 Check(1.4f*ship.Scale-NavalCannonSupplyLayout.Width*.5f>.46f,"Both main side corridors clear the crate");
 Check(NavalCannonSupplyLayout.PickupZ(ship.Scale)>.85f,"Crate pickup is outside staircase");
 var destination=new NavalWalkPoint(0,ship.HoldFloor,NavalCannonSupplyLayout.PickupZ(ship.Scale));
 foreach(float side in new float[]{-1,1}){
 var start=new NavalWalkPoint(side*ship.GunPostX,ship.WeatherDeck,NavalShipDefinition.GunZ(ship,0));
 var route=NavalShipRoutes.Plan(start,destination,ship);var last=route[route.Count-1];
 Check(last.Y==ship.HoldFloor&&last.Z==destination.Z,"Repairer reaches lower-hold stock");
 var back=NavalShipRoutes.Plan(last,start,ship);Check(back[back.Count-1].Y==ship.WeatherDeck,"Carried kit returns to gun deck");}
 }
 var repair=new NavalRepairProgress();for(int i=0;i<180;i++)repair.StepAccepted(.5f,false);
 Check(repair.Elapsed==0,"Travel and fetching do not count toward repair minute");
 for(int i=0;i<120;i++)repair.StepAccepted(.5f,true);
 var budget=new NavalCannonRepairBudget();Check(budget.Complete(true,repair.Elapsed)&&!budget.Complete(true,120),"Only one full minute-long repair per gun");
 Console.WriteLine("PASS: destroyed stock, no kit duplication, carried-kit survival, lost parts, crate/body clearance and gun/hold round trips on all profiles, separate repair minute.");}
}

using System;
class SteamRefitChecks
{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  var ship=NavalShipDefinition.Ironclad;Check(!ship.Partitions,"Old bulkheads are removed from geometry, routes and flooding");
  var latch=new NavalCrewReleaseLatch();Check(latch.Step(.1f,1,false)==1,"Immediate emergency release");
  for(int i=0;i<30;i++)Check(latch.Step(1f,i%2,false)==1,"Oscillating demand cannot recall the gunner");
  for(int i=0;i<20;i++)Check(latch.Step(1f,0,true)==1,"Finish accepted emergency work");
  for(int i=0;i<9;i++)Check(latch.Step(1f,0,false)==1,"Require sustained calm before recall");
  Check(latch.Step(1f,0,false)==0,"Return to main battery after calm");
  var heat=new NavalFireHeatState();for(int i=0;i<7;i++)Check(!heat.Step(1,1),"No instant thermal destruction");
  Check(heat.Step(1,1),"Sustained fire damages equipment");heat.Step(5,1);heat.Step(4,0);Check(heat.Heat==0,"Cooling removes heat");
  Check(!NavalDeckGapMath.IsDeckLayer(ship.HoldFloor+.06875f,.1375f,ship),"A missing partition foot is not a deck hole");
  Check(NavalDeckGapMath.IsDeckLayer(.7f,.25f,ship),"Real deck planks are detected");
  foreach(float z in new[]{-6f,6f}){
   float area=0,half=ship.HullHalfWidth(z);
   for(int i=0;i<23;i++){float cx,width;if(NavalHullOutline.ClipEndCell(-2.75f+i*.25f,half,.25f,out cx,out width))area+=width;}
   Check(Math.Abs(area-2*half)<.0001f,"End armour spans the complete stem without two uncovered side strips");
  }
  int count=NavalCargoLayout.Boxes(ship,0)+NavalCargoLayout.Boxes(ship,1)+NavalCargoLayout.Boxes(ship,2);
  var tank=NavalBallastLayout.For(ship);
  for(int i=0;i<count;i++){
   var c=NavalCargoLayout.Position(ship,i);float half=.525f/ship.Scale;
   Check(Math.Abs(c.X)+half<tank.X-tank.Width*.5f,"Cargo stays inboard of tanks");
   for(int j=0;j<i;j++){
    var other=NavalCargoLayout.Position(ship,j);
    Check(Math.Abs(c.Y-other.Y)>.1f||Math.Abs(c.X-other.X)*ship.Scale>1.05f||Math.Abs(c.Z-other.Z)*ship.Scale>1.05f,"Two stores occupy the same volume");
   }
   var goal=NavalCargoLayout.Pickup(ship,c);var start=new NavalWalkPoint(0,c.Y,-3.65f);
   Check(!NavalSteamLayout.BlocksPassage(goal,goal,ship),"Pickup outside machinery envelope");
   var route=NavalSupplyRoutes.Plan(start,goal,ship);Check(route.Count>0,"Cargo route exists");
   var prior=start;foreach(var point in route){Check(!NavalSteamLayout.BlocksPassage(prior,point,ship),"Supply route intersects machinery: crate="+i+" at="+point.X+","+point.Z);prior=point;}
  }
  foreach(float power in new[]{1f,1.55f,5.2f})Check(NavalArmorMath.PenetrationPower(power,0)>power*1.85f,"All calibres receive higher penetration");
  Check(NavalShellFuse.Seconds==2f,"Armed shell time limit");
  Console.WriteLine("PASS: emergency-release hysteresis, heat damage/cooling, deck-hole classification, cargo/tank separation and supply routes past solid machinery.");
 }
}

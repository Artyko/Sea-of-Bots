using System;
class BallastDamageChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static bool Near(float a,float b){return Math.Abs(a-b)<.002f;}
 static void Main()
 {
  foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})
  {
   var grid=new NavalFloodGrid(12,24,5.75f,11.75f,ship.Scale,ship.RimY-ship.HoldFloor);
   var tank=new NavalBallastTank(NavalBallastLayout.Capacity(ship.Scale),2.45f,3);
   tank.SetDamage(true,false,grid);
   Check(tank.FloodFromSea(0)==0,"Above-water opening does not create water");
   float added=tank.FloodFromSea(tank.Capacity);Check(Near(added,tank.Capacity)&&grid.Total==0,"Submerged outer breach fills tank without spilling into hold");
   Check(tank.FloodFromSea(tank.Capacity)==0,"Repeated sea update does not duplicate stored water");
   float drained=tank.Drain(1.2f);Check(Near(drained,1.2f),"Ballast-only sea pumping can remove stored mass");
   Check(Near(tank.FloodFromSea(tank.Capacity),drained),"Unrepaired outer sea connection refills drained volume");
   float initial=tank.Total+grid.Total;tank.SetDamage(true,true,grid);
   Check(Near(tank.Water,0)&&Near(initial,grid.Total+tank.Total),"Inner damage instantly spills all stored volume without loss");
   Check(tank.FloodFromSea(tank.Capacity)==0,"Through-breach cannot recreate a full tank every tick");
   tank.SetDamage(false,true,grid);Check(tank.Broken&&tank.InnerBroken&&!tank.OuterBroken,"Partial repair leaves remaining damaged side open");
   tank.SetDamage(false,false,grid);Check(!tank.Broken,"All seal faces repaired restore the reservoir");
   int source=grid.Index(1,3);grid.Add(source,1);Check(tank.Fill(grid,source,.5f)>0,"Repaired tank accepts pumping again");
   tank.Reseal();float saved=tank.Water;Check(Near(saved,tank.Water)&&!tank.Broken,"Resealing does not delete stored water");
   float y=ship.HoldFloor+.5f/ship.Scale,sy=.2f/ship.Scale;
   Check(NavalBallastWall.Faces(2.45f,2.75f,y,3,.2f,sy,.2f,ship.HoldFloor,ship.Scale)==1,"Outer middle wall classified as sea-only");
   Check(NavalBallastWall.Faces(2.45f,2.15f,y,3,.2f,sy,.2f,ship.HoldFloor,ship.Scale)==2,"Inner middle wall classified as hold-only");
   Check(NavalBallastWall.Faces(2.45f,2.45f,y,3,.2f,sy,.2f,ship.HoldFloor,ship.Scale)==0,"Buried solid core is not an exposed seal face");
   Check(NavalBallastWall.Faces(2.45f,2.75f,ship.HoldFloor+1.21f/ship.Scale,3,.2f,.01f,.08f,ship.HoldFloor,ship.Scale)==0,"Cosmetic raised lid band does not break tank seal");
   var section=new NavalHydrostaticSection();
   Check(section.FractionBelow(2,1.2,0,-.1)==0&&section.FractionBelow(2,1.2,0,2)==1,"Sea target respects dry/full immersion");
   Check(Math.Abs(section.FractionBelow(2,1.2,0,.6)-.5)<.0001,"Waterline controls partial tank fill");
   Check(Math.Abs(section.FractionBelow(2,1.2,30,.5)-section.FractionBelow(2,1.2,-30,.5))<.0001,"Sea fill volume mirrors port/starboard roll");
  }
  Console.WriteLine("PASS: outside fill, dry opening, no duplication, ballast-to-sea mass removal, refilling unrepaired breach, inside spill, partial/full repair, seal faces and submerged fractions on both ships.");
 }
}

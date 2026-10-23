using System;
class SailWorkLayoutChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  Check(NavalSailWorkLayout.CleatOffset>.38f+.3f+.15f,"Working point clears full-size sailor and cleat crossbar with margin");
  Check(NavalSailWorkLayout.InReach(.95f*.95f,0)&&NavalSailWorkLayout.InReach(1.5f*1.5f,.2f),"Several stand points can reach rope without exact-point arrival");
  Check(!NavalSailWorkLayout.InReach(4,0)&&!NavalSailWorkLayout.InReach(1,.5f),"No remote or raised-on-cleat operation");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   float x=NavalSailWorkLayout.PostX,z=NavalSailWorkLayout.PostZ;
   Check(z>.85f&&Math.Abs(x)>ship.StairWidth*.5f+.4f/ship.Scale,"Work post stays outside stair opening");
   foreach(float mast in ship.MastZ)Check(Math.Sqrt(x*x+(z-mast)*(z-mast))*ship.Scale>ship.MastFootWidth*ship.Scale*.5f+.5f,"Post clears broad mast footings");
   for(int i=0;i<NavalPowderLayout.Count(ship);i++){float dx=x-NavalPowderLayout.X(ship,i),dz=z-NavalPowderLayout.Z(ship,i);Check(Math.Sqrt(dx*dx+dz*dz)*ship.Scale>NavalPowderLayout.Diameter*.5f+.5f,"Post clears powder barrels");}
   for(int gun=0;gun<ship.GunCount;gun++){float dx=x-NavalShipDefinition.GunSide(gun)*ship.GunMountX,dz=z-NavalShipDefinition.GunZ(ship,gun);Check(Math.Sqrt(dx*dx+dz*dz)*ship.Scale>1.6f,"Post clears cannon positions");}
  }
  Console.WriteLine("PASS: rope reach, crossbar/body separation, stairs, mast footings, powder and cannon clearance on all ships.");
 }
}

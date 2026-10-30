using System;
class PowderBlastChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  foreach(var ship in NavalShipDefinition.Choices)
  {float local=NavalPowderBlastMath.HullRadius/ship.Scale;Check(Math.Abs(local*ship.Scale-6)<.001,"Blast radius remains six world metres on every ship");}
  Check(NavalPowderBlastMath.HitsRobot(0)&&NavalPowderBlastMath.HitsRobot(25)&&NavalPowderBlastMath.HitsRobot(30.25f),"Robots in expanded blast including boundary take lethal damage");
  Check(!NavalPowderBlastMath.HitsRobot(30.26f)&&!NavalPowderBlastMath.HitsRobot(100),"Distant robots outside blast survive");
  float core=NavalArmorMath.ExplosionArmorWear(NavalAmmunitionBlastMath.ReadyPower,0,NavalAmmunitionBlastMath.ReadyHullRadius);
  float middle=NavalArmorMath.ExplosionArmorWear(NavalAmmunitionBlastMath.ReadyPower,NavalAmmunitionBlastMath.ReadyHullRadius*.5f,NavalAmmunitionBlastMath.ReadyHullRadius);
  Check(core>=NavalArmorMath.BreakWear&&middle>0&&middle<core,"Ready-ammunition blast can tear adjacent armour and decays with distance");
  Check(NavalArmorMath.ExplosionArmorWear(4,8,8)==0,"Explosion cannot damage armour outside its pressure radius");
  var barrel=new NavalPowderState();barrel.Ignite(0);Check(!barrel.Step(1,false,false)&&barrel.Lit,"Adjacent powder retains a warning fuse");
  Console.WriteLine("PASS: shared world-space blast radius, distance-decayed armour pressure, robot damage boundaries and delayed chain fuse.");
 }
}

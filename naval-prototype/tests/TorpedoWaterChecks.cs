using System;
class TorpedoWaterChecks{
 static void Main(){
  if(NavalTorpedoWaterMath.Submerged(float.NegativeInfinity,-2))throw new Exception("Empty hold cannot support torpedo");
  if(NavalTorpedoWaterMath.Submerged(-4,0)||NavalTorpedoWaterMath.Submerged(0,-.1f))throw new Exception("Air or shallow film cannot provide propulsion");
  if(!NavalTorpedoWaterMath.Submerged(0,-1.5f)||!NavalTorpedoWaterMath.Submerged(8,6.5f))throw new Exception("Ocean and flooded hold support propulsion");
  Console.WriteLine("PASS: dry hold and shallow film reject water propulsion; ocean and deep local flood allow it.");
 }
}

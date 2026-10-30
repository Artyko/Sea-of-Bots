using System;
class LedgeRecoveryChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
 foreach(float scale in new[]{2f,3f}){
  Check(!NavalLedgeRecoveryMath.Needed(.825f,scale),"Normal deck walk does not start ledge recovery");
  Check(!NavalLedgeRecoveryMath.Needed(.825f+.1f/scale,scale),"Controller skin does not count as a ledge");
  Check(!NavalLedgeRecoveryMath.Needed(.825f+.21f/scale,scale),"Moving-deck controller gap does not count as a ledge");
  Check(!NavalLedgeRecoveryMath.Needed(.825f+.35f/scale,scale),"Ledge threshold has stable boundary hysteresis");
  Check(!NavalLedgeRecoveryMath.Needed(.825f+.52f/scale,scale),"Logged moving-deck contact gap is not a ledge");
  Check(NavalLedgeRecoveryMath.Needed(.825f+.70f/scale,scale),"A genuinely raised footing starts ledge recovery");
  Check(NavalLedgeRecoveryMath.Needed(.825f+.8f/scale,scale),"Standing on bulwark needs recovery");
  foreach(float side in new[]{-1f,1f}){
   float x=NavalLedgeRecoveryMath.InwardX(side*3f),z=NavalLedgeRecoveryMath.InwardZ(side*6f);
   Check(Math.Abs(x)<=2.1f&&Math.Abs(z)<=5f,"Landing search starts inside both side and end rails");
   Check((3f-Math.Abs(x))*scale>.38f+.12f,"Target has clearance for full capsule");
  }
  Check(!NavalLedgeRecoveryMath.Arrived(.25f/scale,0f,0f,scale),"Do not start dropping while still outside the safe column");
  Check(NavalLedgeRecoveryMath.Arrived(.05f/scale,.1f/scale,0f,scale),"Accept skin offset after full horizontal clearance");
  Check(!NavalLedgeRecoveryMath.Arrived(0f,.4f/scale,0f,scale),"Do not finish landing while still above deck");
 }
 Check(Math.Abs(NavalLedgeRecoveryMath.DeckBelow(1.14f,-.81f,2.025f,3)-.825f)<.001f,"Logged cargo-top footing belongs to the middle deck");
 Check(Math.Abs(NavalLedgeRecoveryMath.DeckBelow(2.20f,-.81f,2.025f,3)-2.025f)<.001f,"Raised main mount belongs to the weather deck");
 Check(Math.Abs(NavalLedgeRecoveryMath.DeckBelow(-.55f,-.81f,2.025f,3)+.81f)<.001f,"Lower-hold obstacles recover to the hold floor");
 Check(NavalLedgeRecoveryMath.Detached(31f,2.2f,-15f,1f,2.025f,4f),"Logged moving-deck separation is detected");
 Check(!NavalLedgeRecoveryMath.Detached(.7f,2.1f,-6.27f,1.1f,2.025f,4f),"A sailor brushing the logged tapered stern is not teleported to the bow");
 Check(!NavalLedgeRecoveryMath.Detached(1.7f,2.2f,-2.8f,2.2f,2.025f,4f),"A sailor using the weather-deck bypass is not detached");
 Check(!NavalLedgeRecoveryMath.Detached(20f,.5f,20f,1f,2.025f,4f),"Water-level overboard cases remain owned by rescue logic");
 Check(Math.Abs(NavalLedgeRecoveryMath.RecoveryZ(-15f)+5f)<.001f,"Catastrophic separation returns to the nearest stern muster, not the bow");
 Console.WriteLine("Ledge recovery checks passed");
 }
}

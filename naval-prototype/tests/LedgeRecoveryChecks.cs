using System;
class LedgeRecoveryChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
 foreach(float scale in new[]{2f,3f}){
  Check(!NavalLedgeRecoveryMath.Needed(.825f,scale),"Normal deck walk does not start ledge recovery");
  Check(!NavalLedgeRecoveryMath.Needed(.825f+.1f/scale,scale),"Controller skin does not count as a ledge");
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
 Console.WriteLine("Ledge recovery checks passed");
 }
}

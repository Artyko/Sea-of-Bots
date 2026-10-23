using System;
class LedgeWalkOffChecks {
 static void Check(bool ok){if(!ok)throw new Exception("Ledge walk-off regression");}
 static void Main(){foreach(float scale in new float[]{2,3,4}){
 Check(NavalLedgeRecoveryMath.WalkedOff(.05f/scale,-.5f/scale,.05f/scale,scale));
 Check(!NavalLedgeRecoveryMath.Arrived(.05f/scale,-.5f/scale,.05f/scale,scale));
 Check(!NavalLedgeRecoveryMath.WalkedOff(.3f/scale,0,0,scale));
 Check(!NavalLedgeRecoveryMath.WalkedOff(0,.4f/scale,0,scale));}
 Console.WriteLine("PASS: descending walk-off waypoint completes without requiring feet to return into midair; lateral/height limits retained on all ships.");}
}

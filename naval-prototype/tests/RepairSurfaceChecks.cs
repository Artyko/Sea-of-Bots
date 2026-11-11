using System;
class RepairSurfaceChecks{static void Main(){
 if(!NavalRepairPatchMath.SameSurface(0,2,0,.1f,1,1,.1f,1,1,1))throw new Exception("Same side patch must cover adjacent upper cells");
 if(NavalRepairPatchMath.SameSurface(1,0,0,.1f,1,1,.1f,1,1,1))throw new Exception("Opposite plate cannot join patch");
 if(NavalRepairPatchMath.SameSurface(0,0,0,.1f,1,1,1,.1f,1,1))throw new Exception("Different deck surface cannot join patch");
 if(NavalRepairPatchMath.SameSurface(0,3.01f,0,.1f,1,1,.1f,1,1,1))throw new Exception("Radius must remain three metres");
 Console.WriteLine("PASS: same-surface three-metre patches reject parallel walls, deck changes and remote cells.");}}

using System;
class RepairRadiusChecks{
 static void Main(){foreach(float scale in new[]{1f,5f,8f,12f}){
  if(!NavalRepairPatchMath.Within(2.999f/scale,0,0,scale)||NavalRepairPatchMath.Within(3.01f/scale,0,0,scale))throw new Exception("Repair radius must be three metres at every ship scale");
 }Console.WriteLine("PASS: three-metre world-space repair boundary for all ship scales.");}
}

using System;
class DoorChecks
{
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
 static void Main(){
  foreach(float wall in new float[]{-1.4375f,1.4375f})foreach(float door in new float[]{-2.625f,2.625f}){
   double angle=NavalDoorMath.OpenAngle(wall)*Math.PI/180;
   Check((-Math.Sin(angle))*wall>0,"Open leaf folds into room");
   Check(Math.Abs(Math.Sin(angle)*1.25)<.23,"Folded leaf does not block corridor");
   foreach(float side in new float[]{-1f,1f}){
    float tx; Check(NavalDoorMath.Detour(wall+side*.5f,door+.7f,wall+side*.5f,door+2f,wall,door,out tx),"Approach gets detour");
    Check(Math.Abs(tx-wall)>.79f,"Detour clears leaf and handle");
    Check(NavalDoorMath.Detour(tx,door+.7f,wall+side*.5f,door+2f,wall,door,out tx),"Keep clearance while walking past leaf");
    Check(!NavalDoorMath.Detour(tx,door+2f,wall+side*.5f,door+3f,wall,door,out tx),"Resume route once clear");
   }
   float target;Check(!NavalDoorMath.Detour(wall-.5f,door,wall+.5f,door,wall,door,out target),"Portal crossing stays unobstructed");
  }
  Console.WriteLine("Door checks passed");
 }
}

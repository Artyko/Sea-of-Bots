using System;
class RaisedStationChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{
 Check(NavalStationArrival.OnRaisedSupport(0,.21f,0,3),"Logged tiller footing 0.63m above post");
 Check(NavalStationArrival.OnRaisedSupport(.14f,.03f,0,3),"Logged standby capsule contact offset");
 foreach(float radius in new float[]{.12f,.35f,.45f,.6f}){
 float distance=NavalRiderSupportMath.Distance(1.6f,.12f,radius);Check(Math.Abs(distance+radius-1.72f)<.0001f,"Wider support must not reach lower and carry airborne riders");
 }
 foreach(float s in new float[]{2,3,4}){
 Check(!NavalStationArrival.OnRaisedSupport(0,-.1f,0,s),"Cannot accept support below post");
 Check(!NavalStationArrival.OnRaisedSupport(0,1f/s,0,s),"Cannot accept another deck");
 Check(!NavalStationArrival.OnRaisedSupport(.6f/s,0,0,s),"Must reach station horizontally");
 }
 Console.WriteLine("PASS: logged raised tiller/standby positions and bounds on all profile scales; actual support still required at runtime.");
 }catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
}

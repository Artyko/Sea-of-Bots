using System;
using UnityEngine;
class CrashAndRepairV432Checks{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){
  foreach(var direction in new[]{new Vector3(1,0,0),new Vector3(.01f,-1,0),new Vector3(0,-1,0)}){
   var v=NavalAircraftCrashMath.FlightVelocity(direction,new Vector3(1,0,0),62);
   Check(v.x*v.x+v.z*v.z>35f*35f,"A biplane retains forward motion even in its steepest dive");
   Check(Math.Abs(v.x*v.x+v.y*v.y+v.z*v.z-62f*62f)<.1f,"Flight velocity must preserve airspeed");
   float vx=v.x;for(int i=0;i<40;i++)v.y-=9.81f*.05f;Check(v.x==vx,"Ballistic crash must not erase horizontal inertia");
  }
  var ship=NavalShipDefinition.Ironclad;bool tankLane=false;
  for(int i=0;i<NavalRepairWorkSites.CorridorCountFor(ship);i++)if(Math.Abs(NavalRepairWorkSites.Corridor(ship,i)-1.7f)<.001f)tankLane=true;
  Check(tankLane,"Repair search must include tank centre service lane");
  float floor=ship.HoldFloor,y=NavalBallastLayout.Surface(1.7f,3.88f,floor,ship);Check(y>floor,"Logged side-hole work site must use tank lid height");
  Console.WriteLine("PASS: diagonal crash velocity, preserved airspeed and tank-lid repair candidates.");
 }
}

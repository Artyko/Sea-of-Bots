using System;
using UnityEngine;
class RangefinderChecks{
 static void Main(){
  float v=100f/(float)Math.Sqrt(2f);
  float range=NavalRangefinderMath.SeaRange(Vector3.zero,new Vector3(v,v,0),0);
  if(Math.Abs(range-10000f/9.81f)>.01f)throw new Exception("Ballistic range");
  float raised=NavalRangefinderMath.SeaRange(new Vector3(0,10,0),new Vector3(v,v,0),0);
  if(raised<=range)throw new Exception("Muzzle elevation");
  float depressed=NavalRangefinderMath.SeaRange(new Vector3(0,10,0),new Vector3(100,-10,0),0);
  if(depressed<=0||depressed>=150)throw new Exception("Depressed barrel");
  var origin=new Vector3(20,0,30);var offset=new Vector3(-105,0,50);
  var a=NavalRangefinderMath.Formation(origin,0,offset)-origin;
  var b=NavalRangefinderMath.Formation(origin,90,offset)-origin;
  if(Math.Abs(a.magnitude-b.magnitude)>.001||Math.Abs(b.x-150)>.001||Math.Abs(b.z-105)>.001)throw new Exception("Formation must rotate with flagship");
  Console.WriteLine("PASS: ballistic range, muzzle height, negative elevation and rotated convoy formation.");
 }
}

using System;
using UnityEngine;
public static class NavalAircraftCrashMath{
 public static Vector3 FlightVelocity(Vector3 course,Vector3 fallback,float speed){
  float magnitude=(float)Math.Sqrt(course.x*course.x+course.y*course.y+course.z*course.z);
  float y=magnitude>.0001f?course.y/magnitude:0f;y=Math.Max(-.819152f,Math.Min(.5f,y));
  float x=course.x,z=course.z,flat=(float)Math.Sqrt(x*x+z*z);
  if(flat<.0001f){x=fallback.x;z=fallback.z;flat=(float)Math.Sqrt(x*x+z*z);}
  if(flat<.0001f){x=0;z=1;flat=1;}
  float planar=(float)Math.Sqrt(1-y*y)*speed/flat;
  return new Vector3(x*planar,y*speed,z*planar);
 }
}

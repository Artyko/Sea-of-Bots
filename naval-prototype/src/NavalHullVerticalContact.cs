using System;
using UnityEngine;
// Hull-only vertical bounds: masts, funnels and rigging have separate contact handling.
public static class NavalHullVerticalContact
{
 public static void Bounds(NavalShipDefinition ship,Vector3 position,Quaternion rotation,out float low,out float high){
  float bottom=ship.HoldFloor-.25f,top=Math.Max(ship.WeatherDeck,ship.RimY);
  Vector3 right=rotation*Vector3.right,up=rotation*Vector3.up,forward=rotation*Vector3.forward;
  float centre=position.y+up.y*(top+bottom)*.5f*ship.Scale;
  float extent=Math.Abs(right.y)*ship.HullHalfWidth(0)*ship.Scale+Math.Abs(up.y)*(top-bottom)*.5f*ship.Scale+Math.Abs(forward.y)*BoatDimensions.HalfLength*ship.Scale;
  low=centre-extent;high=centre+extent;
 }
 public static bool Separated(float lowA,float highA,float lowB,float highB){return lowA>highB+.05f||lowB>highA+.05f;}
 public static bool SweepSeparated(NavalShipDefinition a,Vector3 start,Quaternion rotation,Vector3 next,Quaternion nextRotation,NavalShipDefinition b,Vector3 other,Quaternion otherRotation){
  float lo,hi,nlo,nhi,blo,bhi;Bounds(a,start,rotation,out lo,out hi);Bounds(a,next,nextRotation,out nlo,out nhi);Bounds(b,other,otherRotation,out blo,out bhi);
  // Reserve the maximum intermediate rotation excursion, not just endpoint bounds.
  float angle=Quaternion.Angle(rotation,nextRotation)*(float)Math.PI/180f;
  float radius=a.Scale*(float)Math.Sqrt(BoatDimensions.HalfLength*BoatDimensions.HalfLength+a.HullHalfWidth(0)*a.HullHalfWidth(0)+(a.RimY-a.HoldFloor)*(a.RimY-a.HoldFloor));
  float arc=radius*Math.Min(angle,1f);
  return Separated(Math.Min(lo,nlo)-arc,Math.Max(hi,nhi)+arc,blo,bhi);
 }
}

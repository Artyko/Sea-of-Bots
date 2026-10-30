using System;
public static class NavalRiderSupportMath {
 // Widen the lateral footprint without extending the old cast's vertical reach.
 public static float Distance(float originalDistance,float originalRadius,float radius)
 {return Math.Max(.01f,originalDistance+originalRadius-radius);}
 // Reject the other vessel before asking PhysX about thousands of compound
 // shapes. Coordinates are ship-local; padding is the world-space capsule
 // radius converted by the ship scale.
 public static bool NearShip(float x,float z,float worldRadius,float shipScale)
 {
  double padding=worldRadius/Math.Max(.01,Math.Abs(shipScale))+.35;
  return Math.Abs(x)<=BoatDimensions.HalfWidth+padding&&Math.Abs(z)<=BoatDimensions.HalfLength+padding;
 }
}

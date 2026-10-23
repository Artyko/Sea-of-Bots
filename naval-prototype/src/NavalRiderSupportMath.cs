using System;
public static class NavalRiderSupportMath {
 // Widen the lateral footprint without extending the old cast's vertical reach.
 public static float Distance(float originalDistance,float originalRadius,float radius)
 {return Math.Max(.01f,originalDistance+originalRadius-radius);}
}

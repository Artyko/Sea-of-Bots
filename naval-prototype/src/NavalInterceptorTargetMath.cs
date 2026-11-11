using System;
public static class NavalInterceptorTargetMath{
 public static float Score(float distanceSquared,bool flagship,float sideAspect){
  return distanceSquared*(flagship?.85f:1f)*(1f-.15f*Math.Max(0f,Math.Min(1f,sideAspect)));
 }
}

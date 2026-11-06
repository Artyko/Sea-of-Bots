using System;
public static class NavalTorpedoMath {
 public const float BlastRadius=5.015f,BlastPower=21f,BlastArmorPower=15f,CrewBlastRadius=8f;
 public static bool CanAttack(bool firstReady,bool secondReady){return firstReady||secondReady;}
 public const float Speed=20f,Range=1600f,SafeRun=70f,Depth=1.5f;
 public static bool Lead(float x,float z,float vx,float vz,out float time){
  double a=vx*vx+vz*vz-Speed*Speed,b=2*(x*vx+z*vz),c=x*x+z*z;time=0;
  if(c<1)return false;if(Math.Abs(a)<.00001){if(Math.Abs(b)<.00001)return false;time=(float)(-c/b);}else{double d=b*b-4*a*c;if(d<0)return false;double t1=(-b-Math.Sqrt(d))/(2*a),t2=(-b+Math.Sqrt(d))/(2*a);time=(float)(t1>0&&t2>0?Math.Min(t1,t2):Math.Max(t1,t2));}
  return time>0&&time*Speed<=Range;
 }
 public static bool SweepNearHull(float startX,float startZ,float endX,float endZ,float radius)
 {
  float dx=endX-startX,dz=endZ-startZ;
  float lengthSquared=dx*dx+dz*dz;
  float t=lengthSquared>0f?Math.Max(0f,Math.Min(1f,-(startX*dx+startZ*dz)/lengthSquared)):0f;
  float x=startX+t*dx,z=startZ+t*dz;
  return x*x+z*z<=radius*radius;
 }
 public static bool Armed(float travel){return travel>=SafeRun;}
 public static bool Threat(float x,float z,float vx,float vz,float radius,out float time){float v=vx*vx+vz*vz;time=v<.001f?0:-(x*vx+z*vz)/v;return time>0&&time<25&&(x+vx*time)*(x+vx*time)+(z+vz*time)*(z+vz*time)<radius*radius;}
}

using System;
// World-space appearance, independent of vessel scale.
public static class NavalCubeSmokeMath
{
 public const float RiseSpeed=3.6f;
 public const int Capacity=576,ShadeLevels=8;
 public static float Grey(float height,float lifetime)
 {
  float t=Math.Max(0,Math.Min(1,height/Math.Max(1,RiseSpeed*lifetime*.75f)));
  t=t*t*(3-2*t);return .12f+(.60f-.12f)*t;
 }
 public static float Size(float age,float lifetime)
 {
  float progress=Math.Max(0,Math.Min(1,age/Math.Max(.001f,lifetime)));
  float birth=Math.Max(0,Math.Min(1,age/.4f));birth=birth*birth*(3-2*birth);
  float end=Math.Max(0,Math.Min(1,(1-progress)/.25f));end=end*end*(3-2*end);
  return (.15f+.85f*birth)*(1+1.6f*progress)*end;
 }
 public static float Face(int face){return face==2?1f:face<2?.84f:.68f;}
 public static int Shade(float grey){return Math.Max(0,Math.Min(ShadeLevels-1,(int)Math.Round((grey-.12f)/.48f*(ShadeLevels-1))));}
}

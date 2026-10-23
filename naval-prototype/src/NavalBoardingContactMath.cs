using System;
public static class NavalBoardingContactMath
{
 public static bool Allows(bool same,float age,float gap)
 {return same&&age>=0&&!float.IsInfinity(age)&&!float.IsNaN(age)&&(age<=8||age<=90&&gap<=1.5f);}
 public static float Gap(float x,float z,float yaw,float scale,float ox,float oz,float oyaw,float otherScale)
 {
  double a=yaw*Math.PI/180,b=oyaw*Math.PI/180,arx=Math.Cos(a),arz=-Math.Sin(a),afx=Math.Sin(a),afz=Math.Cos(a),brx=Math.Cos(b),brz=-Math.Sin(b),bfx=Math.Sin(b),bfz=Math.Cos(b);
  return (float)Math.Max(0,Math.Max(Math.Max(Axis(ox-x,oz-z,arx,arz,arx,arz,afx,afz,brx,brz,bfx,bfz,scale,otherScale),Axis(ox-x,oz-z,afx,afz,arx,arz,afx,afz,brx,brz,bfx,bfz,scale,otherScale)),Math.Max(Axis(ox-x,oz-z,brx,brz,arx,arz,afx,afz,brx,brz,bfx,bfz,scale,otherScale),Axis(ox-x,oz-z,bfx,bfz,arx,arz,afx,afz,brx,brz,bfx,bfz,scale,otherScale))));
 }
 private static double Axis(double dx,double dz,double x,double z,double arx,double arz,double afx,double afz,double brx,double brz,double bfx,double bfz,float a,float b)
 {return Math.Abs(dx*x+dz*z)-NavalShipContactMath.HalfWidth*.5*(a*Math.Abs(x*arx+z*arz)+b*Math.Abs(x*brx+z*brz))-NavalShipContactMath.HalfLength*.5*(a*Math.Abs(x*afx+z*afz)+b*Math.Abs(x*bfx+z*bfz));}
 public static float EndStand(float scale){return Math.Min(5.7f,6f-.6f/Math.Max(.1f,scale));}
}

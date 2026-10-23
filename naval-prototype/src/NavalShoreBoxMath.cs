using System;
public struct NavalBoxVector
{
 public float X,Y,Z;
 public NavalBoxVector(float x,float y,float z){X=x;Y=y;Z=z;}
 public static NavalBoxVector Sub(NavalBoxVector a,NavalBoxVector b){return new NavalBoxVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);}
 public static float Dot(NavalBoxVector a,NavalBoxVector b){return a.X*b.X+a.Y*b.Y+a.Z*b.Z;}
 public static NavalBoxVector Cross(NavalBoxVector a,NavalBoxVector b){return new NavalBoxVector(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);}
}
public struct NavalShoreBox
{
 public NavalBoxVector Center,Half,Right,Up,Forward;
 public NavalBoxVector Axis(int i){return i==0?Right:i==1?Up:Forward;}
}
public static class NavalShoreBoxMath
{
 public static float Penetration(NavalShoreBox a,NavalShoreBox b)
 {
  var delta=NavalBoxVector.Sub(b.Center,a.Center);
  // Conservative world AABB rejection. Positive overlap still runs every SAT axis.
  var ea=WorldHalf(a);var eb=WorldHalf(b);
  if(Math.Abs(delta.X)>ea.X+eb.X+.0001f||Math.Abs(delta.Y)>ea.Y+eb.Y+.0001f||Math.Abs(delta.Z)>ea.Z+eb.Z+.0001f)return 0;
  float minimum=float.MaxValue;
  for(int i=0;i<3;i++)if(!Axis(a.Axis(i),delta,a,b,ref minimum)||!Axis(b.Axis(i),delta,a,b,ref minimum))return 0;
  for(int i=0;i<3;i++)for(int j=0;j<3;j++)if(!Axis(NavalBoxVector.Cross(a.Axis(i),b.Axis(j)),delta,a,b,ref minimum))return 0;
  return minimum;
 }
 private static NavalBoxVector WorldHalf(NavalShoreBox b){return new NavalBoxVector(
  Math.Abs(b.Right.X)*b.Half.X+Math.Abs(b.Up.X)*b.Half.Y+Math.Abs(b.Forward.X)*b.Half.Z,
  Math.Abs(b.Right.Y)*b.Half.X+Math.Abs(b.Up.Y)*b.Half.Y+Math.Abs(b.Forward.Y)*b.Half.Z,
  Math.Abs(b.Right.Z)*b.Half.X+Math.Abs(b.Up.Z)*b.Half.Y+Math.Abs(b.Forward.Z)*b.Half.Z);}
 private static bool Axis(NavalBoxVector axis,NavalBoxVector delta,NavalShoreBox a,NavalShoreBox b,ref float minimum)
 {
  float length=(float)Math.Sqrt(NavalBoxVector.Dot(axis,axis));if(length<.00001f)return true;
  float radius=Radius(axis,a)+Radius(axis,b),depth=(radius-Math.Abs(NavalBoxVector.Dot(axis,delta)))/length;
  if(depth<=0)return false;minimum=Math.Min(minimum,depth);return true;
 }
 private static float Radius(NavalBoxVector axis,NavalShoreBox box)
 {return Math.Abs(NavalBoxVector.Dot(axis,box.Right))*box.Half.X+Math.Abs(NavalBoxVector.Dot(axis,box.Up))*box.Half.Y+Math.Abs(NavalBoxVector.Dot(axis,box.Forward))*box.Half.Z;}
}

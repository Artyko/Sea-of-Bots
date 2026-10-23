using System;
public static class ReferenceShoreSat
{
 public static float Penetration(NavalShoreBox a,NavalShoreBox b)
 {
  var delta=NavalBoxVector.Sub(b.Center,a.Center);
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

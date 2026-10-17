using System;
using System.Diagnostics;
class SailShapeChecks
{
 static NavalSailPose Direct(float x,float y,float t){
  float u=Math.Max(0f,Math.Min(1f,(x+2.625f)/5.25f)),v=Math.Max(0f,Math.Min(1f,(y-3.825f)/3.75f));
  float e=(float)(Math.Sin(u*Math.PI)*Math.Sin(v*Math.PI)),g=.36f+(float)Math.Sin(t*.85f)*.12f;
  return new NavalSailPose{X=e*(float)Math.Sin(t*1.3f+v*4f)*.04f,Z=.02f+e*(g+(float)Math.Sin(t*2.2f+u*5f+v*3f)*.10f),
   SlopeX=(float)(Math.Cos(u*Math.PI)*Math.Sin(v*Math.PI))*g*(float)Math.PI/5.25f,SlopeY=(float)(Math.Sin(u*Math.PI)*Math.Cos(v*Math.PI))*g*(float)Math.PI/3.75f};
 }
 static float Error(NavalSailPose a,NavalSailPose b){return Math.Max(Math.Abs(a.X-b.X),Math.Max(Math.Abs(a.Z-b.Z),Math.Max(Math.Abs(a.SlopeX-b.SlopeX),Math.Abs(a.SlopeY-b.SlopeY))));}
 static void Main(){
  var x=new float[352];var y=new float[352];for(int i=0;i<352;i++){x[i]=((i%22+.5f)/22f-.5f)*5.5f;y[i]=3.7f+(i/22+.5f)/16f*4f;}
  var cache=new NavalSailShape(x,y);float error=0f;foreach(float t in new[]{0f,.2f,5f,20f,100f,1000f}){cache.Prepare(t);for(int i=0;i<x.Length;i++)error=Math.Max(error,Error(cache.At(i),Direct(x[i],y[i],t)));}
  if(error>.0001f)throw new Exception("Cached sail pose drift "+error);
  float sum=0f;var watch=Stopwatch.StartNew();for(int f=0;f<1500;f++)for(int i=0;i<x.Length;i++)sum+=Direct(x[i],y[i],f*.016f).Z;watch.Stop();double before=watch.Elapsed.TotalMilliseconds;
  watch.Restart();for(int f=0;f<1500;f++){cache.Prepare(f*.016f);for(int i=0;i<x.Length;i++)sum+=cache.At(i).Z;}watch.Stop();
  Console.WriteLine("PASS: sail pose max error="+error+"; analytic "+before.ToString("F1")+" ms, cached "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms. Pure math only, not FPS. "+sum);
 }
}

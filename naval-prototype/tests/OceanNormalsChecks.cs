using System;
using System.Diagnostics;
class OceanNormalsChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main()
 {
  const int count=101;const float spacing=3;var field=new NavalOceanNormals(count);var heights=new float[count*count];
  // A sloped plane has a known normal, including at the one-sided edge samples.
  float length=(float)Math.Sqrt(.2*.2+1+.35*.35);
  for(int z=0;z<count;z++)for(int x=0;x<count;x++)heights[z*count+x]=x*spacing*.2f-z*spacing*.35f+12;
  field.Build(heights,spacing);var random=new Random(117);
  for(int i=0;i<20000;i++)
  {
   NavalSurfaceNormal n=field.Sample((float)random.NextDouble()*120-10,(float)random.NextDouble()*120-10);
   Check(Math.Abs(n.X+.2/length)<.00002&&Math.Abs(n.Y-1/length)<.00002&&Math.Abs(n.Z-.35/length)<.00002,"Grid/cut seam normals match analytic plane, including clamped edges");
  }
  for(int z=0;z<count;z++)for(int x=0;x<count;x++)heights[z*count+x]=NavalWaveField.Sample(x*spacing-150,z*spacing-150,.65f,17);
  field.Build(heights,spacing);
  for(int i=0;i<20000;i++)
  {
   NavalSurfaceNormal n=field.Sample((float)random.NextDouble()*100,(float)random.NextDouble()*100);
   Check(n.Y>0&&!float.IsNaN(n.X)&&Math.Abs(n.X*n.X+n.Y*n.Y+n.Z*n.Z-1)<.00001,"Wave/cut seam normals are finite, unit length and upward");
  }
  // Exercise the same fixed-slot topology as the persistent hull buffers.
  int[][] source={new int[]{0,1,2},new int[]{0,1,2},new int[]{0,1,2}};
  var topology=new NavalBatchTopology(new int[]{3,3,3},source);
  for(int i=0;i<2000;i++)
  {topology.SetCell(0,i%2==0?0:-1);topology.SetCell(1,1);topology.SetCell(2,i%3==0?-1:0);topology.Rebuild();Check(topology.Triangles(1)[0]==3,"Damage never shifts other cells' vertex slots");}
  var watch=Stopwatch.StartNew();for(int i=0;i<300;i++)field.Build(heights,spacing);watch.Stop();
  Console.WriteLine("PASS: 40000 planar/wave seam normals, unit length/edge continuity, persistent damaged hull slots; 300 normal builds "+watch.Elapsed.TotalMilliseconds.ToString("F1")+"ms (.NET only, not Unity FPS).");
 }
}

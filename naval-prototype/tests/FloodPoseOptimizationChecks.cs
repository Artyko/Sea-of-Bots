using System;
using System.Diagnostics;
class FloodPoseOptimizationChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Reference(NavalFloodGrid grid,float[] output,float sx,float sz,float floor,float translation)
 {
  for(int i=0;i<output.Length;i++)
  {
   float x=((i%grid.Columns)+.5f)*grid.CellWidth-grid.Width*.5f;
   float z=((i/grid.Columns)+.5f)*grid.CellLength-grid.Length*.5f;
   output[i]=sx*x+floor+sz*z+translation;
  }
 }
 static double Consume;
 static void Main()
 {
  var random=new Random(111);var grid=new NavalFloodGrid(12,24,5.75f,11.75f,3,3);var before=new float[288];var after=new float[288];
  for(int test=0;test<4000;test++)
  {
   float sx=(float)(random.NextDouble()*6-3),sz=(float)(random.NextDouble()*6-3),floor=(float)(random.NextDouble()*12-6),translation=(float)(random.NextDouble()*2000000-1000000);
   Reference(grid,before,sx,sz,floor,translation);grid.FillFloorHeights(after,sx,sz,floor,translation);
   for(int i=0;i<288;i++)Check(before[i]==after[i],"Floor heights preserve float operation order exactly, including far world coordinates");
  }
  for(int i=0;i<288;i++)
  {
   Check(grid.X(i)==((i%12)+.5f)*grid.CellWidth-grid.Width*.5f,"Cached X matches original coordinate");
   Check(grid.Z(i)==((i/12)+.5f)*grid.CellLength-grid.Length*.5f,"Cached Z matches original coordinate");
  }
  Reference(grid,before,.8f,-.3f,-4.4f,2);grid.FillFloorHeights(after,.8f,-.3f,-4.4f,2);
  var a=new NavalFloodGrid(12,24,5.75f,11.75f,3,3);var b=new NavalFloodGrid(12,24,5.75f,11.75f,3,3);
  for(int i=0;i<288;i++){float water=(float)random.NextDouble();a.Add(i,water);b.Add(i,water);}
  a.Spread(.05f,before,.8f);b.Spread(.05f,after,.8f);for(int i=0;i<288;i++)Check(a.Water[i]==b.Water[i],"Flood spreading remains identical");
  for(int i=0;i<1000;i++){Reference(grid,before,.8f,-.3f,-4.4f,2);grid.FillFloorHeights(after,.8f,-.3f,-4.4f,2);}
  var watch=Stopwatch.StartNew();for(int i=0;i<100000;i++){Reference(grid,before,.8f,-.3f,-4.4f,2);Consume+=before[i%288];}watch.Stop();double oldMs=watch.Elapsed.TotalMilliseconds;
  watch.Restart();for(int i=0;i<100000;i++){grid.FillFloorHeights(after,.8f,-.3f,-4.4f,2);Consume+=after[i%288];}watch.Stop();
  Console.WriteLine("PASS: 4000 poses/1152000 exact cell heights, cached coordinates and identical flood propagation. 100000 grids: old managed arithmetic "+oldMs.ToString("F1")+" -> new "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms; excluded Unity TransformPoint boundary cost, not FPS.");
 }
}

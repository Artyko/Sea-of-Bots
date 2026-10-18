using System;
using System.Collections.Generic;
using System.Diagnostics;
class RepairWorkSiteChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static List<NavalWalkPoint> Reference(float x,float z,float preferred,float bottom)
 {
  var points=new List<NavalWalkPoint>();float[] floors={preferred,preferred>.3f?bottom:.825f};float[] shifts={0f,-.35f,.35f,-.7f,.7f,-1.2f,1.2f};
  foreach(float floor in floors)
  {
   if(Math.Abs(z)>BoatDimensions.HalfLength-.3f)foreach(float shift in shifts)points.Add(new NavalWalkPoint(Math.Max(-2.35f,Math.Min(2.35f,x+shift)),floor,(z<0?-1:1)*5.5f));
   foreach(float corridor in new[]{2.35f,2.1f,1.65f,.95f})foreach(float shift in shifts)points.Add(new NavalWalkPoint(x<0?-corridor:corridor,floor,Math.Max(-5.1f,Math.Min(5.1f,z+shift))));
  }
  return points;
 }
 static float ScanOld(int accept)
 {
  float[] floors={-.825f,.825f};float[] shifts={0f,-.35f,.35f,-.7f,.7f,-1.2f,1.2f};int n=0;
  foreach(float floor in floors)foreach(float corridor in new[]{2.35f,2.1f,1.65f,.95f})foreach(float shift in shifts)
  {float x=-corridor,z=Math.Max(-5.1f,Math.Min(5.1f,2+shift));if(n++==accept)return x+floor+z;}
  return 0;
 }
 static List<NavalWalkPoint> Optimized(float x,float z,float preferred,float bottom)
 {
  var points=new List<NavalWalkPoint>();
  for(int deck=0;deck<2;deck++)
  {
   float floor=NavalRepairWorkSites.Floor(preferred,bottom,deck);
   if(NavalRepairWorkSites.EndWall(z))for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
     points.Add(new NavalWalkPoint(Math.Max(-2.35f,Math.Min(2.35f,x+NavalRepairWorkSites.Shift(i))),floor,(z<0?-1:1)*5.5f));
   for(int c=0;c<NavalRepairWorkSites.CorridorCount;c++)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
   {float corridor=NavalRepairWorkSites.Corridor(c),shift=NavalRepairWorkSites.Shift(i);points.Add(new NavalWalkPoint(x<0?-corridor:corridor,floor,Math.Max(-5.1f,Math.Min(5.1f,z+shift))));}
  }
  return points;
 }
 static float ScanNew(int accept)
 {
  int n=0;for(int deck=0;deck<2;deck++)
  {
   float floor=NavalRepairWorkSites.Floor(-.825f,-.825f,deck);
   for(int c=0;c<NavalRepairWorkSites.CorridorCount;c++)for(int i=0;i<NavalRepairWorkSites.ShiftCount;i++)
   {float x=-NavalRepairWorkSites.Corridor(c),z=Math.Max(-5.1f,Math.Min(5.1f,2+NavalRepairWorkSites.Shift(i)));if(n++==accept)return x+floor+z;}
  }
  return 0;
 }
 static double Consume;
 static void Main()
 {
  var random=new Random(112);
  foreach(var ship in new[]{NavalShipDefinition.Sloop,NavalShipDefinition.Brigantine})for(int test=0;test<3000;test++)
  {
   float x=(float)(random.NextDouble()*7-3.5),z=test%5==0?(test%2==0?-6:6):(float)(random.NextDouble()*12-6),preferred=test%2==0?ship.HoldFloor:.825f;
   var before=Reference(x,z,preferred,ship.HoldFloor);var after=Optimized(x,z,preferred,ship.HoldFloor);Check(before.Count==after.Count,"Candidate count unchanged");
   int accepted=random.Next(before.Count),selected=-1;
   for(int i=0;i<before.Count;i++)
   {
    var a=before[i];var b=after[i];Check(a.X==b.X&&a.Y==b.Y&&a.Z==b.Z,"Exact candidate and order parity on both decks and profiles");
    if(i>=accepted&&selected<0)selected=i;
   }
   var choice=after[selected];Check(choice.X==before[accepted].X&&choice.Y==before[accepted].Y&&choice.Z==before[accepted].Z,"Same accepted physical predicate selects same work point");
  }
  // Equivalent early-exit searches; no materialized reference list in this timing.
  for(int i=0;i<1000;i++){ScanOld(30);ScanNew(30);}
  GC.Collect();int gen=GC.CollectionCount(0);var watch=Stopwatch.StartNew();for(int i=0;i<200000;i++)Consume+=ScanOld(30);watch.Stop();double oldMs=watch.Elapsed.TotalMilliseconds;int oldGC=GC.CollectionCount(0)-gen;
  GC.Collect();gen=GC.CollectionCount(0);watch.Restart();for(int i=0;i<200000;i++)Consume+=ScanNew(30);watch.Stop();int newGC=GC.CollectionCount(0)-gen;
  Check(ScanOld(30)==ScanNew(30),"Benchmarks choose identical point");
  Console.WriteLine("PASS: 6000 complete ordered repair searches on both ships; physical probes remain live. 200000 equivalent candidate scans "+oldMs.ToString("F1")+" -> "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms; Gen0 "+oldGC+" -> "+newGC+" (not Unity FPS).");
 }
}

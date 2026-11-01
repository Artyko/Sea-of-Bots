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
  Check(Math.Abs(NavalRepairWorkSites.EndZ(NavalShipDefinition.Ironclad,6f)-4.85f)<.001f,"Steam bow repair post leaves capsule shoulder clearance in the tapered hull");
  Check(Math.Abs(NavalRepairWorkSites.EndZ(NavalShipDefinition.Brigantine,-6f)+5.5f)<.001f,"Sail end-wall repair geometry is unchanged");
  var ironclad=NavalShipDefinition.Ironclad;
  float serviceX,serviceY,serviceZ;
  int sternSites=0;for(int candidate=0;candidate<NavalRepairWorkSites.SteamSternBeltCandidateCount;candidate++){
   Check(NavalRepairWorkSites.SteamSternBelt(ironclad,1.67f,1.11f,-5.63f,candidate,out serviceX,out serviceY,out serviceZ),"Every logged stern-belt fallback is defined");
   Check(NavalCrewDeckSafety.Inside(serviceX,serviceZ,ironclad,.85f),"Stern-belt service point retains a full sailor capsule inside the taper");
   float dx=(1.01f-serviceX)*ironclad.Scale,dy=(1.11f-serviceY)*ironclad.Scale-NavalRepairAim.HandHeight,dz=(-5.63f-serviceZ)*ironclad.Scale;
   if(NavalRepairSite.WithinReach(dx,dy,dz))sternSites++;
  }
  Check(sternSites==NavalRepairWorkSites.SteamSternBeltCandidateCount,"All dedicated stern points reach the inner face of the logged seam");
  Check(NavalRepairWorkSites.SteamBowBelt(ironclad,-1.90f,1.11f,4.13f,out serviceX,out serviceY,out serviceZ),"Logged forward port-belt hole has a dedicated service pocket");
  Check(Math.Abs(serviceX+1.38f)<.001f&&Math.Abs(serviceY-.825f)<.001f&&Math.Abs(serviceZ-4.05f)<.001f,"Primary forward-belt service pocket is close to the plate and clear of the magazine store");
  float serviceDx=(-1.775f-serviceX)*ironclad.Scale,serviceDy=(1.11f-serviceY)*ironclad.Scale-NavalRepairAim.HandHeight,serviceDz=(4.13f-serviceZ)*ironclad.Scale;
  Check(NavalRepairSite.WithinReach(serviceDx,serviceDy,serviceDz),"Dedicated service pocket reaches the logged port armour hole");
  int reachableBowSites=0;for(int candidate=0;candidate<NavalRepairWorkSites.SteamBowBeltCandidateCount;candidate++){
   Check(NavalRepairWorkSites.SteamBowBelt(ironclad,-1.38f,1.42f,4.63f,candidate,out serviceX,out serviceY,out serviceZ),"Every damaged-bow fallback is defined");
   float dx=(-1.38f-serviceX)*ironclad.Scale,dy=(1.42f-serviceY)*ironclad.Scale-NavalRepairAim.HandHeight,dz=(4.63f-serviceZ)*ironclad.Scale;
   if(NavalRepairSite.WithinReach(dx,dy,dz))reachableBowSites++;
  }
  Check(reachableBowSites>=6,"Logged upper port-bow hole retains several independent in-reach work sites after deck damage");
  bool aftReserve=false,weatherReserve=false;for(int candidate=5;candidate<NavalRepairWorkSites.SteamBowBeltCandidateCount;candidate++){
   NavalRepairWorkSites.SteamBowBelt(ironclad,-1.90f,1.11f,4.13f,candidate,out serviceX,out serviceY,out serviceZ);
   if(serviceZ<3.6f)aftReserve=true;if(Math.Abs(serviceY-ironclad.WeatherDeck)<.01f)weatherReserve=true;
  }
  Check(aftReserve&&weatherReserve,"Forward belt has independent aft and weather-deck emergency approaches");
  for(int gun=0;gun<ironclad.GunCount;gun+=2){float z=NavalShipDefinition.GunZ(ironclad,gun);
   Check(!NavalRepairWorkSites.ClearOfCasemate(ironclad,ironclad.WeatherDeck,z),"Occupied steam gun post is not a repair stand");
   Check(!NavalRepairWorkSites.ClearOfCasemate(ironclad,ironclad.WeatherDeck,z+.55f),"Casemate shield bay remains excluded");
   Check(NavalRepairWorkSites.ClearOfCasemate(ironclad,ironclad.WeatherDeck,z+(z>0?-.7f:.7f)),"Fore/aft casemate edge remains available for repairs");
   float holeX=2.05f,standX=NavalRepairWorkSites.Corridor(ironclad,1),standZ=z+(z>0?-.7f:.7f);
   float dx=(holeX-standX)*ironclad.Scale,dy=(2.24f-ironclad.WeatherDeck)*ironclad.Scale-NavalRepairAim.HandHeight,dz=(z-standZ)*ironclad.Scale;
   Check(NavalRepairSite.WithinReach(dx,dy,dz),"Casemate-edge stand reaches the logged upper-belt hole");
  }
  Console.WriteLine("PASS: 6000 complete ordered repair searches on both ships; physical probes remain live. 200000 equivalent candidate scans "+oldMs.ToString("F1")+" -> "+watch.Elapsed.TotalMilliseconds.ToString("F1")+" ms; Gen0 "+oldGC+" -> "+newGC+" (not Unity FPS).");
 }
}

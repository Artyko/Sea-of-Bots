using System;
class SkeletonCrewChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Helm(NavalSkeletonCrewPolicy policy,float time,int gun=1)
 {for(int i=0;i<(int)Math.Ceiling(time/.05f);i++)policy.Step(.05f,true,true,gun,true,true,false);}
 static void Main()
 {
  var p=new NavalSkeletonCrewPolicy();
  Helm(p,5);Check(p.GunStation==0,"Steer before leaving for a shot");
  for(int i=0;i<200;i++)p.Step(.05f,true,false,1,true,true,false);
  Check(p.GunStation==0,"Walking to helm is not time spent steering");
  Helm(p,1.2f);Check(p.GunStation==1,"One free combat hand visits loaded, aligned gun");
  p.Step(.05f,true,false,1,true,true,false);Check(p.GunStation==1,"Keeps same cannon assignment while walking/aiming");
  p.Step(.05f,true,false,1,false,true,false);Check(p.GunStation==0,"Shot/reload immediately sends helmsman back");
  Helm(p,3);Check(p.GunStation==0,"Steering interval prevents role oscillation");
  Helm(p,3.2f);Check(p.GunStation==1,"Another loaded cannon can be visited after steering");
  p.Step(.05f,true,false,1,true,false,false);Check(p.GunStation==0,"Lost bearing recalls helm");
  Helm(p,6.2f);p.Step(.05f,true,false,1,true,true,true);Check(p.GunStation==0,"Critical flood/urgent trim/retreat cancels relief fire");
  for(int i=0;i<300;i++)p.Step(.05f,true,true,1,true,true,true);
  Check(p.GunStation==0,"Cannot abandon emergency helm for guns");
  Helm(p,6.2f);p.Step(.05f,false,false,1,true,true,false);Check(p.GunStation==0,"Player takeover, second free crewman, death or target loss restores ordinary staffing");
  Helm(p,6.2f);for(int i=0;i<410;i++)p.Step(.05f,true,false,1,true,true,false);
  Check(p.GunStation==0,"Unreachable/blocked cannon visit cannot last forever");
  for(int i=0;i<300;i++)p.Step(.05f,true,true,2,false,true,false);
  Check(p.GunStation==0,"No visit to reloading gun");
  Check(NavalSkeletonCrewPolicy.SoloMaintenance(1,true),"Last survivor releases helm/guns for actionable maintenance");
  Check(!NavalSkeletonCrewPolicy.SoloMaintenance(2,true),"A repairer plus helm may still fight");
  Check(!NavalSkeletonCrewPolicy.SoloMaintenance(1,false),"Healthy last survivor may steer and fire");
  Check(NavalCrewPriorities.CanBoard(true,true,true,true),"Disarmed survivors preserve final ram/boarding attack");
  Console.WriteLine("PASS: steering/shot/return cycles, loaded gun and bearing checks, player/crew/target interruption, danger, route timeout, lone-survivor repair and disarmed final attack.");
 }
}

using System;
class HoldSafetyChecks
{
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static void Main()
 {
  Check(!NavalHoldSafety.MustEscape(false,false,1,20),"Waist water or missed foot support cannot force evacuation with dry head");
  Check(!NavalHoldSafety.MustEscape(true,false,30,6),"Fresh air permits submerged damage control");
  Check(!NavalHoldSafety.MustEscape(true,false,19,6),"Work continues beyond old ten-second cutoff");
  Check(NavalHoldSafety.MustEscape(true,false,10,6),"Reserve actual exit time plus margin");
  Check(NavalHoldSafety.MustEscape(false,true,30,1),"Irreversibly sinking ship evacuates even with air");
  Check(NavalHoldSafety.AirReserve(100)==22&&NavalHoldSafety.AirReserve(0)==8,"Reserve bounded without consuming full breath immediately");
  float near=NavalHoldSafety.ExitSeconds(1.65f,-1,-3.65f,-1,2,5.5f,false);
  float far=NavalHoldSafety.ExitSeconds(2f,-1,5,-1,3,5.5f,false);
  Check(far>near,"Far or larger hold gets earlier evacuation");
  Check(NavalHoldSafety.ExitSeconds(0,0,.2f,-1,3,5.5f,true)<far,"Partial stairs need less reserve than distant hold");
  Check(!NavalHoldSafety.CanResume(true,false,30,2,true),"Never cancel escape while head remains submerged");
  Check(!NavalHoldSafety.CanResume(false,false,10,2,true),"Must actually regain breath");
  Check(!NavalHoldSafety.CanResume(false,false,30,.3f,true),"Brief wave trough cannot flip evacuation state");
  Check(!NavalHoldSafety.CanResume(false,false,30,2,false),"Do not abort halfway up stairs");
  Check(!NavalHoldSafety.CanResume(false,true,30,2,true),"Sinking vessel cannot recall crew into work");
  Check(NavalHoldSafety.CanResume(false,false,30,1,true),"Receded water resumes damage control without forced deck round trip");
  NavalBreathClock breath=new NavalBreathClock();float workSeconds=0;
  while(!NavalHoldSafety.MustEscape(true,false,breath.Air,5))
  {Check(!breath.Step(.2f,.1f),"Not drowning during work window");workSeconds+=.1f;}
  Check(workSeconds>=20&&breath.Air>=8.8f,"Useful repair window and escape air both preserved");
  for(int i=0;i<50;i++)Check(!breath.Step(.2f,.1f),"Predicted escape can finish alive");
  breath.Step(-.2f,.1f);Check(breath.Air==30,"Surfacing instantly restores actual air");
  Console.WriteLine("PASS: no dry-head panic, actual air budget, scaled exit reserve, safe resumption and work-to-surface breath lifecycle.");
 }
}

using System;
class EngagementRangeChecks
{
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
 static void Main()
 {
  Check(NavalEngagementRange.BallisticReach(80,0),"Ordinary firing elevation is degrees, not radians");
  Check(NavalEngagementRange.BallisticReach(103,0),"Near-max range remains attainable");
  Check(!NavalEngagementRange.BallisticReach(110,0),"Beyond ballistic range correctly rejected");
  NavalEngagementRange state=new NavalEngagementRange();state.Reset(0);
  for(int i=0;i<29;i++)state.Step(.1f,120,false,3,3);
  Check(!state.Pursuing,"Brief failed solution does not twitch course");
  for(int i=0;i<3;i++)state.Step(.1f,120,false,3,3);
  Check(state.Pursuing,"Persistent out-of-range orbit triggers closing");
  state.Step(.1f,95,true,3,3);Check(!state.Pursuing&&state.KeepBroadside,"First possible solution immediately ends nose-on pursuit");
  state.Step(.1f,state.Desired+7,true,3,3);Check(!state.Pursuing,"Return to broadside at comfortable range");
  float min=999,max=0;
  for(int i=0;i<4800;i++)
  {
   state.Step(.1f,75,true,3,3);min=Math.Min(min,state.Desired);max=Math.Max(max,state.Desired);
   Check(state.Desired>NavalHelmTactics.Clearance(3,3)+5.9f,"Keep collision margin");
   Check(state.Desired<NavalGunneryMath.LevelRange*.9f,"All varied ranges have ballistic margin");
  }
  Check(max-min>20,"Meaningful close/medium/far distance variation");
  for(int side=1;side<=2;side++)foreach(float distance in new[]{55f,65f,80f,110f})
  {
   float course=NavalEngagementRange.Course(0,side,distance,75,false);
   Check(Math.Abs(NavalBroadsideMath.Delta(NavalBroadsideMath.Heading(0,side),course))<20,"Normal range corrections preserve traverse");
   float closure=(float)Math.Cos(course*Math.PI/180);
   Check(distance>75?closure>0:closure<0,"Feedback closes or opens distance as needed");
  }
  for(int side=1;side<=2;side++)foreach(float enemySpeed in new[]{0f,2.5f})
  {
   state.Reset(side);double x=0,z=-130,enemyZ=0;float heading=side==1?90:-90,rudder=0;bool entered=false;
   for(int i=0;i<8000;i++)
   {
    double dz=enemyZ-z;float distance=(float)Math.Sqrt(x*x+dz*dz);float bearing=(float)(Math.Atan2(-x,dz)*180/Math.PI);
    bool reachable=NavalEngagementRange.BallisticReach(distance,0);
    state.Step(.05f,distance,reachable,3,3);
    if(distance<95){entered=true;break;}
    float wanted=NavalEngagementRange.Course(bearing,side,distance,state.Desired,state.Pursuing);
    float turn=Math.Max(-1,Math.Min(1,NavalBroadsideMath.Delta(heading,wanted)/25));
    rudder=NavalRigMath.StepRudder(rudder,turn,.05f);heading+=NavalRigMath.RudderRate(3,rudder)*.05f;
    x+=Math.Sin(heading*Math.PI/180)*3*.05;z+=Math.Cos(heading*Math.PI/180)*3*.05;enemyZ+=enemySpeed*.05;
   }
   Check(entered,"Tangential ship outside range closes into reliable firing range");
  }
  Console.WriteLine("PASS: ballistic units/range, delayed pursuit, hysteresis, varied safe ranges, signed distance feedback and four outside-range recoveries against stationary and retreating targets.");
 }
}

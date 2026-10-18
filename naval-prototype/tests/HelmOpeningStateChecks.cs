using System;
class HelmOpeningStateChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main()
 {
  NavalEngagementRange state=new NavalEngagementRange();state.Reset(4);
  for(int i=0;i<40;i++)state.Step(.1f,130,false,3,3);
  Check(state.Pursuing,"Initially outside range may seek closure");
  state.Step(.1f,110,true,3,3);
  Check(!state.Pursuing&&state.KeepBroadside,"Do not wait for a close preferred distance to expose guns");
  for(int i=0;i<20;i++)state.Step(.1f,112,false,3,3);
  Check(state.KeepBroadside,"Waves/brief range failures cannot flip back to bow attack");
  for(int i=0;i<15;i++)state.Step(.1f,130,false,3,3);
  Check(!state.KeepBroadside,"Sustained loss of reach permits renewed approach");
  for(int side=1;side<=2;side++)
  {
   float previous=0;
   foreach(float distance in new[]{250f,190f,160f,140f,120f,110f})
   {
    float angle=Math.Abs(NavalBroadsideMath.Delta(0,NavalEngagementRange.ApproachCourse(0,side,distance,3,-3,0)));
    Check(angle>=previous&&angle<=65,"Head-on convergence turns progressively, never early full broadside then bow");previous=angle;
   }
   Check(previous>=60,"Working guns exposed near range entry");
   float course=NavalEngagementRange.Course(0,side,110,75,false);
   Check(Math.Abs(NavalBroadsideMath.Delta(NavalBroadsideMath.Heading(0,side),course))<20,"Reachable enemy retains broadside at long range");
  }
  NavalEngagementRange a=new NavalEngagementRange(),b=new NavalEngagementRange(),c=new NavalEngagementRange();a.Reset(1);b.Reset(1);c.Reset(2);
  bool diverse=false;float bias=a.CourseBias;
  for(int i=0;i<100;i++)
  {a.Step(.1f,80,true,3,3);b.Step(.1f,80,true,3,3);c.Step(.1f,80,true,3,3);Check(a.Desired==b.Desired,"Seeded scenarios reproducible");if(a.Desired!=c.Desired||a.CourseBias!=c.CourseBias)diverse=true;Check(a.CourseBias==bias,"No per-frame random steering");}
  Check(diverse,"Different ships have different stable manoeuvres");
  bool[] options={true,true,true,true};for(int i=0;i<1000;i++)Check(NavalGunneryMath.Choose(i/1000f,-1,options)!=2,"No ordinary mast targeting");
  Check(NavalGunneryMath.Choose(.5f,-1,new bool[]{false,false,true,false})==2,"Rig-only fallback remains available");
  Console.WriteLine("PASS: pursuit-to-fire transition, wave hysteresis, continuous approach, stable random manoeuvres and rig-only targeting policy.");
 }
}

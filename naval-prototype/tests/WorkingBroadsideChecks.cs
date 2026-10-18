using System;
class WorkingBroadsideChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main()
 {
  Check(NavalBroadsideMath.HelmSide(0,-90,1,2,2,1)==1,"Visible one-gun port side overrides hidden two-gun side");
  Check(NavalBroadsideMath.HelmSide(0,90,2,1,1,2)==2,"Symmetric visible starboard priority");
  Check(NavalBroadsideMath.HelmSide(0,0,0,1,1,0)==2,"Destroyed side forces surviving side");
  Check(NavalBroadsideMath.HelmSide(0,179,1,1,0,0)==2,"Choose shorter starboard orientation");
  Check(NavalBroadsideMath.HelmSide(0,-179,1,1,0,0)==1,"Choose shorter port orientation");
  for(int side=1;side<=2;side++)for(int heading=0;heading<360;heading+=15)
  for(int bearing=0;bearing<360;bearing+=15)for(int waypoint=0;waypoint<360;waypoint+=45)
  {
   float ideal=NavalBroadsideMath.Heading(bearing,side);
   float desired=NavalBroadsideMath.FiringCourse(heading,bearing,side,waypoint,80,60);
   Check(Math.Abs(NavalBroadsideMath.Delta(ideal,desired))<=12.001,"Firing course never loses gun sector to waypoint");
   if(Math.Abs(NavalBroadsideMath.Delta(heading,ideal))>20)
    Check(Math.Abs(NavalBroadsideMath.Delta(ideal,desired))<.001,"Complete orientation before waypoint trim");
  }
  bool left=false,right=false;
  for(int side=1;side<=2;side++)for(int initial=0;initial<360;initial+=15)
  {
   double x=0,z=-80;float heading=initial,rudder=0;bool acquired=false;
   for(int step=0;step<2400;step++)
   {
    float bearing=(float)(Math.Atan2(-x,-z)*180/Math.PI);
    float ideal=NavalBroadsideMath.Heading(bearing,side);
    float error=NavalBroadsideMath.Delta(heading,ideal);
    if(Math.Abs(error)<20){acquired=true;break;}
    if(error<0)left=true;else right=true;
    float turn=Math.Max(-1f,Math.Min(1f,error/25));
    rudder=NavalRigMath.StepRudder(rudder,turn,.05f);
    heading+=NavalRigMath.RudderRate(3,rudder)*.05f;
    x+=Math.Sin(heading*Math.PI/180)*3*.05;
    z+=Math.Cos(heading*Math.PI/180)*3*.05;
   }
   Check(acquired,"Surviving broadside acquires target from every starting heading with real rudder/headway");
  }
  Check(left&&right,"Both turn directions used");
  Console.WriteLine("PASS: visible gun priority, destroyed-side recovery, 9216 course/waypoint cases and 48 moving-hull turn acquisitions in both directions.");
 }
}

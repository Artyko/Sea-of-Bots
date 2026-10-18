using System;
class OpeningBroadsideChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main()
 {
  foreach(int side in new[]{1,2})
  {
   float entry=NavalBroadsideMath.OpeningHeading(0,side);
   Check(Math.Abs(NavalBroadsideMath.Delta(NavalBroadsideMath.Heading(0,side),entry))==25,"Approach already lies inside 30-degree gun traverse");
   Check(Math.Cos(entry*Math.PI/180)>.4,"Approach retains closure rather than orbiting out of range");
   float slow=NavalBroadsideMath.OpeningDistance(0,0,side,3,2,3,3);
   float fast=NavalBroadsideMath.OpeningDistance(0,0,side,3,7,3,3);
   Check(fast>slow,"Faster closure starts turn earlier");
   Check(NavalBroadsideMath.OpeningDistance(0,0,side,1,7,3,3)>fast,"Slow rudder gets longer lead");
   Check(NavalBroadsideMath.FiringCourse(0,0,side,0,180,60,true)==entry,"Early approach overrides waypoint chase");
   Check(NavalBroadsideMath.FiringCourse(0,0,side,0,300,60,false)==0,"Distant approach preserved");
   foreach(float enemySpeed in new[]{0f,3f})foreach(float startHeading in new[]{-30f,0f,30f})
   {
    double x=0,z=-250,enemyZ=0;float heading=startHeading,rudder=0;bool prepared=false,reached=false;
    float range=NavalGunneryMath.LevelRange+2.25f*3+6.45f*3;
    for(int step=0;step<5000;step++)
    {
     double dx=-x,dz=enemyZ-z;float distance=(float)Math.Sqrt(dx*dx+dz*dz);
     float bearing=(float)(Math.Atan2(dx,dz)*180/Math.PI);
     if(distance<=range)
     {
      reached=true;
      Check(Math.Abs(NavalBroadsideMath.Delta(heading,NavalBroadsideMath.Heading(bearing,side)))<=30,"Gun sector acquired by first range entry");break;
     }
     float closing=(float)((Math.Sin(heading*Math.PI/180)*3*dx+(Math.Cos(heading*Math.PI/180)*3+enemySpeed)*dz)/distance);
     float early=NavalBroadsideMath.OpeningDistance(heading,bearing,side,3,closing,3,3);
     if(distance<=early)prepared=true;else if(distance>early+45)prepared=false;
     float desired=NavalBroadsideMath.FiringCourse(heading,bearing,side,bearing,distance,51,prepared);
     float error=NavalBroadsideMath.Delta(heading,desired);
     rudder=NavalRigMath.StepRudder(rudder,Math.Max(-1,Math.Min(1,error/25)),.05f);
     heading+=NavalRigMath.RudderRate(3,rudder)*.05f;
     x+=Math.Sin(heading*Math.PI/180)*3*.05;z+=Math.Cos(heading*Math.PI/180)*3*.05;
     enemyZ-=enemySpeed*.05;
    }
    Check(reached,"Early turn continues closing into firing range");
   }
  }
  Console.WriteLine("PASS: predictive turn lead, traverse margin, continued closure and 12 first-range-entry approach simulations.");
 }
}

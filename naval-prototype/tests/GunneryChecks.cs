using System;
class GunneryChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        Check(NavalTargetVisibility.Broadside(100f,0f)&&NavalTargetVisibility.Broadside(-100f,0f),"Both broadside views permit visible mechanisms");
        Check(!NavalTargetVisibility.Broadside(0f,-100f),"Stern view excludes cannons and wheel");
        Check(!NavalTargetVisibility.Broadside(0f,100f),"Bow view excludes mechanisms");
        Check(!NavalTargetVisibility.Broadside(20f,-100f),"Oblique stern view remains excluded");
        Check(NavalTargetVisibility.Broadside(100f,20f),"Oblique broadside view permitted");
        bool[] masked={true,false,true,false};
        for(int i=0;i<100;i++){int kind=NavalGunneryMath.Choose(i/100f,-1,masked);Check(kind==0||kind==2,"Hidden mechanisms are never selected");}
        bool[] all={true,true,true,true};int[] counts=new int[4];
        for(int n=0;n<1000;n++)
        {
            float roll=n/1000f;int choice=NavalGunneryMath.Choose(roll,-1,all);
            Check(choice>=0&&choice<4,"Valid target");counts[choice]++;
            for(int previous=0;previous<4;previous++)Check(NavalGunneryMath.Choose(roll,previous,all)!=previous,"Alternate target types");
            Check(NavalGunneryMath.Choose(roll,2,new bool[]{false,false,true,false})==2,"Only remaining mast stays targetable");
        }
        foreach(int count in counts)Check(count>0,"Every feature can be chosen");
        Check(counts[0]>counts[1]&&counts[1]>counts[2]&&counts[2]>counts[3],"Weighted target preference");
        Check(NavalGunneryMath.Choose(.5f,-1,new bool[4])==-1,"No shot without available target");
        Check(NavalGunneryMath.Spread(100f)>NavalGunneryMath.Spread(20f),"Distant shots have more spread");
        Check(NavalGunneryMath.Spread(1000f)<=6f,"Spread remains bounded");
        foreach(float height in new float[]{-2f,0f,4f,10f})
        {
            float angle;Check(NavalBattleMath.FiringAngle(60f,height,32f,out angle),"Reachable ballistic solution");
            float time=NavalGunneryMath.FlightTime(60f,angle);
            double actual=32f*Math.Sin(angle*Math.PI/180f)*time-4.905*time*time;
            Check(Math.Abs(actual-height)<.001f,"Arc reaches target height");
            Check(time>=60f/32f,"Flight time accounts for launch elevation");
        }
        Check(NavalGunneryMath.Spread(100f)>4.5f,"Stronger long range scatter");
        float weight=NavalGunneryMath.ApproachWeight(NavalGunneryMath.LevelRange);
        double bearing=Math.Atan2(1f,weight)*180/Math.PI;
        Check(bearing>=60f&&bearing<=120f,"Maximum range approach exposes broadside gun arc");
        Check(NavalGunneryMath.ApproachWeight(200f)==1f,"Out of range ship still closes distance");
        float farAngle;
        Check(NavalBattleMath.FiringAngle(NavalGunneryMath.LevelRange-.1f,0f,32f,out farAngle),"Shot at physical range limit exists");
        Check(!NavalBattleMath.FiringAngle(NavalGunneryMath.LevelRange+10f,0f,32f,out farAngle),"No artificial range increase");
        Check(NavalGunneryMath.ReloadSeconds==5f,"Shared reload interval");
        Console.WriteLine("PASS: target variety and weights, destroyed feature exclusion, bounded spread, ballistic flight time and shared reload.");
    }
}

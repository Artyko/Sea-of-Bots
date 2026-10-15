using System;
class BattleMathChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static void Main()
    {
        for(int range=20;range<=80;range+=10)
        {
            float angle;Check(NavalBattleMath.FiringAngle(range,-2f,32f,out angle),"Broadside range reachable");
            double radians=angle*Math.PI/180, time=range/(32*Math.Cos(radians));
            double height=32*Math.Sin(radians)*time-9.81*time*time/2;
            Check(Math.Abs(height+2)<0.0001,"Computed shot reaches waterline target");
            Check(angle>=-5&&angle<=45,"Aim stays inside cannon elevation range");
        }
        float ignored;Check(!NavalBattleMath.FiringAngle(200f,0f,32f,out ignored),"Unreachable target rejected");
        Check(!NavalBattleMath.FiringAngle(0f,0f,32f,out ignored),"Zero range rejected");
        Check(NavalBattleMath.CanDamageCrew(false,false,false),"Player/allied cannon can damage enemy sailors");
        Check(NavalBattleMath.CanDamageCrew(true,true,false),"Enemy cannon can damage allied sailors and player");
        Check(!NavalBattleMath.CanDamageCrew(false,true,false),"Allied sailors protected from allied cannon");
        Check(!NavalBattleMath.CanDamageCrew(true,false,false),"Enemy sailors protected from enemy cannon");
        Console.WriteLine("PASS: broadside firing solutions, trajectory height, elevation limits and unreachable targets.");
    }
}

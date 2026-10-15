using System;
class CrewMovementChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        Check(NavalCrewMovement.TargetSpeed(6f,1f,10f,false,false)==6f,"Normal deck travel uses native robot speed");
        Check(NavalCrewMovement.TargetSpeed(6f,1f,0.05f,false,false)==6f,"Intermediate waypoint does not brake");
        Check(NavalCrewMovement.TargetSpeed(6f,1f,0.05f,true,false)<2f,"Final approach brakes for accurate arrival");
        Check(NavalCrewMovement.TargetSpeed(6f,0f,10f,false,false)>4f,"Turning avoids old quarter-speed penalty");
        Check(Math.Abs(NavalCrewMovement.TargetSpeed(6f,1f,10f,false,true)-4.8f)<0.00001f,"Stairs retain moderate control margin");
        float speed=0f;for(int i=0;i<6;i++)speed=NavalCrewMovement.Accelerate(speed,6f,0.05f);
        Check(speed==6f,"Native speed reached after 0.3 seconds");
        Check(NavalCrewMovement.Accelerate(0f,6f,0f)==0f,"Pause does not accelerate");
        Check(NavalCrewMovement.BaseSpeed(float.NaN)==5.5f,"Invalid native speed has safe fallback");
        Check(NavalCrewMovement.TargetSpeed(8f,1f,10f,false,false)==8f,"Faster robot is not capped at 3.2 m/s");
        Console.WriteLine("PASS: native walking speed, waypoint flow, rapid acceleration, final braking and stair control.");
    }
}

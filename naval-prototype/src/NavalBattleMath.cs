using System;
public static class NavalBattleMath
{
    public static bool CanDamageCrew(bool hostile,bool playerTeam,bool neutral)
    { return hostile?playerTeam:!playerTeam&&!neutral; }
    public static bool FiringAngle(float distance,float height,float speed,out float degrees)
    {
        degrees=0f;if(distance<0.1f)return false;
        double v2=speed*speed, discriminant=v2*v2-9.81*(9.81*distance*distance+2*height*v2);
        if(discriminant<0)return false;
        degrees=(float)(Math.Atan((v2-Math.Sqrt(discriminant))/(9.81*distance))*180/Math.PI);
        return true;
    }
}

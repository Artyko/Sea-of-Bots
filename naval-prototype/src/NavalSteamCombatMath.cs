using System;

public static class NavalCasemateHeelMath
{
    public const float SlowAngle=6f,InhibitAngle=13f;
    public static int LowSide(float roll){return roll>0f?1:roll<0f?2:0;}
    public static bool IsLowSide(int gun,float roll)
    {int side=gun%2==0?1:2;return side==LowSide(roll);}
    public static float ReloadFactor(int gun,float roll)
    {
        float heel=Math.Abs(roll);if(!IsLowSide(gun,roll)||heel<=SlowAngle)return 1f;
        if(heel>=InhibitAngle)return 0f;
        return 1f-(heel-SlowAngle)/(InhibitAngle-SlowAngle)*.55f;
    }
    public static bool CanOperate(int gun,float roll){return ReloadFactor(gun,roll)>0f;}
    public static float AimSpread(float roll)
    {return Math.Max(0f,Math.Abs(roll)-SlowAngle)*.055f;}
}

public static class NavalSteamDamageControlTactics
{
    public const float WaterDifference=4f;
    public static int HealthierSide(int selected,int portGuns,int starboardGuns,float portWater,float starboardWater)
    {
        if(portWater>starboardWater+WaterDifference&&starboardGuns>0)return 2;
        if(starboardWater>portWater+WaterDifference&&portGuns>0)return 1;
        if(selected==1&&portGuns>0||selected==2&&starboardGuns>0)return selected;
        return portGuns>0?1:starboardGuns>0?2:selected;
    }
}

public static class NavalSteamRangeTactics
{
    public static float MinimumDistance(float desired,float collisionClearance)
    {return Math.Max(collisionClearance+22f,desired*.65f);}
    public static float EscapeUrgency(float distance,float desired,float collisionClearance)
    {
        float minimum=MinimumDistance(desired,collisionClearance);
        return Math.Max(0f,Math.Min(1f,(minimum-distance)/Math.Max(1f,minimum-collisionClearance)));
    }
}

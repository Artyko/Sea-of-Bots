using System;
public static class NavalRamMath
{
    public static int DamageCells(float closingSpeed,bool bow,bool stern)
    {
        if(closingSpeed<0.8f)return 0;
        float strength=bow?0.35f:stern?1.2f:1f;
        return Math.Max(1,(int)Math.Ceiling(Math.Min(5f,closingSpeed*1.1f)*strength));
    }
    public static bool Assault(bool portWorks,bool starboardWorks){return !portWorks&&!starboardWorks;}
}

using System;
public class NavalGaitClock
{
    public float Phase;
    public void Advance(float distance,float stride)
    {if(distance>0f&&stride>0.01f){Phase+=distance/stride;Phase-=(float)Math.Floor(Phase);}}
}

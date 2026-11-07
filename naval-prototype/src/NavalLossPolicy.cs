using System;
public static class NavalLossPolicy
{
 public static bool Irrecoverable(float flooded,float heel,float submergedRim,float inflow,float pumpRate){
  return flooded>=.92f&&submergedRim>.35f&&inflow>pumpRate*1.2f||Math.Abs(heel)>=75f&&flooded>=.65f;
 }
 public static float Step(float seconds,float dt,bool terminal){return terminal?seconds+dt:Math.Max(0f,seconds-dt*3f);}
}

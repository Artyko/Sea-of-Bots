using System;
public static class NavalHelmIntentMath
{
 public static int Mode(bool rescue,bool ram,bool repair){return rescue?3:ram?2:repair?1:0;}
 public static float Heading(float current,float wanted,float dt,float rate)
 {float error=NavalBroadsideMath.Delta(current,wanted),step=Math.Max(0,dt)*Math.Max(0,rate);return current+Math.Max(-step,Math.Min(step,error));}
 public static bool ReverseRisk(bool land,bool contact,bool ram,float forwardClear,float speed)
 {return land||forwardClear<Math.Max(6,Math.Abs(speed)*3)||contact&&!ram;}
 public static float TurnBudget(float error,float speed)
 {return Math.Max(12,Math.Min(45,Math.Abs(error)/Math.Max(2,Math.Abs(speed)*2)+8));}
}

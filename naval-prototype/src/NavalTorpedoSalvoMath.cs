using System;
public static class NavalTorpedoSalvoMath {
 public static float MountOffset(int mount){return (mount-1.5f)*1.4f;}
 public const int LongSingle=3, SequentialFan=4;
 public static int AttackStyle(float roll,int rounds,float distance)
 {
  if(distance>900f&&(roll<.35f||rounds==1))return LongSingle;
  if(rounds>=3&&roll>.75f)return SequentialFan;
  return Style(roll,rounds);
 }
 public static int Style(float roll,int rounds){return rounds<2?0:roll<.33f?0:roll<.67f?1:2;}
 public static float LaunchDistance(int style,float roll){float jitter=Math.Max(0,Math.Min(1,roll));return style==LongSingle?1250+150*jitter:style==SequentialFan?360+180*jitter:style==0?280+120*jitter:style==1?350+150*jitter:450+150*jitter;}
 public static float FanHalfAngle(float distance,float targetBeam){return Math.Max(.75f,Math.Min(3.5f,(float)(Math.Atan2(Math.Max(4,targetBeam*.4f)+distance*.008f,Math.Max(100,distance))*180/Math.PI)));}
}

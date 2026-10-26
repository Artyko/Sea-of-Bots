// Cloth collision and render poses move together at a bounded update rate.
public sealed class NavalSailPoseClock
{
 private float next=float.NegativeInfinity,lastSetting=float.NaN;
 private int nextFrame;
 public bool Due(float now,float setting,bool distant)
 {return Due(now,setting,distant,-1,1);}
 public bool Due(float now,float setting,bool distant,int frame,int minimumFrameGap)
 {
  bool changed=System.Math.Abs(setting-lastSetting)>=.01f;
  if(!changed&&(now<next||frame>=0&&frame<nextFrame))return false;
  next=now+(distant?1f/15f:1f/30f);if(frame>=0)nextFrame=frame+System.Math.Max(1,minimumFrameGap);lastSetting=setting;return true;
 }
}

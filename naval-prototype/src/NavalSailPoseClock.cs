// Cloth collision and render poses move together at a bounded update rate.
public sealed class NavalSailPoseClock
{
 private float next=float.NegativeInfinity,lastSetting=float.NaN;
 public bool Due(float now,float setting,bool distant)
 {
  if(now<next&&System.Math.Abs(setting-lastSetting)<.01f)return false;
  next=now+(distant?1f/15f:1f/30f);lastSetting=setting;return true;
 }
}

using UnityEngine;
public static class NavalStandbyWaterPolicy
{
 public static bool Unsafe(float depth){return depth>.60f;}
 public static bool DryAgain(float depth){return depth<.20f;}
 public static int AALimit(bool emergency,int mounts){return emergency?Mathf.Min(2,mounts):mounts;}
}
public partial class NavalRepairSailor
{
 private Vector3 safeWaiting;private bool displacedWaiting;private float waitingReview,returnDryAfter;
 public Vector3 SafeStandby(Vector3 original,int index){
  var world=NavalTestWorld.Active;if(world==null)return original;
  if(Time.time<waitingReview)return displacedWaiting?safeWaiting:original;
  waitingReview=Time.time+1f;
  Vector3 baseWorld=boat.TransformPoint(original);float depth=world.WaterLevelAt(baseWorld)-baseWorld.y;
  if(!displacedWaiting&&!NavalStandbyWaterPolicy.Unsafe(depth))return original;
  if(displacedWaiting&&NavalStandbyWaterPolicy.DryAgain(depth)&&Time.time>=returnDryAfter){displacedWaiting=false;return original;}
  returnDryAfter=NavalStandbyWaterPolicy.DryAgain(depth)?returnDryAfter:Time.time+20f;
  if(displacedWaiting){Vector3 cached=boat.TransformPoint(safeWaiting);if(world.WaterLevelAt(cached)-cached.y<.3f&&Supported(safeWaiting)&&ClearBody(safeWaiting))return safeWaiting;}
  float best=float.PositiveInfinity;Vector3 selected=feet;bool found=false;
  for(int deck=0;deck<hull.Definition.DeckCount;deck++){
   float y=deck==hull.Definition.DeckCount-1?hull.Definition.WeatherDeck:hull.Definition.MaintenanceFloor(deck);
   if(y<original.y-.01f)continue;
   for(int n=0;n<12;n++){
    float z=n/4==0?1.1f:n/4==1?2.4f:-3.8f;
    float x=(n%2==0?-1:1)*(n%4<2?.5f:1.5f);
    Vector3 point=new Vector3(x,y,z),position=boat.TransformPoint(point);
    if(world.WaterLevelAt(position)-position.y>.3f||!Supported(point)||!ClearBody(point))continue;
    float cost=boat.TransformVector(point-feet).sqrMagnitude+(n==index%12?0f:1f);
    if(cost<best){best=cost;selected=point;found=true;}
   }
  }
  if(found){safeWaiting=selected;displacedWaiting=true;return safeWaiting;}
  // Never order an idle sailor down into a flooded station when no dry site remains.
  if(!displacedWaiting){safeWaiting=feet;displacedWaiting=true;}return safeWaiting;
 }
}

using System.Collections.Generic;
using UnityEngine;
public static class NavalRangefinderMath
{
 public static float SeaRange(Vector3 muzzle,Vector3 velocity,float sea){
  float h=Mathf.Max(0,muzzle.y-sea),t=(velocity.y+Mathf.Sqrt(velocity.y*velocity.y+2f*9.81f*h))/9.81f;
  return new Vector2(velocity.x,velocity.z).magnitude*t;
 }
 public static Vector3 Formation(Vector3 position,float heading,Vector3 offset){
  double angle=heading*System.Math.PI/180.0;float sine=(float)System.Math.Sin(angle),cosine=(float)System.Math.Cos(angle);
  float z=offset.z+100f;return position+new Vector3(offset.x*cosine+z*sine,offset.y,z*cosine-offset.x*sine);
 }
}
public partial class NavalTestWorld
{
 internal bool RangefinderCrewReady {get{return allies!=null&&allies.HasHelmsman&&!hull.Sunk;}}
}
public partial class NavalStations
{
 private readonly List<NavalHullVoxel> rangeBridge=new List<NavalHullVoxel>();
 private bool rangeBridgeKnown;private float rangeReview,rangeReading,rangeError;private Transform rangeTarget;
 private bool RangeBridgeReady(){
  if(!rangeBridgeKnown){rangeBridgeKnown=true;
   float z=NavalRaisedBridge.Center(hull.Definition),w=NavalRaisedBridge.HalfWidth(hull.Definition),top=NavalRaisedBridge.Top(hull.Definition);
   foreach(var cell in hull.Voxels)if(cell.transform.parent==boat&&Mathf.Abs(cell.LocalCenter.x)<=w+.1f&&Mathf.Abs(cell.LocalCenter.z-z)<=NavalRaisedBridge.HalfDepth(hull.Definition)+.1f&&cell.LocalCenter.y>=hull.Definition.WeatherDeck&&cell.LocalCenter.y<=top+.6f)rangeBridge.Add(cell);
  }
  int intact=0;foreach(var cell in rangeBridge)if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
  return rangeBridge.Count>0&&intact>=rangeBridge.Count*.65f&&helmMechanism!=null&&helmMechanism.Functional;
 }
 private void DrawRangefinder(){
  var world=NavalTestWorld.Active;
  if(EnemyControlled||world==null||world.Spectating||!hull.Definition.SteamPowered||playerCapitalSlot<0&&(active<=0||active==SailStation))return;
  Transform barrel;Vector3 muzzle;float velocity;
  if(playerCapitalSlot>=0){barrel=capitalTurrets[playerCapitalSlot].Barrels;muzzle=barrel.TransformPoint(new Vector3(0,0,CapitalMuzzleZ));velocity=NavalCapitalLayout.Speed;}
  else {barrel=barrels[active-1];muzzle=barrel.TransformPoint(new Vector3(0,0,hull.Definition.GunMuzzleZ));velocity=hull.Definition.ProjectileSpeed;}
  float sea=ocean.Height(muzzle.x,muzzle.z);
  float setting=NavalRangefinderMath.SeaRange(muzzle,barrel.forward*velocity+FiringPlatformVelocity(),sea);
  bool available=world.RangefinderCrewReady&&RangeBridgeReady();
  if(available&&Time.time>=rangeReview){
   rangeReview=Time.time+2f;Transform selected=null;float best=Mathf.Cos(25f*Mathf.Deg2Rad);
   foreach(var other in world.ConvoyHulls()){
    if(other==hull||other.Sunk||other.SimulationRetired)continue;
    if(world.ConvoyMission&&other.Definition.Destroyer==hull.Definition.Destroyer)continue;
    if(!world.ConvoyMission&&other.Root!=world.EnemyBoat)continue;
    Vector3 delta=other.Root.position-muzzle;float score=Vector3.Dot(Vector3.ProjectOnPlane(barrel.forward,Vector3.up).normalized,Vector3.ProjectOnPlane(delta,Vector3.up).normalized);
    if(score>best){best=score;selected=other.Root;}
   }
   // Ordinary duels are not enumerated by ConvoyHulls.
   if(!world.ConvoyMission&&world.EnemyBoat!=null){Vector3 d=world.EnemyBoat.position-muzzle;if(Vector3.Angle(Vector3.ProjectOnPlane(barrel.forward,Vector3.up),Vector3.ProjectOnPlane(d,Vector3.up))<25f)selected=world.EnemyBoat;}
   rangeTarget=selected;
   if(selected!=null){float actual=Vector3.ProjectOnPlane(selected.position-muzzle,Vector3.up).magnitude;rangeError=Mathf.Max(10f,actual*.02f);rangeReading=Mathf.Round((actual+Random.Range(-rangeError,rangeError))/10f)*10f;}
  }
  string report=!available?"Дальномер недоступен: нужны рубка и рулевой":rangeTarget==null?"Дальномер: наведитесь на корабль":"Цель ≈ "+rangeReading.ToString("F0")+" ± "+rangeError.ToString("F0")+" м";
  GUI.Box(new Rect(Screen.width*.5f-260,Screen.height-144,520,36),report+"  |  Прицел ≈ "+setting.ToString("F0")+" м");
 }
}

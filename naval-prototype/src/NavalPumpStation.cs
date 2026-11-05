using UnityEngine;
// Independent physical station, shared ship-wide hydraulic circuit.
public sealed class NavalPumpStation
{
 public Vector3 Post;
 public Transform Crank;
 public NavalRepairableMechanism Mechanism;
 public NavalRepairSailor Operator;
 public float LastWork=-100f,Phase;
 public bool LastTransfer;
 public bool Functional {get{return Mechanism!=null&&Mechanism.Functional;}}
}
public partial class NavalBilgeSystem
{
 private readonly System.Collections.Generic.List<NavalPumpStation> pumps=new System.Collections.Generic.List<NavalPumpStation>();
 private NavalPumpStation playerStation;
 public int PumpCount {get{return pumps.Count;}}
 public NavalPumpStation PumpAt(int index){return pumps[index];}
 public NavalRepairSailor Operator {get{foreach(var pump in pumps)if(pump.Operator!=null)return pump.Operator;return null;}}
 public int OpenPumpSlots {get{int total=0;foreach(var pump in pumps)if(pump.Functional&&pump.Operator==null&&(!playerPump||pump!=playerStation))total++;return total;}}
 public NavalPumpStation NearestPump(Vector3 local,bool free)
 {
  NavalPumpStation result=null;float best=float.PositiveInfinity;
  foreach(var pump in pumps){
   if(!pump.Functional||free&&(pump.Operator!=null||playerPump&&playerStation==pump))continue;
   float travel=0;var previous=new NavalWalkPoint(local.x,local.y,local.z);
   foreach(var point in NavalShipRoutes.Plan(previous,new NavalWalkPoint(pump.Post.x,pump.Post.y,pump.Post.z),hull.Definition)){
    travel+=new Vector3(point.X-previous.X,point.Y-previous.Y,point.Z-previous.Z).magnitude;previous=point;
   }
   if(travel<best){best=travel;result=pump;}
  }
  return result;
 }
 public Vector3 NearestPumpPost(Vector3 local){var station=NearestPump(local,true);return station!=null?station.Post:PumpPost;}
 public bool PlayerUsingStation(NavalPumpStation station){return playerPump&&playerStation==station;}
 private void StopStation(NavalPumpStation station)
 {
  station.LastWork=-100f;
  if(playerStation==station)ReleasePlayer();
  var worker=station.Operator;station.Operator=null;if(worker!=null)worker.CancelPumpDuty();
  if(station.Crank!=null)station.Crank.localRotation=Quaternion.identity;
 }
 private void BuildPumpStation(float floor,Material trim,Material metal)
 {
  if(hull.Definition.Destroyer){
   float q=hull.WorldScale;var pump=new NavalPumpStation{Post=new Vector3(NavalDestroyerAccess.PumpX/q,floor,NavalDestroyerAccess.PumpZ/q)};int begin=hull.Voxels.Count;
   NavalDestroyerLayout.Box(hull,metal,pump.Post+new Vector3(0,.5f,-.9f)/q,new Vector3(1f,1f,1f),false,true,null);
   pump.Crank=new GameObject("Pump crank").transform;pump.Crank.SetParent(transform,false);pump.Crank.localPosition=pump.Post+new Vector3(0,1.1f,-.9f)/q;
   hull.AddDecorationBox(pump.Crank,trim,Vector3.zero,new Vector3(1.1f,.12f,.12f)/q,false);
   var cells=new System.Collections.Generic.List<NavalHullVoxel>();for(int i=begin;i<hull.Voxels.Count;i++)cells.Add(hull.Voxels[i]);pump.Mechanism=new NavalRepairableMechanism(hull,cells,true);pumps.Add(pump);return;
  }
  var station=new NavalPumpStation{Post=new Vector3(0,floor,-4.9f)};int first=hull.Voxels.Count;
  hull.AddDecorationBox(transform,metal,station.Post+new Vector3(0,.3f,NavalMaintenanceMath.PumpOffsetZ),new Vector3(.5f,.6f,.5f),true);
  hull.AddDecorationBox(transform,trim,station.Post+new Vector3(0,.65f,NavalMaintenanceMath.PumpOffsetZ),new Vector3(.8f,.12f,.15f),true);
  if(hull.Definition.SteamPowered)
  {
   // Compact gauge chest and exposed delivery pipes; visual details do not
   // obstruct the aft pump approach.
   hull.AddDecorationBox(transform,metal,station.Post+new Vector3(0,.48f,NavalMaintenanceMath.PumpOffsetZ+.13f),new Vector3(.44f,.34f,.10f),false);
   hull.AddDecorationBox(transform,trim,station.Post+new Vector3(0,.49f,NavalMaintenanceMath.PumpOffsetZ+.20f),new Vector3(.22f,.22f,.04f),false);
  }
  station.Crank=new GameObject("Bilge pump handle").transform;station.Crank.SetParent(transform,false);station.Crank.localPosition=station.Post+new Vector3(0,.65f,NavalMaintenanceMath.PumpOffsetZ);
  // Keep the transverse delivery pipes behind the strongly tapered stern
  // shell.  The old fixed 5.6 m span crossed both sides of the ironclad here.
  float pipeHalf=NavalPumpLayout.PipeHalfSpan(hull.Definition,-5.45f);
  for(int side=-1;side<=1;side+=2)hull.AddDecorationBox(transform,metal,new Vector3(side*pipeHalf*.5f,floor+.13f,-5.45f),new Vector3(pipeHalf,.12f,.12f),false);
  var members=new System.Collections.Generic.List<NavalHullVoxel>();
  for(int i=first;i<hull.Voxels.Count;i++){NavalHullVoxel cell=hull.Voxels[i];if(hull.Definition.SteamPowered){cell.Metallic=true;cell.Flammable=false;}members.Add(cell);}
  foreach(var cell in members)if(cell.LocalCenter.y>floor+.6f)cell.transform.SetParent(station.Crank,true);
  station.Mechanism=new NavalRepairableMechanism(hull,members,true);pumps.Add(station);
 }
}
public static class NavalPumpLayout
{
 public static float PipeHalfSpan(NavalShipDefinition ship,float z)
 {return Mathf.Max(.12f,ship.HullHalfWidth(z)-.14f);}
}

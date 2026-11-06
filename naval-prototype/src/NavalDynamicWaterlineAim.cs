using UnityEngine;
public partial class NavalStations {
 private readonly Vector3[] liveMainAim=new Vector3[16];
 private readonly float[] liveMainAimAt=new float[16];
 private readonly NavalHullVoxel[] liveMainAimCell=new NavalHullVoxel[16];
 private Vector3 CurrentMainAim(int sequence,Vector3 source){NavalHullVoxel cell;return CurrentMainAim(sequence,source,out cell);}
 private Vector3 CurrentMainAim(int sequence,Vector3 source,out NavalHullVoxel selected){
  Vector3 view=boat.InverseTransformPoint(source);int side=view.x<0?-1:1;
  int zone=((sequence%8)+8)%8,key=zone*2+(side>0?1:0);
  selected=liveMainAimCell[key];
  if(Time.time<liveMainAimAt[key]&&(object.ReferenceEquals(selected,null)||TrackedHeavyAimValid(selected)))return liveMainAim[key];
  selected=null;
  liveMainAimAt[key]=Time.time+.5f;PrepareTargetIndex();PrepareAimSamples(ocean);
  Vector3 original=NavalMainBatteryMath.AimPoint(hull.Definition,zone,side),chosen=original;
  Vector3 preferred=boat.TransformPoint(original);float best=float.PositiveInfinity;
  // Select actual remaining cells, never a virtual point on the old waterline.
  for(int pass=0;pass<2;pass++)foreach(var cell in pass==0?shellTargets:deckTargets){
   if(cell==null||cell.Missing||cell.Detached)continue;
   Vector3 world;float depth=SampleAimDepth(cell,out world);
   if(depth<.03f)continue;
   Vector3 local=boat.InverseTransformPoint(world);
   float incidence=ArmorAimIncidenceLocal(cell,view,pass==1);if(incidence<.12f)continue;
   float score=Mathf.Abs(depth-.25f)*2f+(world-preferred).magnitude*.08f+(pass==1?.3f:0)+(1f-incidence)*18f;
   if(score>=best)continue;best=score;chosen=local;selected=cell;
  }
  liveMainAimCell[key]=selected;liveMainAim[key]=chosen;return chosen;
 }
 private bool TrackedHeavyAimValid(NavalHullVoxel cell){
  if(cell==null||cell.Missing||cell.Detached)return false;
  Vector3 point=cell.transform.position;
  return point.y>=ocean.Height(point.x,point.z)+.03f;
 }
}

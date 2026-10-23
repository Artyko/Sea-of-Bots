using System.Collections.Generic;
using UnityEngine;
public partial class NavalWorldMap
{
 private readonly List<Bounds> islandSolids=new List<Bounds>();
 private readonly List<int> nearIslandSolids=new List<int>(),clearanceIslandSolids=new List<int>();
 private readonly List<float> initialPenetration=new List<float>();
 private readonly NavalBoundsIndex islandIndex=new NavalBoundsIndex();
 private void IndexIsland(Bounds box){int id=islandSolids.Count;islandSolids.Add(box);Vector3 min=box.min,max=box.max;islandIndex.Add(id,min.x,min.y,min.z,max.x,max.y,max.z);}
 private void QueryIslands(Bounds box,List<int> output){Vector3 min=box.min,max=box.max;islandIndex.Query(min.x,min.y,min.z,max.x,max.y,max.z,output);}
 private void RegisterIsland(ObjectPlacedInLevel obj)
 {
  if(!obj.name.StartsWith("Island ")||obj.name.IndexOf("terrain")<0)return;
  // Editor CubeFlat blocks are solids even when a native physics query misses
  // a layer, a disabled shape or the interior/back face of a mesh collider.
  foreach(Renderer renderer in obj.GetComponentsInChildren<Renderer>())
   if(renderer!=null&&renderer.bounds.size.sqrMagnitude>.001f)IndexIsland(renderer.bounds);
 }
 private static NavalBoxVector BoxVector(Vector3 v){return new NavalBoxVector(v.x,v.y,v.z);}
 private static NavalShoreBox IslandBox(Bounds b)
 {return new NavalShoreBox{Center=BoxVector(b.center),Half=BoxVector(b.extents),Right=BoxVector(Vector3.right),Up=BoxVector(Vector3.up),Forward=BoxVector(Vector3.forward)};}
 private static NavalShoreBox ShipBox(Vector3 point,Quaternion rotation,Vector3 offset,Vector3 half)
 {return new NavalShoreBox{Center=BoxVector(point+rotation*offset),Half=BoxVector(half+Vector3.one*.06f),Right=BoxVector(rotation*Vector3.right),Up=BoxVector(rotation*Vector3.up),Forward=BoxVector(rotation*Vector3.forward)};}
 private bool IslandAdvance(Vector3 from,Quaternion oldRotation,Vector3 to,Quaternion rotation,Vector3 offset,Vector3 half)
 {
  nearIslandSolids.Clear();
  float radius=half.magnitude+offset.magnitude+.2f;
  Bounds broad=new Bounds((from+to)*.5f,new Vector3(Mathf.Abs(to.x-from.x),Mathf.Abs(to.y-from.y),Mathf.Abs(to.z-from.z))+Vector3.one*(radius*2));
  QueryIslands(broad,nearIslandSolids);
  for(int i=nearIslandSolids.Count-1;i>=0;i--)if(!broad.Intersects(islandSolids[nearIslandSolids[i]]))nearIslandSolids.RemoveAt(i);
  if(nearIslandSolids.Count==0)return true;
  var initial=ShipBox(from,oldRotation,offset,half);
  initialPenetration.Clear();foreach(int index in nearIslandSolids)initialPenetration.Add(NavalShoreBoxMath.Penetration(initial,IslandBox(islandSolids[index])));
  int steps=NavalShoreSweepMath.Steps(Vector3.Distance(from,to),Quaternion.Angle(oldRotation,rotation),radius);
  for(int step=1;step<=steps;step++)
  {
   float t=(float)step/steps;var next=ShipBox(Vector3.Lerp(from,to,t),Quaternion.Slerp(oldRotation,rotation,t),offset,half);
   var policy=new NavalShoreEscape();
   for(int i=0;i<nearIslandSolids.Count;i++){var land=IslandBox(islandSolids[nearIslandSolids[i]]);policy.Observe(initialPenetration[i],NavalShoreBoxMath.Penetration(next,land));}
   if(!policy.Allowed)return false;
  }
  return true;
 }
 private float IslandClearance(Vector3 point,Vector3 direction,Quaternion heading,Vector3 offset,Vector3 half,float range)
 {
  Vector3 r=heading*Vector3.right,u=heading*Vector3.up,f=heading*Vector3.forward;
  Vector3 extents=new Vector3(Mathf.Abs(r.x)*half.x+Mathf.Abs(u.x)*half.y+Mathf.Abs(f.x)*half.z,Mathf.Abs(r.y)*half.x+Mathf.Abs(u.y)*half.y+Mathf.Abs(f.y)*half.z,Mathf.Abs(r.z)*half.x+Mathf.Abs(u.z)*half.y+Mathf.Abs(f.z)*half.z);
  Ray ray=new Ray(point+heading*offset,direction);
  Bounds query=new Bounds(ray.origin+direction*(range*.5f),new Vector3(Mathf.Abs(direction.x)*range,Mathf.Abs(direction.y)*range,Mathf.Abs(direction.z)*range)+extents*2f);
  QueryIslands(query,clearanceIslandSolids);
  foreach(int id in clearanceIslandSolids){Bounds original=islandSolids[id];if(!query.Intersects(original))continue;Bounds expanded=original;expanded.Expand(extents*2f);float hit;if(expanded.Contains(ray.origin))range=0;else if(expanded.IntersectRay(ray,out hit)&&hit>=0)range=Mathf.Min(range,hit);}
  return range;
 }
}

using UnityEngine;
using System.Collections.Generic;
// Intact cargo uses five crate panels plus one bounded content volume.
// External damage immediately restores exact voxel colliders before subsequent queries.
public sealed class NavalCargoCollision : MonoBehaviour {
 private NavalHull hull;private NavalCargoCrate crate;
 private List<NavalHullVoxel> shell,contents;private readonly List<NavalCargoProxy> proxies=new List<NavalCargoProxy>();
 private bool compact,dirty;private int shellCount=-1,contentCount=-1;
 public bool Compact {get{return compact;}}
 public int ActiveShapes {get{
  int n=0;if(compact){foreach(var p in proxies)if(p!=null&&p.Shape.enabled)n++;}
  else {foreach(var c in shell)if(c!=null&&c.PhysicsCollider.enabled)n++;foreach(var c in contents)if(c!=null&&c.PhysicsCollider.enabled)n++;}return n;
 }}
 public void Initialize(NavalHull owner,NavalCargoCrate stock,List<NavalHullVoxel> sides,List<NavalHullVoxel> items){
  hull=owner;crate=stock;shell=sides;contents=items;
  foreach(var cell in shell)cell.CargoCollision=this;foreach(var cell in contents)cell.CargoCollision=this;
  TryCompact();
 }
 public void TryCompact(){
  foreach(var cell in shell)if(cell==null||cell.Missing||cell.Detached||cell.transform.parent!=hull.Root)return;
  foreach(var cell in contents)if(cell==null||cell.Detached||cell.transform.parent!=hull.Root)return;
  compact=true;SetSources(shell,true);SetSources(contents,true);dirty=true;shellCount=contentCount=-1;RefreshShapes();
 }
 private void SetSources(List<NavalHullVoxel> cells,bool batched){foreach(var cell in cells)if(cell!=null){cell.ColliderBatched=batched;cell.PhysicsCollider.enabled=!batched&&!cell.Missing;if(hull.PhysicsBody!=null)hull.PhysicsBody.MarkDirty(cell);}}
 public void SourceChanged(){
  crate.NotifyDamage();
  if(!compact)return;
  if(crate.UpdatingContents){dirty=true;return;}
  // No old solid box may cover a new hole, a detached part or a destroyed resource.
  compact=false;foreach(var proxy in proxies)if(proxy!=null)proxy.Shape.enabled=false;
  SetSources(shell,false);SetSources(contents,false);NavalPhysicsQueries.Invalidate();
 }
 private NavalCargoProxy Proxy(int index,List<NavalHullVoxel> cells){
  if(index<proxies.Count)return proxies[index];
  var obj=new GameObject("Naval cargo compact collision");obj.layer=PhysicsManager.GetEnvironmentLayer();obj.transform.SetParent(hull.Root,false);
  var shape=obj.AddComponent<BoxCollider>();shape.enabled=false;shape.sharedMaterial=shell[0].PhysicsCollider.sharedMaterial;shape.contactOffset=shell[0].PhysicsCollider.contactOffset;
  var proxy=obj.AddComponent<NavalCargoProxy>();proxy.Shape=shape;proxy.Cells=cells;proxies.Add(proxy);return proxy;
 }
 private void Box(int index,Vector3 center,Vector3 size,List<NavalHullVoxel> cells){var proxy=Proxy(index,cells);proxy.Shape.center=center;proxy.Shape.size=size;proxy.Shape.enabled=true;}
 public void RefreshShapes(){
  if(!compact||!dirty)return;dirty=false;
  int sides=0,items=0;foreach(var c in shell)if(!c.Missing&&!c.Detached)sides++;foreach(var c in contents)if(!c.Missing&&!c.Detached)items++;
  // Inventory changes inside a partially emptied crate never reenable individual colliders.
  if(sides!=shellCount){
   float scale=hull.WorldScale;Vector3 p=crate.Point;
   Box(0,p+Vector3.up*(.1f/scale),new Vector3(1.3f,.2f,1.3f)/scale,shell);
   Box(1,p+new Vector3(-.60f,.55f,0)/scale,new Vector3(.16f,.72f,1.35f)/scale,shell);
   Box(2,p+new Vector3(.60f,.55f,0)/scale,new Vector3(.16f,.72f,1.35f)/scale,shell);
   Box(3,p+new Vector3(0,.55f,-.60f)/scale,new Vector3(1.35f,.72f,.16f)/scale,shell);
   Box(4,p+new Vector3(0,.55f,.60f)/scale,new Vector3(1.35f,.72f,.16f)/scale,shell);
   if(sides==0)for(int i=0;i<5;i++)proxies[i].Shape.enabled=false;
   shellCount=sides;
  }
  if(items!=contentCount){
   var proxy=Proxy(5,contents);proxy.Shape.enabled=false;
   bool first=true;Bounds bounds=new Bounds();
   foreach(var cell in contents)if(!cell.Missing&&!cell.Detached){
    var box=cell.PhysicsCollider as BoxCollider;if(box==null)continue;
    var b=new Bounds(cell.transform.localPosition+Vector3.Scale(box.center,cell.transform.localScale),Vector3.Scale(box.size,cell.transform.localScale));
    if(first){bounds=b;first=false;}else bounds.Encapsulate(b);
   }
   if(!first){proxy.Shape.center=bounds.center;proxy.Shape.size=bounds.size;proxy.Shape.enabled=true;}
   contentCount=items;
  }
  NavalPhysicsQueries.Invalidate();
 }
 private void OnDestroy(){foreach(var proxy in proxies)if(proxy!=null)Destroy(proxy.gameObject);if(shell!=null){SetSources(shell,false);SetSources(contents,false);foreach(var c in shell)if(c!=null)c.CargoCollision=null;foreach(var c in contents)if(c!=null)c.CargoCollision=null;}}
}
public sealed class NavalCargoProxy : MonoBehaviour {
 public BoxCollider Shape;public List<NavalHullVoxel> Cells;
 public NavalHullVoxel Resolve(Vector3 point){
  if(Shape==null||!Shape.enabled)return null;
  NavalHullVoxel nearest=null;float distance=float.PositiveInfinity;
  foreach(var cell in Cells)if(cell!=null&&!cell.Missing&&!cell.Detached){
   Vector3 local=cell.transform.InverseTransformPoint(point);
   local=new Vector3(Mathf.Clamp(local.x,-.5f,.5f),Mathf.Clamp(local.y,-.5f,.5f),Mathf.Clamp(local.z,-.5f,.5f));
   float d=(cell.transform.TransformPoint(local)-point).sqrMagnitude;if(d<distance){distance=d;nearest=cell;}
  }return nearest;
 }
}

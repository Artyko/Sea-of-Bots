using UnityEngine;
using System.Collections.Generic;
public sealed class NavalCargoCrate {
 public readonly NavalCargoStock Stock;public readonly Vector3 Point,Pickup;public readonly int Kind,Gun;
 private readonly NavalHull hull;private readonly List<NavalHullVoxel> shell=new List<NavalHullVoxel>(),contents=new List<NavalHullVoxel>();
 private int shown=-1;private bool updating,damageDirty=true;
 public void NotifyDamage(){if(!updating)damageDirty=true;}
 public int CollisionShapes {get{return collision!=null?collision.ActiveShapes:CollisionSources;}}
 public bool CompactCollision {get{return collision!=null&&collision.Compact;}}
 public int CollisionSources {get{return shell.Count+contents.Count;}}
 public bool UpdatingContents {get{return updating;}}
 private NavalCargoCollision collision;
 public NavalCargoCrate(NavalHull owner,Vector3 point,int kind,int gun,Material wood,Material trim,Material iron){
 hull=owner;Point=point;Kind=kind;Gun=gun;Stock=new NavalCargoStock(kind==1?40:kind==2?1:20);
 Pickup=point+new Vector3(point.x>0?-1.65f:1.65f,0,0)/hull.WorldScale;
 if(gun<0){var pickup=NavalCargoLayout.Pickup(hull.Definition,new NavalWalkPoint(point.x,point.y,point.z));Pickup=new Vector3(pickup.X,pickup.Y,pickup.Z);}
 int first=hull.Voxels.Count;
 Box(wood,new Vector3(0,.10f,0),new Vector3(1.3f,.20f,1.3f));
 // Open slatted sides, broad corner posts and contrasting rim.
 for(int side=-1;side<=1;side+=2){
  for(int row=0;row<3;row++){
   Box(wood,new Vector3(side*.60f,.28f+row*.22f,0),new Vector3(.10f,.18f,1.3f));
   Box(wood,new Vector3(0,.28f+row*.22f,side*.60f),new Vector3(1.3f,.18f,.10f));
  }
  Box(trim,new Vector3(side*.60f,.85f,0),new Vector3(.14f,.12f,1.35f));
  Box(trim,new Vector3(0,.85f,side*.60f),new Vector3(1.35f,.12f,.14f));
  for(int end=-1;end<=1;end+=2)Box(trim,new Vector3(side*.59f,.48f,end*.59f),new Vector3(.16f,.9f,.16f));
 }
 for(int i=first;i<hull.Voxels.Count;i++)shell.Add(hull.Voxels[i]);first=hull.Voxels.Count;
 if(kind==0){
  for(int layer=0;layer<2;layer++)for(int x=0;x<3;x++)for(int z=0;z<3;z++)Ball(iron,new Vector3((x-1)*.37f,.38f+layer*.35f,(z-1)*.37f),.34f);
  for(int x=-1;x<=1;x+=2)Ball(iron,new Vector3(x*.185f,1.04f,0),.34f);
 }else if(kind==1){
  // Long boards visibly protrude above the open rim, with alternating wood colours.
  for(int row=0;row<5;row++)for(int x=0;x<3;x++)
   Box((row+x)%2==0?trim:wood,new Vector3((x-1)*.32f,.28f+row*.13f,0),new Vector3(.25f,.10f,1.12f));
  for(int x=-1;x<=1;x++)Box(x==0?wood:trim,new Vector3(x*.3f,1.05f,.28f),new Vector3(.22f,1.55f,.12f));
 }else {
  // A barrel-shaped cannon tube and wooden carriage parts extend above the crate.
  for(int ring=0;ring<5;ring++)for(int side=0;side<4;side++){
   float angle=side*Mathf.PI*.5f;
   Box(iron,new Vector3(Mathf.Cos(angle)*.18f,.38f+ring*.23f,Mathf.Sin(angle)*.18f),new Vector3(.18f,.23f,.18f));
  }
  Box(trim,new Vector3(.40f,.88f,0),new Vector3(.16f,1.35f,.8f));
  Box(wood,new Vector3(-.4f,.48f,0),new Vector3(.18f,.35f,1f));
 }
 for(int i=first;i<hull.Voxels.Count;i++)contents.Add(hull.Voxels[i]);shown=Stock.Count;
 collision=hull.Root.gameObject.AddComponent<NavalCargoCollision>();collision.Initialize(hull,this,shell,contents);
 }
 private static Mesh ballMesh;
 private void Ball(Material material,Vector3 offset,float diameter){
  if(ballMesh==null){
   var vertices=new List<Vector3>();var triangles=new List<int>();
   const int rings=4,sides=6;
   for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++){
    float latitude=Mathf.PI*y/rings,longitude=2*Mathf.PI*x/sides;
    vertices.Add(new Vector3(Mathf.Sin(latitude)*Mathf.Cos(longitude),Mathf.Cos(latitude),Mathf.Sin(latitude)*Mathf.Sin(longitude))*.5f);
   }
   for(int y=0;y<rings;y++)for(int x=0;x<sides;x++){int i=y*(sides+1)+x;
    triangles.Add(i);triangles.Add(i+1);triangles.Add(i+sides+1);triangles.Add(i+1);triangles.Add(i+sides+2);triangles.Add(i+sides+1);}
   ballMesh=new Mesh();ballMesh.name="Cargo cannonball low-poly";ballMesh.SetVertices(vertices);ballMesh.SetTriangles(triangles,0);ballMesh.RecalculateNormals();ballMesh.RecalculateBounds();
  }
  var cell=hull.AddVoxel(hull.Root,material,Point+offset/hull.WorldScale,Vector3.one*(diameter/hull.WorldScale),false,true);
  cell.GetComponent<MeshFilter>().sharedMesh=ballMesh;
 }
 private void Box(Material mat,Vector3 offset,Vector3 size){hull.AddDecorationBox(hull.Root,mat,Point+offset/hull.WorldScale,size/hull.WorldScale,true);}
 public int Count {get{Refresh();return Stock.Count;}}
 public void Refresh(){if(updating)return;updating=true;try{
 if(damageDirty){damageDirty=false;foreach(var cell in shell)if(cell==null||cell.Missing||cell.Detached){Stock.Break();break;}
 // Destruction of the visible resource also loses the crate's stock.
 if(shown>0){int expected=Mathf.CeilToInt(contents.Count*shown/(float)Stock.Capacity);for(int i=0;i<expected;i++)if(contents[i]==null||contents[i].Missing||contents[i].Detached){Stock.Break();break;}}
 }
 if(shown!=Stock.Count){int visible=Stock.Count==0?0:Mathf.CeilToInt(contents.Count*Stock.Count/(float)Stock.Capacity);
 for(int i=0;i<contents.Count;i++)if(contents[i]!=null&&contents[i].Missing!=(i>=visible))contents[i].SetMissing(i>=visible);
 if(Kind==1&&Stock.Count==0)foreach(var cell in shell)if(cell!=null&&!cell.Missing)cell.SetMissing(true);
 shown=Stock.Count;}
 }finally{updating=false;if(collision!=null)collision.RefreshShapes();}}
 public int Take(int amount){Refresh();int n=Stock.Take(amount);Refresh();return n;}
 public int Put(int amount){Refresh();int n=Stock.Put(amount);Refresh();return n;}
 public void Reset(){Stock.Reset();shown=-1;damageDirty=false;Refresh();if(collision!=null)collision.TryCompact();}
}

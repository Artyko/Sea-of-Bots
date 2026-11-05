using UnityEngine;
using System.Collections.Generic;
public sealed class NavalCargoCrate {
 public readonly NavalCargoStock Stock;public readonly Vector3 Point,Pickup;public readonly int Kind,Gun;public readonly float Width;
 private readonly NavalHull hull;private readonly List<NavalHullVoxel> shell=new List<NavalHullVoxel>(),contents=new List<NavalHullVoxel>();
 private int shown=-1;private bool updating,damageDirty=true;
 public void NotifyDamage(){if(!updating)damageDirty=true;}
 public int CollisionShapes {get{return collision!=null?collision.ActiveShapes:CollisionSources;}}
 public int CompactCollisionShapes {get{return collision!=null&&collision.Compact?collision.ActiveShapes:0;}}
 public bool CompactCollision {get{return collision!=null&&collision.Compact;}}
 public int CollisionSources {get{return shell.Count+contents.Count;}}
 public bool ExplosiveAmmunition {get{return Kind==0&&Gun<0&&shell.Count+contents.Count>0;}}
 public Vector3 ExplosivePoint {get{return Point+Vector3.up*(.58f/hull.WorldScale);}}
 public List<NavalHullVoxel> ExplosiveCells()
 {var result=new List<NavalHullVoxel>(shell.Count+contents.Count);result.AddRange(shell);result.AddRange(contents);return result;}
 public bool UpdatingContents {get{return updating;}}
 private NavalCargoCollision collision;
 private static Material steel,mark;
 private static void RackMaterials(){if(steel!=null)return;var shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");steel=new Material(shader);steel.color=new Color(.50f,.56f,.60f);mark=new Material(shader);mark.color=new Color(.92f,.91f,.73f);}

 public NavalCargoCrate(NavalHull owner,Vector3 point,int kind,int gun,Material wood,Material trim,Material iron,int units=1){
 hull=owner;Point=point;Kind=kind;Gun=gun;Width=hull.Definition.SteamPowered?1.2f:1.4f;Stock=new NavalCargoStock((kind==1?40:kind==2?1:20)*units);
  Pickup=hull.Definition.Destroyer&&gun>=0?hull.Definition.GunOperator(gun):hull.Definition.CapitalShip&&gun>=0?new Vector3(NavalShipDefinition.GunSide(gun)*hull.Definition.GunPostX,hull.Definition.GunDeck,NavalShipDefinition.GunZ(hull.Definition,gun)):hull.Definition.SteamPowered?new Vector3(point.x>0?.92f:-.92f,point.y,point.z):point+new Vector3(point.x>0?-1.65f:1.65f,0,0)/hull.WorldScale;
 if(gun<0){var pickup=NavalCargoLayout.Pickup(hull.Definition,new NavalWalkPoint(point.x,point.y,point.z));Pickup=new Vector3(pickup.X,pickup.Y,pickup.Z);}
 // Steam ready-use ammunition is stored inside each gun mounting. It remains
 // a logical replenishment target, but no loose steel crate or collider is
 // placed in the weather-deck passage.
 if(NavalCargoLayout.IntegratedReadyUse(hull.Definition)&&gun>=0){shown=Stock.Count;return;}
 int first=hull.Voxels.Count;
 float half=Width*.5f-.05f;
 RackMaterials();Material frame=kind==0?steel:trim, shelf=kind==0?steel:wood;
 // Tall open racks retain the previous footprint and full resource destruction rules.
 for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
  Box(frame,new Vector3(side*half,1.05f,end*half),new Vector3(.12f,2.10f,.12f));
 foreach(float y in new[]{.12f,.88f,1.64f})Box(shelf,new Vector3(0,y,0),new Vector3(Width,.10f,Width));
 for(int side=-1;side<=1;side+=2)Box(frame,new Vector3(side*half,2.02f,0),new Vector3(.10f,.12f,Width));
 if(kind==0){
  for(int side=-1;side<=1;side+=2){
   Box(steel,new Vector3(side*half,1.06f,0),new Vector3(.08f,2.12f,Width));
   Box(steel,new Vector3(0,1.06f,side*half),new Vector3(Width,2.12f,.08f));
  }
  // Raised contrasting projectile silhouette on each aisle-facing door.
  for(int side=-1;side<=1;side+=2){float x=side*(half+.055f);
   Box(mark,new Vector3(x,1.02f,0),new Vector3(.025f,.64f,.24f));
   Box(mark,new Vector3(x,1.40f,0),new Vector3(.025f,.12f,.16f));
   Box(mark,new Vector3(x,1.49f,0),new Vector3(.025f,.07f,.08f));
   Box(mark,new Vector3(x,.66f,0),new Vector3(.025f,.08f,.29f));
  }
 }
 for(int i=first;i<hull.Voxels.Count;i++)shell.Add(hull.Voxels[i]);first=hull.Voxels.Count;
 if(kind==0){
  if(hull.Definition.RifledWeapons){
   for(int layer=0;layer<2;layer++)for(int x=0;x<3;x++)for(int z=0;z<3;z++)Shell(iron,new Vector3((x-1)*.34f,.42f+layer*.76f,(z-1)*.34f));
   for(int x=-1;x<=1;x+=2)Shell(iron,new Vector3(x*.18f,1.88f,0));
  }else{
   for(int layer=0;layer<2;layer++)for(int x=0;x<3;x++)for(int z=0;z<3;z++)Ball(iron,new Vector3((x-1)*.37f,.38f+layer*.35f,(z-1)*.37f),.34f);
   for(int x=-1;x<=1;x+=2)Ball(iron,new Vector3(x*.185f,1.04f,0),.34f);
  }
 }else if(kind==1){
  // Long boards visibly protrude above the open rim, with alternating wood colours.
  for(int row=0;row<5;row++)for(int x=0;x<3;x++)
   Box((row+x)%2==0?trim:wood,new Vector3((x-1)*.32f,.28f+row*.13f,0),new Vector3(.25f,.10f,1.12f));
  for(int x=-1;x<=1;x++)Box(x==0?wood:trim,new Vector3(x*.3f,1.05f,.28f),new Vector3(.22f,1.55f,.12f));
 }else {
  if(hull.Definition.RifledWeapons){
   Box(iron,new Vector3(0,.55f,0),new Vector3(.42f,1.25f,.42f));
   Box(trim,new Vector3(.38f,.62f,0),new Vector3(.24f,.72f,.62f));
   Box(iron,new Vector3(-.38f,.62f,0),new Vector3(.18f,1.15f,.18f));
  }else{
   // A barrel-shaped cannon tube and wooden carriage parts extend above the crate.
   for(int ring=0;ring<5;ring++)for(int side=0;side<4;side++){
    float angle=side*Mathf.PI*.5f;
    Box(iron,new Vector3(Mathf.Cos(angle)*.18f,.38f+ring*.23f,Mathf.Sin(angle)*.18f),new Vector3(.18f,.23f,.18f));
   }
   Box(trim,new Vector3(.40f,.88f,0),new Vector3(.16f,1.35f,.8f));
   Box(wood,new Vector3(-.4f,.48f,0),new Vector3(.18f,.35f,1f));
  }
 }
 for(int i=first;i<hull.Voxels.Count;i++)contents.Add(hull.Voxels[i]);shown=Stock.Count;
 collision=hull.Root.gameObject.AddComponent<NavalCargoCollision>();collision.Initialize(hull,this,shell,contents);
 }
 private static Mesh ballMesh;
 private static void EnsureBallMesh(){
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
 }
 private void Ball(Material material,Vector3 offset,float diameter){
  EnsureBallMesh();
  var cell=hull.AddVoxel(hull.Root,material,Point+offset/hull.WorldScale,Vector3.one*(diameter/hull.WorldScale),false,true);
  cell.GetComponent<MeshFilter>().sharedMesh=ballMesh;
 }
 private void Shell(Material material,Vector3 offset){
  EnsureBallMesh();
  var cell=hull.AddVoxel(hull.Root,material,Point+offset/hull.WorldScale,new Vector3(.20f,.52f,.20f)/hull.WorldScale,false,true);
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
 public void DestroyAmmunition(){Refresh();Stock.Take(Stock.Count);Refresh();}
 public void Reset(){Stock.Reset();shown=-1;damageDirty=false;Refresh();if(collision!=null)collision.TryCompact();}
}

using System.Collections.Generic;
using UnityEngine;
// Shared hinged steel closures. Geometry, crew clearance and flood edges use the same positions.
public sealed class NavalWatertightBulkheads:MonoBehaviour {
 private sealed class Door {public Transform Leaf;public float Angle;public bool HeldOpen;public Vector3 Post;public readonly List<NavalHullVoxel> Cells=new List<NavalHullVoxel>();}
 private sealed class Edge {public int Index;public float Previous;public Vector3 Top;public NavalHullVoxel Panel;public Door Door;}
 private readonly List<Door> doors=new List<Door>();private readonly List<Edge> edges=new List<Edge>();private NavalHull hull;private Door nearest;
 public void Initialize(NavalHull owner,Material steel){hull=owner;var s=hull.Definition;var grid=hull.Flood;float q=hull.WorldScale;
  foreach(int row in NavalDestroyerAccess.BulkheadRows){float z=(grid.Z(row*grid.Columns)+grid.Z((row+1)*grid.Columns))*.5f;var pair=new Door[2];
   for(int side=-1;side<=1;side+=2){var d=new Door();pair[side<0?0:1]=d;d.Post=new Vector3(side*5.2f/q,s.HoldFloor,z);d.Leaf=new GameObject("Watertight door").transform;d.Leaf.SetParent(transform,false);d.Angle=90;d.Leaf.localRotation=Quaternion.Euler(0,side*90,0);d.Leaf.localPosition=d.Post+new Vector3(side*1.6f/q,0,0);int first=hull.Voxels.Count;
    hull.AddDecorationBox(d.Leaf,steel,new Vector3(-side*1.6f/q,2.8f/q,0),new Vector3(3.2f,5.6f,.12f)/q,true);for(int n=first;n<hull.Voxels.Count;n++){var c=hull.Voxels[n];c.Metallic=true;c.Flammable=false;c.MovableEquipment=true;c.InternalBarrier=true;d.Cells.Add(c);}new NavalRepairableMechanism(hull,d.Cells,false,false,"Водонепроницаемая дверь");doors.Add(d);
   }
   for(int col=0;col<grid.Columns;col++){float x=grid.X(col),worldX=Mathf.Abs(x*q);Door d=worldX+grid.CellWidth*q*.5f>3.6f&&worldX-grid.CellWidth*q*.5f<6.8f?pair[x<0?0:1]:null;NavalHullVoxel panel=null;
    if(d==null){panel=hull.AddVoxel(transform,steel,new Vector3(x,s.HoldFloor+2.8f/q,z),new Vector3(grid.CellWidth,5.6f/q,.12f/q),false,true);panel.Metallic=true;panel.InternalBarrier=true;}
    if(d!=null){float lo=worldX-grid.CellWidth*q*.5f,hi=worldX+grid.CellWidth*q*.5f;foreach(var segment in new[]{new Vector2(lo,Mathf.Min(hi,3.6f)),new Vector2(Mathf.Max(lo,6.8f),hi)})if(segment.y>segment.x+.001f)NavalDestroyerLayout.Box(hull,steel,new Vector3(Mathf.Sign(x)*(segment.x+segment.y)*.5f/q,s.HoldFloor+2.8f/q,z),new Vector3(segment.y-segment.x,5.6f,.12f),false,true,null);}
    edges.Add(new Edge{Index=row*grid.Columns+col,Previous=float.NegativeInfinity,Panel=panel,Door=d,Top=new Vector3(x,s.HoldFloor+5.6f/q,z)});
   }
  }
 }
 private bool Broken(Door door){foreach(var c in door.Cells)if(c.Missing||c.Detached)return true;return false;}
 private void Update(){if(hull==null||hull.SimulationRetired)return;float dt=Mathf.Min(Time.deltaTime,.05f);var world=NavalTestWorld.Active;if(world==null||!world.SimulationActive)return;
  nearest=null;float best=2f;
  foreach(var door in doors){bool near=false;foreach(var worker in hull.RepairWorkers)if(worker!=null&&!worker.Stranded&&hull.Root.TransformVector(worker.CarriedPosition-door.Post).sqrMagnitude<16){near=true;break;}
   if(!world.Spectating&&world.PlayerHull!=null){Vector3 player=world.PlayerFootPoint;float d=Vector3.Distance(hull.Root.TransformPoint(door.Post),player);if(d<4)near=true;if(d<best&&!Broken(door)){nearest=door;best=d;}}
   float next=Mathf.MoveTowards(door.Angle,near||door.HeldOpen||hull.Volume<1f||Broken(door)?90:0,90*dt);if(Mathf.Abs(next-door.Angle)<.001f)continue;door.Angle=next;door.Leaf.localRotation=Quaternion.Euler(0,next*(door.Post.x<0?-1:1),0);foreach(var cell in door.Cells)hull.NoteCellMoved(cell);
  }
 }
 private void OnGUI(){if(nearest!=null)GUI.Box(new Rect(Screen.width*.5f-230,Screen.height-200,460,30),"K — "+(nearest.HeldOpen?"вернуть автозакрытие":"закрепить дверь открытой"));}
 private void LateUpdate(){if(nearest!=null&&Input.GetKeyDown(KeyCode.K))nearest.HeldOpen=!nearest.HeldOpen;}
 public void RestoreSills(){foreach(var edge in edges)hull.Flood.ForwardSill[edge.Index]=edge.Previous;}
 public void ApplySills(){foreach(var e in edges){e.Previous=hull.Flood.ForwardSill[e.Index];bool open=e.Door!=null?(e.Door.Angle>65||Broken(e.Door)):e.Panel==null||e.Panel.Missing||e.Panel.Detached;if(!open)hull.Flood.ForwardSill[e.Index]=Mathf.Max(e.Previous,hull.Root.TransformPoint(e.Top).y);}}
}

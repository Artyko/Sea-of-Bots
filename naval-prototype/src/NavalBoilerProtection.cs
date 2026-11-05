using System.Collections.Generic;
using UnityEngine;
// Open-topped steel cofferdam around the fireboxes. Engineers service them from the aisle.
// Flood volume remains in the normal mass-bearing flood grid; each damaged panel opens its edge.
public sealed class NavalBoilerProtection {
 private sealed class Edge {public int Index;public bool Right;public float Previous;public NavalHullVoxel Panel;public Vector3 Top;}
 private readonly HashSet<NavalHullVoxel> panels=new HashSet<NavalHullVoxel>();
 public bool IsPanel(NavalHullVoxel cell){return panels.Contains(cell);}
 private readonly List<Edge> edges=new List<Edge>();private readonly NavalHull hull;private readonly bool[] inside;
 public NavalBoilerProtection(NavalHull owner,Material metal){
  hull=owner;var grid=hull.Flood;inside=new bool[grid.Water.Length];
  float z=NavalSteamLayout.BoilerZFor(hull.Definition);
  if(hull.Definition.Destroyer)return; // Raised fireboxes and room bulkheads protect this plant; no aisle-blocking individual cofferdams.
  if(hull.Definition.Destroyer){
   for(int i=0;i<inside.Length;i++)for(int boiler=0;boiler<6;boiler++)if(Mathf.Abs(grid.X(i)*hull.WorldScale)<2f&&Mathf.Abs(grid.Z(i)*hull.WorldScale-(26-boiler*8))<2f)inside[i]=true;
  }else for(int i=0;i<inside.Length;i++)inside[i]=Mathf.Abs(grid.X(i)-NavalSteamLayout.BoilerX)<.52f&&grid.Z(i)>z-.64f&&grid.Z(i)<z+.78f;
  for(int i=0;i<inside.Length;i++){
   if(i%grid.Columns+1<grid.Columns&&inside[i]!=inside[i+1])Build(i,i+1,true,metal);
   if(i+grid.Columns<inside.Length&&(inside[i]!=inside[i+grid.Columns]||hull.Definition.CapitalShip&&inside[i]&&inside[i+grid.Columns]&&grid.Z(i)<z&&grid.Z(i+grid.Columns)>=z))Build(i,i+grid.Columns,false,metal);
  }
 }
 private void Build(int a,int b,bool right,Material metal){
  var grid=hull.Flood;float height=1.1f/hull.WorldScale;
  Vector3 p=new Vector3((grid.X(a)+grid.X(b))*.5f,hull.Floor+height*.5f,(grid.Z(a)+grid.Z(b))*.5f);
  Vector3 size=right?new Vector3(.055f,height,grid.CellLength):new Vector3(grid.CellWidth,height,.055f);
  var cell=hull.AddVoxel(hull.Root,metal,p,size,false,true);cell.Metallic=true;cell.InternalBarrier=true;cell.Flammable=false;
  panels.Add(cell);
  edges.Add(new Edge{Previous=float.NegativeInfinity,Index=a,Right=right,Panel=cell,Top=p+Vector3.up*(height*.5f)});
 }
 public void RestoreSills(){var grid=hull.Flood;foreach(var e in edges){if(e.Right)grid.RightSill[e.Index]=e.Previous;else grid.ForwardSill[e.Index]=e.Previous;}}
 public void ApplySills(){var grid=hull.Flood;foreach(var e in edges){
  e.Previous=e.Right?grid.RightSill[e.Index]:grid.ForwardSill[e.Index];
  if(e.Panel.Missing||e.Panel.Detached)continue;
  float sill=Mathf.Max(e.Previous,hull.Root.TransformPoint(e.Top).y);
  if(e.Right)grid.RightSill[e.Index]=sill;else grid.ForwardSill[e.Index]=sill;
 }}
 public bool ContainsCell(int index){return index>=0&&index<inside.Length&&inside[index];}
}

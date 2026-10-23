using UnityEngine;
using System.Collections.Generic;
// Voxelized stock; revision cache avoids scanning its cells every simulation tick.
public sealed class NavalPlankSupply : MonoBehaviour
{
 private NavalHull hull;
 private NavalCargoCrate cargo;
 public void InitializeCargo(NavalHull owner,NavalCargoCrate crate){hull=owner;cargo=crate;owner.PlankSupplies.Add(this);if(owner.PlankSupply==null)owner.PlankSupply=this;}
 public bool TakeBundle(){return cargo==null?Available:cargo.Count>=NavalPlankBundle.Capacity&&cargo.Take(NavalPlankBundle.Capacity)==NavalPlankBundle.Capacity;}
 private float floor;
 private readonly List<NavalHullVoxel> cells=new List<NavalHullVoxel>();
 private int revision=-1;
 private bool available;
 public Vector3 PickupPost {get{return cargo!=null?cargo.Pickup:new Vector3(0,floor,NavalPlankSupplyLayout.PickupZ(hull.WorldScale));}}
 public bool Available {get{
  if(cargo!=null)return cargo.Count>0;
  if(revision!=hull.Revision){revision=hull.Revision;available=cells.Count>0;
   foreach(var cell in cells)if(cell==null||cell.Missing||cell.Detached){available=false;break;}}
  return available;
 }}
 public void Initialize(NavalHull owner,Material wood,Material trim,float deckFloor=float.NaN)
 {
  hull=owner;floor=float.IsNaN(deckFloor)?owner.Floor:deckFloor;hull.PlankSupplies.Add(this);if(hull.PlankSupply==null)hull.PlankSupply=this;int first=hull.Voxels.Count;float s=hull.WorldScale;
  Vector3 origin=new Vector3(0,floor,NavalPlankSupplyLayout.Z);
  Box(origin,new Vector3(0,.10f,0),new Vector3(1.4f,.20f,1.4f),wood,s);
  // Open square crate, pale corner posts and stacked planks distinguish it from the tall parts chest.
  for(int side=-1;side<=1;side+=2){
   Box(origin,new Vector3(side*.64f,.60f,0),new Vector3(.12f,.9f,1.4f),wood,s);
   Box(origin,new Vector3(0,.60f,side*.64f),new Vector3(1.4f,.9f,.12f),wood,s);
   for(int end=-1;end<=1;end+=2)Box(origin,new Vector3(side*.64f,.60f,end*.64f),new Vector3(.16f,.98f,.16f),trim,s);
  }
  for(int layer=0;layer<4;layer++)for(int row=-1;row<=1;row++)
   Box(origin,new Vector3(row*.32f,.28f+layer*.18f,0),new Vector3(.27f,.13f,1.15f),trim,s);
  for(int i=first;i<hull.Voxels.Count;i++)cells.Add(hull.Voxels[i]);
 }
 private void Box(Vector3 origin,Vector3 point,Vector3 size,Material mat,float scale)
 {hull.AddDecorationBox(hull.Root,mat,origin+point/scale,size/scale,true);}
 private void OnDestroy(){if(hull!=null){hull.PlankSupplies.Remove(this);if(hull.PlankSupply==this)hull.PlankSupply=null;}}
}

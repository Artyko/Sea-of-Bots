using UnityEngine;
using System.Collections.Generic;
// Physical shared stock: a damaged/detached crate cannot issue new kits.
public sealed class NavalCannonSupply : MonoBehaviour
{
 private NavalHull hull;
 private NavalCargoCrate cargo;
 public void InitializeCargo(NavalHull owner,NavalCargoCrate crate,FirstPersonMover observer,NavalStations controls,bool hostile){hull=owner;cargo=crate;player=observer;stations=controls;enemy=hostile;owner.CannonSupplies.Add(this);if(owner.CannonSupply==null)owner.CannonSupply=this;}
 public bool TakeKit(){return cargo==null?Available:cargo.Take(1)==1;}
 private float floor;
 private FirstPersonMover player;
 private NavalStations stations;
 private readonly List<NavalHullVoxel> cells=new List<NavalHullVoxel>();
 private int revision=-1;
 private bool available,enemy;
 private readonly NavalCannonKit playerKit=new NavalCannonKit();
 public bool PlayerHasKit {get{foreach(var supply in hull.CannonSupplies)if(supply.playerKit.Held)return true;return false;}}
 public Vector3 PickupPost {get{return cargo!=null?cargo.Pickup:new Vector3(0,floor,NavalCannonSupplyLayout.PickupZ(hull.WorldScale));}}
 public bool Available
 {
  get{if(cargo!=null)return cargo.Count>0;if(revision!=hull.Revision){revision=hull.Revision;available=cells.Count>0;
   foreach(var cell in cells)if(cell==null||cell.Missing||cell.Detached){available=false;break;}}
   return available;}
 }
 public void Initialize(NavalHull owner,FirstPersonMover observer,NavalStations controls,bool hostile,Material wood,Material trim,Material metal,float deckFloor=float.NaN)
 {
  hull=owner;floor=float.IsNaN(deckFloor)?owner.Floor:deckFloor;player=observer;stations=controls;enemy=hostile;hull.CannonSupplies.Add(this);if(hull.CannonSupply==null)hull.CannonSupply=this;
  float scale=hull.WorldScale;Vector3 basePoint=new Vector3(0,floor,NavalCannonSupplyLayout.Z);
  int first=hull.Voxels.Count;
  float width=NavalCannonSupplyLayout.Width,height=NavalCannonSupplyLayout.Height,depth=NavalCannonSupplyLayout.Depth;
  hull.AddDecorationBox(hull.Root,wood,basePoint+Vector3.up*(height*.5f/scale),new Vector3(width,height,depth)/scale,true);
  for(int side=-1;side<=1;side+=2)
   hull.AddDecorationBox(hull.Root,trim,basePoint+new Vector3(side*(width*.5f-.16f),height*.5f,0)/scale,new Vector3(.24f,height+.08f,depth+.04f)/scale,true);
  hull.AddDecorationBox(hull.Root,metal,basePoint+new Vector3(0,height*.6f,-depth*.5f-.02f)/scale,new Vector3(.5f,.44f,.08f)/scale,true);
  for(int i=first;i<hull.Voxels.Count;i++)cells.Add(hull.Voxels[i]);
 }
 private bool NearPlayer()
 {
  // Reject inaccessible/enemy/far-away supplies before searching their neighbours.
  if(enemy||player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||stations.Operating||
   NavalTestWorld.Active==null||!NavalTestWorld.Active.SimulationActive)return false;
  Vector3 playerPoint=player.transform.position,post=hull.Root.TransformPoint(PickupPost);
  if((playerPoint-post-Vector3.up*.8f).sqrMagnitude>=2.5f*2.5f)return false;
  if(cargo!=null){float own=(playerPoint-post).sqrMagnitude;
   foreach(var other in hull.CannonSupplies)if(other!=this&&other.Available&&(playerPoint-hull.Root.TransformPoint(other.PickupPost)).sqrMagnitude<own)return false;}
  return true;
 }
 private void Update(){if(player==null||!player.IsAlive())playerKit.Drop();else if(Input.GetKeyDown(KeyCode.E)&&NearPlayer()&&!PlayerHasKit)playerKit.Take(TakeKit());}
 public bool DeliverPlayerKit(){foreach(var supply in hull.CannonSupplies)if(supply.playerKit.Deliver())return true;return false;}
 public void ResetStock(){revision=-1;playerKit.Drop();}
 private void OnGUI()
 {
  if(Event.current.type!=EventType.Repaint||!NearPlayer())return;
  string text=!Available?"Ящик запчастей повреждён — новые детали недоступны":PlayerHasKit?"Комплект запчастей взят — подойди к пушке и удерживай R":"E — взять запчасти для ремонта пушки";
  GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,560),Screen.height-245,NavalHudLayout.ContextWidth(Screen.width,Screen.height,560),28),text);
 }
 private void OnDestroy(){if(hull!=null){hull.CannonSupplies.Remove(this);if(hull.CannonSupply==this)hull.CannonSupply=null;}}
}

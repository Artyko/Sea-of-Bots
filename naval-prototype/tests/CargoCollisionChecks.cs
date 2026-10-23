using System;
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector3 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 up {get{return new Vector3(0,1,0);}}public static Vector3 zero {get{return new Vector3();}}
  public float sqrMagnitude {get{return x*x+y*y+z*z;}}
  public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
  public static Vector3 operator *(Vector3 a,float f){return new Vector3(a.x*f,a.y*f,a.z*f);}public static Vector3 operator /(Vector3 a,float f){return a*(1/f);}
  public static Vector3 Scale(Vector3 a,Vector3 b){return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);}
 }
 public static class Mathf {public static float Clamp(float v,float a,float b){return Math.Max(a,Math.Min(b,v));}}
 public class Transform {
  public Vector3 localPosition,localScale=new Vector3(1,1,1);public Transform parent;
  public void SetParent(Transform p,bool world){parent=p;}
  public Vector3 TransformPoint(Vector3 p){return localPosition+Vector3.Scale(p,localScale);}public Vector3 InverseTransformPoint(Vector3 p){p=p-localPosition;return new Vector3(p.x/localScale.x,p.y/localScale.y,p.z/localScale.z);}
 }
 public class GameObject {
  public static List<GameObject> All=new List<GameObject>();public int layer;public Transform transform=new Transform();public List<MonoBehaviour> Components=new List<MonoBehaviour>();
  public GameObject(string name){All.Add(this);}public T AddComponent<T>() where T:MonoBehaviour,new(){var c=new T();c.gameObject=this;Components.Add(c);return c;}
 }
 public class MonoBehaviour {public GameObject gameObject;public Transform transform {get{return gameObject.transform;}}protected static void Destroy(GameObject g){GameObject.All.Remove(g);}}
 public class BoxCollider:MonoBehaviour {public bool enabled;public object sharedMaterial;public float contactOffset;public Vector3 center,size;}
 public struct Bounds {public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}public void Encapsulate(Bounds b){Vector3 aMin=center-size*.5f,aMax=center+size*.5f,bMin=b.center-b.size*.5f,bMax=b.center+b.size*.5f;Vector3 lo=new Vector3(Math.Min(aMin.x,bMin.x),Math.Min(aMin.y,bMin.y),Math.Min(aMin.z,bMin.z)),hi=new Vector3(Math.Max(aMax.x,bMax.x),Math.Max(aMax.y,bMax.y),Math.Max(aMax.z,bMax.z));center=(lo+hi)*.5f;size=hi-lo;}}
}
public class NavalHull {public UnityEngine.Transform Root;public float WorldScale;public FakePhysics PhysicsBody=new FakePhysics();}
public class FakePhysics {public void MarkDirty(NavalHullVoxel c){}}
public class NavalHullVoxel {
 public bool Missing,Detached,ColliderBatched;public NavalCargoCollision CargoCollision;public UnityEngine.Transform transform;public UnityEngine.BoxCollider PhysicsCollider;
 public void SetMissing(bool value){Missing=value;CargoCollision.SourceChanged();PhysicsCollider.enabled=!value&&!ColliderBatched;}
}
public class NavalCargoCrate {public UnityEngine.Vector3 Point;public bool UpdatingContents;public int DamageNotifications;public void NotifyDamage(){if(!UpdatingContents)DamageNotifications++;}}
public static class PhysicsManager {public static int GetEnvironmentLayer(){return 0;}}
public static class NavalPhysicsQueries {public static void Invalidate(){}}
class CargoCollisionChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static NavalHullVoxel Cell(NavalHull h,float y){var g=new UnityEngine.GameObject("cell");g.transform.parent=h.Root;g.transform.localPosition=new UnityEngine.Vector3(0,y,0);var c=g.AddComponent<UnityEngine.BoxCollider>();c.enabled=true;c.size=new UnityEngine.Vector3(1,1,1);return new NavalHullVoxel{transform=g.transform,PhysicsCollider=c};}
 static List<NavalCargoProxy> Proxies(){var result=new List<NavalCargoProxy>();foreach(var g in UnityEngine.GameObject.All)foreach(var c in g.Components)if(c is NavalCargoProxy)result.Add((NavalCargoProxy)c);return result;}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){foreach(float scale in new float[]{2,3,4}){
 UnityEngine.GameObject.All.Clear();var root=new UnityEngine.GameObject("ship");var h=new NavalHull{Root=root.transform,WorldScale=scale};var stock=new NavalCargoCrate();
 var shell=new List<NavalHullVoxel>{Cell(h,0),Cell(h,.5f)};var items=new List<NavalHullVoxel>{Cell(h,1),Cell(h,2)};var collision=root.AddComponent<NavalCargoCollision>();collision.Initialize(h,stock,shell,items);
 Check(Proxies().Count==6,"Intact crate has six proxies");foreach(var c in shell)Check(c.ColliderBatched&&!c.PhysicsCollider.enabled,"Shell source colliders disabled");foreach(var c in items)Check(c.ColliderBatched&&!c.PhysicsCollider.enabled,"Content source colliders disabled");
 Check(Proxies()[5].Resolve(new UnityEngine.Vector3(0,2,0))==items[1],"Proxy hit resolves a real visible content voxel");
 float height=Proxies()[5].Shape.size.y;stock.UpdatingContents=true;items[1].SetMissing(true);stock.UpdatingContents=false;collision.RefreshShapes();
 Check(Proxies()[5].Shape.size.y<height&&items[0].ColliderBatched&&!items[0].PhysicsCollider.enabled,"Inventory depletion shrinks proxy without exact-collider fallback");
 Check(stock.DamageNotifications==0,"Internal consumption does not dirty external damage state");
 shell[0].SetMissing(true);Check(stock.DamageNotifications>0,"External damage notifies crate immediately");foreach(var p in Proxies())Check(!p.Shape.enabled,"Damage immediately removes all obsolete solid proxies");
 Check(!shell[1].ColliderBatched&&shell[1].PhysicsCollider.enabled&&!shell[0].PhysicsCollider.enabled,"Damage restores exact remaining shell and preserves hole");
 Check(items[0].PhysicsCollider.enabled&&!items[1].PhysicsCollider.enabled,"Damage fallback retains hidden inventory state");
 stock.UpdatingContents=true;shell[0].SetMissing(false);items[1].SetMissing(false);stock.UpdatingContents=false;collision.TryCompact();Check(Proxies().Count==6,"Explicit reset reuses proxy objects");
 shell[1].Detached=true;collision.SourceChanged();foreach(var p in Proxies())Check(!p.Shape.enabled,"Detachment immediately removes original proxy");
 shell[1].Detached=false;collision.TryCompact();stock.UpdatingContents=true;foreach(var c in shell)c.SetMissing(true);foreach(var c in items)c.SetMissing(true);stock.UpdatingContents=false;collision.RefreshShapes();foreach(var p in Proxies())Check(!p.Shape.enabled,"Vanishing empty board crate leaves no invisible obstacle");
 }
 Console.WriteLine("PASS: actual CargoCollision with mocked Unity shapes at all scales: six proxies, resource shrink, hit-to-voxel resolution, immediate damage/detachment fallback, reset reuse and empty-crate removal. Not native PhysX.");}
}

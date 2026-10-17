// Compile with the actual NavalHullColliders.cs and NavalCore.dll, without Unity DLLs.
// This validates lifecycle/control flow, not native PhysX or CharacterController behaviour.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine
{
    public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int order){}}
    public class Object
    {
        public int GetInstanceID(){return GetHashCode();}
        public static void Destroy(Object value){}
    }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float a,float b,float c){x=a;y=b;z=c;}
        public static Vector3 Scale(Vector3 a,Vector3 b){return new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);}
        public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
        public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
        public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
    }
    public struct Quaternion {public float x,y,z,w;}
    public static class Mathf {public static float Abs(float value){return Math.Abs(value);}public static int FloorToInt(float value){return (int)Math.Floor(value);}}
    public class Component:Object
    {
        public GameObject gameObject;public Transform transform {get{return gameObject.transform;}}
        public T GetComponent<T>() where T:class{return gameObject.GetComponent<T>();}
    }
    public class MonoBehaviour:Component {}
    public class Transform:Component
    {
        public Transform parent;public Vector3 localPosition,localScale=new Vector3(1,1,1);public Quaternion localRotation=new Quaternion{w=1};
        public void SetParent(Transform value,bool keepWorld){parent=value;}
        public Vector3 InverseTransformPoint(Vector3 point){return point-localPosition;}
    }
    public class GameObject:Object
    {
        public static readonly List<GameObject> All=new List<GameObject>();
        public Transform transform;public int layer;
        private readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
        public GameObject(string name){transform=new Transform{gameObject=this};components.Add(typeof(Transform),transform);All.Add(this);}
        public T AddComponent<T>() where T:Component,new(){T component=new T{gameObject=this};components.Add(typeof(T),component);return component;}
        public T GetComponent<T>() where T:class
        {foreach(Component component in components.Values)if(component is T)return component as T;return null;}
    }
    public class PhysicsMaterial:Object {}
    public class Collider:Component {public bool enabled=true,isTrigger;public PhysicsMaterial sharedMaterial;public float contactOffset=.01f;}
    public class BoxCollider:Collider {public Vector3 center,size;}
}
public enum NavalPerfArea {ColliderChunks}
public static class NavalPerformance
{
    private struct Scope:IDisposable {public void Dispose(){}}
    public static IDisposable Measure(NavalPerfArea value){return new Scope();}
    public static void SyncTransforms(){NavalHullColliders.FlushAll();}
}
public class NavalShipPhysics {public void MarkDirty(NavalHullVoxel cell){}}
public class NavalHull
{public readonly List<NavalHullVoxel> Voxels=new List<NavalHullVoxel>();public NavalShipPhysics PhysicsBody;public Transform Root;}
public class NavalHullVoxel:MonoBehaviour
{
    public bool ManagedRig,MovableEquipment,RepairableEquipment,Missing,Detached,ColliderBatched;
    public NavalHullColliders ColliderBatchOwner;public int ColliderChunk=-1;
    public Collider PhysicsCollider {get{return GetComponent<Collider>();}}
}
class ColliderLifecycleChecks
{
    static void Check(bool value,string name){if(!value)throw new Exception(name);}
    static List<NavalMergedCollider> EnabledProxies()
    {
        List<NavalMergedCollider> result=new List<NavalMergedCollider>();
        foreach(GameObject obj in GameObject.All){NavalMergedCollider proxy=obj.GetComponent<NavalMergedCollider>();if(proxy!=null&&proxy.Shape.enabled)result.Add(proxy);}
        return result;
    }
    static void Main()
    {
        GameObject ship=new GameObject("ship");NavalHull hull=new NavalHull{Root=ship.transform,PhysicsBody=new NavalShipPhysics()};
        for(int x=0;x<4;x++)for(int y=0;y<4;y++)for(int z=0;z<4;z++)
        {
            GameObject obj=new GameObject("voxel");obj.transform.SetParent(ship.transform,false);
            obj.transform.localPosition=new Vector3((x+.5f)*.25f,(y+.5f)*.25f,(z+.5f)*.25f);obj.transform.localScale=new Vector3(.25f,.25f,.25f);
            obj.AddComponent<BoxCollider>().size=new Vector3(1,1,1);hull.Voxels.Add(obj.AddComponent<NavalHullVoxel>());
        }
        NavalHullColliders manager=ship.AddComponent<NavalHullColliders>();manager.Initialize(hull);NavalHullColliders.FlushAll();
        Check(manager.BatchedCount==64&&manager.ActiveCount==1,"64 original boxes merge into one active proxy");
        foreach(NavalHullVoxel cell in hull.Voxels)Check(cell.ColliderBatched&&!cell.PhysicsCollider.enabled,"no duplicate source colliders");
        NavalHullVoxel damaged=hull.Voxels[0];NavalMergedCollider first=EnabledProxies()[0];
        Check(NavalVoxelHit.Resolve(first.Shape,new Vector3(.125f,.125f,.125f))==damaged,"merged hit returns actual voxel");
        damaged.Missing=true;manager.MarkDirty(damaged);damaged.PhysicsCollider.enabled=false;
        Check(EnabledProxies().Count==0,"old whole proxy disabled synchronously before any flush");
        foreach(NavalHullVoxel cell in hull.Voxels)Check(cell.PhysicsCollider.enabled==!cell.Missing&&!cell.ColliderBatched,"fallback uses exact originals immediately");
        Check(NavalVoxelHit.Resolve(first.Shape,new Vector3(.125f,.125f,.125f))==null,"stale proxy cannot resolve destroyed cell");
        NavalHullColliders.FlushAll();
        foreach(NavalMergedCollider proxy in EnabledProxies())Check(!proxy.Cells.Contains(damaged),"hole absent from every merged shape");
        // Simulate repair with the same notification order as SetMissing.
        damaged.Missing=false;manager.MarkDirty(damaged);damaged.PhysicsCollider.enabled=true;NavalHullColliders.FlushAll();
        Check(manager.ActiveCount==1&&manager.BatchedCount==64,"repair restores the intact merge");
        NavalHullVoxel loose=hull.Voxels[1];loose.Detached=true;loose.transform.SetParent(new GameObject("debris").transform,true);manager.MarkDirty(loose);
        Check(loose.PhysicsCollider.enabled&&!loose.ColliderBatched,"detached cell immediately recovers individual collision");
        NavalHullColliders.FlushAll();
        foreach(NavalMergedCollider proxy in EnabledProxies())Check(!proxy.Cells.Contains(loose),"no ghost cell left in ship after detachment");
        loose.transform.SetParent(ship.transform,false);loose.Detached=false;manager.MarkDirty(loose);NavalHullColliders.FlushAll();
        Check(manager.ActiveCount==1&&manager.BatchedCount==64,"returning debris restores merged geometry");
        NavalHullVoxel changed=hull.Voxels[0];BoxCollider changedShape=(BoxCollider)changed.PhysicsCollider;
        changedShape.sharedMaterial=new PhysicsMaterial();manager.MarkDirty(changed);NavalHullColliders.FlushAll();
        Check(!changed.ColliderBatched&&changedShape.enabled,"changed physical material falls back to the original collider");
        foreach(NavalMergedCollider proxy in EnabledProxies())Check(proxy.Shape.sharedMaterial==null&&!proxy.Cells.Contains(changed),"remaining cells retain captured material even if first source changes");
        changedShape.sharedMaterial=null;manager.MarkDirty(changed);NavalHullColliders.FlushAll();
        Check(manager.ActiveCount==1&&manager.BatchedCount==64,"material restoration permits safe merging again");
        Vector3 originalPosition=changed.transform.localPosition;changed.transform.localPosition=originalPosition+new Vector3(2,0,0);manager.MarkDirty(changed);NavalHullColliders.FlushAll();
        Check(!changed.ColliderBatched&&changedShape.enabled,"moved geometry uses its actual original shape, not stale captured bounds");
        changed.transform.localPosition=originalPosition;manager.MarkDirty(changed);NavalHullColliders.FlushAll();
        Check(manager.ActiveCount==1&&manager.BatchedCount==64,"geometry restoration returns to merged shapes");
        foreach(NavalHullVoxel cell in hull.Voxels){cell.Missing=true;manager.MarkDirty(cell);cell.PhysicsCollider.enabled=false;}
        NavalHullColliders.FlushAll();Check(manager.ActiveCount==0&&manager.BatchedCount==0&&EnabledProxies().Count==0,"complete destruction leaves no physical proxy");
        foreach(NavalHullVoxel cell in hull.Voxels){cell.Missing=false;manager.MarkDirty(cell);cell.PhysicsCollider.enabled=true;}
        NavalHullColliders.FlushAll();Check(manager.ActiveCount==1&&manager.BatchedCount==64,"full restoration reuses proxy pool without duplicates");
        Console.WriteLine("PASS: actual collider manager in headless shim: 64->1, synchronous hole fallback, accurate hit, patch, debris handoff, destruction and full restoration. No PhysX test.");
    }
}

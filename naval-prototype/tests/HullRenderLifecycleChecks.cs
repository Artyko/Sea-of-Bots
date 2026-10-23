// Executes the actual hull renderer with a minimal Unity shim; no GPU/PhysX claims.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine.Rendering {public enum IndexFormat {UInt32}}
namespace UnityEngine
{
 public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int value){}}
 public class Object {public static void Destroy(Object value){}}
 public struct Vector2 {public float x,y;}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public Vector3 normalized {get{float m=(float)Math.Sqrt(x*x+y*y+z*z);return m==0?this:new Vector3(x/m,y/m,z/m);}}
 }
 public struct Vector3Int {public int x,y,z;public Vector3Int(int a,int b,int c){x=a;y=b;z=c;}}
 public struct Quaternion {}
 public struct Matrix4x4
 {
  Vector3 position,scale;bool inverseNormal;
  public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s){return new Matrix4x4{position=p,scale=s};}
  public Matrix4x4 inverse {get{var m=this;m.inverseNormal=true;return m;}}
  public Matrix4x4 transpose {get{return this;}}
  public Vector3 MultiplyPoint3x4(Vector3 p){return new Vector3(position.x+p.x*scale.x,position.y+p.y*scale.y,position.z+p.z*scale.z);}
  public Vector3 MultiplyVector(Vector3 n){return inverseNormal?new Vector3(n.x/scale.x,n.y/scale.y,n.z/scale.z):n;}
 }
 public static class Mathf {public static int FloorToInt(float x){return (int)Math.Floor(x);}}
 public static class Debug {public static void Log(object value){}}
 public class Component:Object {public GameObject gameObject;public Transform transform {get{return gameObject.transform;}}public T GetComponent<T>() where T:class{return gameObject.GetComponent<T>();}}
 public class MonoBehaviour:Component {}
 public class Transform:Component
 {public Transform parent;public Vector3 localPosition,localScale=new Vector3(1,1,1);public Quaternion localRotation;public void SetParent(Transform p,bool keep){parent=p;}}
 public class GameObject:Object
 {
  public readonly string name;public bool activeSelf=true;public Transform transform;
  readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();
  public static readonly List<GameObject> All=new List<GameObject>();
  public GameObject(string value){name=value;transform=new Transform{gameObject=this};components.Add(typeof(Transform),transform);All.Add(this);}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components.Add(typeof(T),c);return c;}
  public T GetComponent<T>() where T:class {foreach(var c in components.Values)if(c is T)return c as T;return null;}
  public void SetActive(bool value){activeSelf=value;}
 }
 public class Material:Object {}
 public class Renderer:Component {public bool enabled=true;public Material sharedMaterial;}
 public class MeshRenderer:Renderer {public readonly List<Material> Materials=new List<Material>();public void SetSharedMaterials(List<Material> values){Materials.Clear();Materials.AddRange(values);}}
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class Mesh:Object
 {
  public UnityEngine.Rendering.IndexFormat indexFormat;public Vector3[] vertices,normals;public Vector2[] uv;public int[] triangles;
  public int subMeshCount {get{return sub.Count;}set{while(sub.Count>value)sub.RemoveAt(sub.Count-1);while(sub.Count<value)sub.Add(new List<int>());}}
  public readonly List<List<int>> sub=new List<List<int>>();
  public void SetTriangles(List<int> values,int index,bool bounds){sub[index].Clear();sub[index].AddRange(values);}
  public void RecalculateBounds(){}
 }
}
public enum NavalPerfArea {Chunks}
public static class NavalPerformance {public struct Scope:IDisposable {public void Dispose(){}}public static Scope Measure(NavalPerfArea area){return new Scope();}}
public class NavalHull {public readonly List<NavalHullVoxel> Voxels=new List<NavalHullVoxel>();}
public class NavalHullVoxel:MonoBehaviour {public bool ManagedRig,Missing,Detached,Batched;public NavalVoxelBatch BatchOwner;public int RenderChunk;}
class HullRenderLifecycleChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Flush(NavalVoxelBatch batch){typeof(NavalVoxelBatch).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(batch,null);}
 static int Indices(Mesh mesh){int total=0;foreach(var sub in mesh.sub)total+=sub.Count;return total;}
 static void Main()
 {
  var root=new GameObject("ship");var hull=new NavalHull();var wood=new Material();var scorch=new Material();
  var source=new Mesh{vertices=new[]{new Vector3(0,0,0),new Vector3(1,0,0),new Vector3(0,0,1)},normals=new[]{new Vector3(0,1,0),new Vector3(0,1,0),new Vector3(0,1,0)},uv=new Vector2[3],triangles=new[]{0,1,2}};
  for(int i=0;i<4;i++)
  {
   var obj=new GameObject("cell");obj.transform.SetParent(root.transform,false);obj.transform.localPosition=new Vector3(i*.25f,0,0);obj.transform.localScale=new Vector3(.25f,.1f,.25f);
   obj.AddComponent<MeshFilter>().sharedMesh=source;obj.AddComponent<MeshRenderer>().sharedMaterial=wood;hull.Voxels.Add(obj.AddComponent<NavalHullVoxel>());
  }
  var batch=root.AddComponent<NavalVoxelBatch>();batch.Initialize(hull);
  GameObject view=null;foreach(var obj in GameObject.All)if(obj.name=="HullRenderChunk")view=obj;
  var mesh=view.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var renderer=view.GetComponent<MeshRenderer>();
  Check(vertices.Length==12&&Indices(mesh)==12&&renderer.Materials.Count==1,"intact hull geometry/material");
  Check(Math.Abs(vertices[4].x-.5f)<.00001&&Math.Abs(mesh.normals[4].y-1)<.00001,"local transform and normalized normals");
  for(int i=0;i<200;i++)
  {
   hull.Voxels[0].Missing=i%2==0;hull.Voxels[1].GetComponent<Renderer>().sharedMaterial=i%3==0?scorch:wood;
   batch.MarkDirty(hull.Voxels[0]);batch.MarkDirty(hull.Voxels[1]);Flush(batch);
   Check(object.ReferenceEquals(vertices,mesh.vertices),"vertex buffer reused after damage/scorch");
   Check(Indices(mesh)==(i%2==0?9:12),"exact triangle removal/restoration");
  }
  var loose=hull.Voxels[2];loose.Detached=true;loose.transform.SetParent(new GameObject("debris").transform,true);batch.MarkDirty(loose);Flush(batch);
  Check(loose.GetComponent<Renderer>().enabled&&!loose.Batched&&Indices(mesh)==9,"detached geometry handed to original renderer");
  foreach(var cell in hull.Voxels){cell.Missing=true;batch.MarkDirty(cell);}Flush(batch);Check(!view.activeSelf&&Indices(mesh)==0,"fully destroyed chunk hidden");
  foreach(var cell in hull.Voxels){cell.Missing=false;cell.Detached=false;cell.transform.SetParent(root.transform,false);batch.MarkDirty(cell);}Flush(batch);
  Check(view.activeSelf&&Indices(mesh)==12&&object.ReferenceEquals(vertices,mesh.vertices),"full restoration reuses same mesh/buffer");
  Console.WriteLine("PASS: actual persistent hull renderer in headless shim: transforms, normals, 200 damage/scorch updates, debris handoff, full destruction/restoration and buffer reuse. No GPU test.");
 }
}

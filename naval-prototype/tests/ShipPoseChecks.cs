// Headless lifecycle test of actual NavalShipPhysics.SetPose, not a native PhysX test.
using System;
using System.Collections.Generic;
using UnityEngine;
namespace UnityEngine {
 public class MonoBehaviour {public GameObject gameObject=new GameObject();public Transform transform {get{return gameObject.transform;}}}
 public class GameObject {public Transform transform=new Transform();public T AddComponent<T>() where T:new(){var item=new T();var body=item as Rigidbody;if(body!=null)body.Owner=transform;return item;}}
 public class Transform {public Vector3 position;public Quaternion rotation;public int TransformWrites;public void SetPositionAndRotation(Vector3 p,Quaternion q){TransformWrites++;position=p;rotation=q;}}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero {get{return new Vector3();}} public static Vector3 one {get{return new Vector3(1,1,1);}}}
 public struct Quaternion {public float x,y,z,w;public Quaternion(float a,float b,float c,float d){x=a;y=b;z=c;w=d;} public static Quaternion identity {get{return new Quaternion(0,0,0,1);}}}
 public enum RigidbodyInterpolation {None}public enum CollisionDetectionMode {Discrete}
 public class Rigidbody {public Transform Owner;public int PositionWrites,RotationWrites,PublishCalls;private Vector3 p;private Quaternion q; public Vector3 position {get{return p;}set{PositionWrites++;p=value;}}public Quaternion rotation {get{return q;}set{RotationWrites++;q=value;}}public void PublishTransform(){PublishCalls++;Owner.position=p;Owner.rotation=q;}public bool isKinematic,useGravity,automaticCenterOfMass,automaticInertiaTensor;public Vector3 centerOfMass,inertiaTensor;public Quaternion inertiaTensorRotation;public RigidbodyInterpolation interpolation;public CollisionDetectionMode collisionDetectionMode;}
 public class Collider {public Rigidbody attachedRigidbody;public bool enabled;}
 public static class Time {public static float unscaledTime;}
 public static class Debug {public static void Log(string s){}public static void LogWarning(string s){}}
}
public static class NavalPhysicsQueries {public static void Invalidate(){}}
public enum NavalPerfArea {HullPose}
public static class NavalPerformance {public struct Scope:IDisposable {public void Dispose(){}}public static Scope Measure(NavalPerfArea a){return new Scope();}public static void SyncTransforms(){}}
public class NavalHullColliders {public int BatchedCount,ActiveCount;public void Initialize(NavalHull h){}}
public class NavalHull {public List<NavalHullVoxel> Voxels=new List<NavalHullVoxel>();public NavalShipDefinition Definition=NavalShipDefinition.Brigantine;public int Revision;}
public class NavalHullVoxel {public Collider PhysicsCollider;public bool Missing,ColliderBatched,Detached;}
class ShipPoseChecks {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){foreach(var ship in NavalShipDefinition.Choices){
 var physics=new NavalShipPhysics();physics.Initialize(new NavalHull{Definition=ship});
 Check(physics.Body.isKinematic&&!physics.Body.useGravity,"Kinematic authority retained");
 var rotation=new Quaternion(0,.3f,0,.95f);var position=new Vector3(120,39,-50);
 physics.SetPose(position,rotation);
 Check(physics.Body.position.Equals(position)&&physics.transform.position.Equals(position),"Position immediately published");
 Check(physics.Body.rotation.Equals(rotation)&&physics.transform.rotation.Equals(rotation),"Rotation immediately published");
 Check(physics.transform.TransformWrites==0,"No compound Transform dirty path");
 Check(physics.Body.PublishCalls==1,"Deferred transform explicitly published after pose setters");
 int p=physics.Body.PositionWrites,q=physics.Body.RotationWrites;
 physics.SetPose(position,rotation);Check(p==physics.Body.PositionWrites&&q==physics.Body.RotationWrites,"Exact unchanged pose has no native writes");Check(physics.Body.PublishCalls==1,"Unchanged pose does not republish");
 position.z-=3;physics.SetPose(position,rotation);Check(physics.Body.PositionWrites==p+1&&physics.Body.RotationWrites==q,"Translation does not republish unchanged rotation");
 Check(physics.transform.position.Equals(position)&&physics.Body.PublishCalls==2,"Rider sees new pose before physics step");
 }
 Console.WriteLine("PASS: actual ship pose implementation publishes immediate kinematic position/rotation, avoids compound Transform writes and unchanged actor writes on all profiles. Native PhysX not simulated.");}
}

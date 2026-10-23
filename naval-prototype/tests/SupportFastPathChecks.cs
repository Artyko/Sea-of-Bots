using System;
namespace UnityEngine {
 public struct Vector3 { public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
 public class Transform { public Transform Parent; public bool IsChildOf(Transform p){return this==p||Parent==p;} }
 public class Collider { public Transform transform; }
 public struct RaycastHit {public Collider collider;public Vector3 normal;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Time {public static int frameCount=1;}
 public static class Physics {
  public static int NearestCalls,FullCalls;public static bool Hit;public static RaycastHit Nearest;public static RaycastHit[] All=new RaycastHit[0];
  public static bool SphereCast(Vector3 a,float r,Vector3 d,out RaycastHit h,float l,int m,QueryTriggerInteraction q){NearestCalls++;h=Nearest;return Hit;}
  public static bool Raycast(Vector3 a,Vector3 d,out RaycastHit h,float l,int m,QueryTriggerInteraction q){NearestCalls++;h=Nearest;return Hit;}
  public static int SphereCastNonAlloc(Vector3 a,float r,Vector3 d,RaycastHit[] b,float l,int m,QueryTriggerInteraction q){return Fill(b);}
  public static int RaycastNonAlloc(Vector3 a,Vector3 d,RaycastHit[] b,float l,int m,QueryTriggerInteraction q){return Fill(b);}
  static int Fill(RaycastHit[] b){FullCalls++;int n=Math.Min(b.Length,All.Length);Array.Copy(All,b,n);return n;}
 }
}
public static class PhysicsManager {public static int GetEnvironmentLayerMask(){return -1;}}
public enum NavalPerfArea {SupportQuery}
public static class NavalPerformance {public struct Scope:IDisposable {public void Dispose(){}} public static Scope Measure(NavalPerfArea a){return new Scope();}}
class SupportFastPathChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){try{Run();}catch(Exception e){Console.WriteLine(e);Environment.ExitCode=1;}}
 static void Run(){
 var ship=new UnityEngine.Transform();var foreign=new UnityEngine.Transform();var origin=new UnityEngine.Vector3(1,2,3);var direction=new UnityEngine.Vector3(0,-1,0);
 foreach(float radius in new float[]{0,.12f}){
  UnityEngine.Physics.Hit=false;NavalPhysicsQueries.Invalidate();int n=UnityEngine.Physics.NearestCalls,f=UnityEngine.Physics.FullCalls;
  Check(!NavalPhysicsQueries.HasSupport(origin,direction,2,ship,radius),"Empty query must reject support");
  Check(UnityEngine.Physics.NearestCalls==n+1&&UnityEngine.Physics.FullCalls==f,"Miss executes one query only");
  Check(!NavalPhysicsQueries.HasSupport(origin,direction,2,ship,radius)&&UnityEngine.Physics.NearestCalls==n+1,"Identical current-generation miss memo");
  UnityEngine.Physics.Hit=true;UnityEngine.Physics.Nearest=new UnityEngine.RaycastHit{collider=new UnityEngine.Collider{transform=ship},normal=new UnityEngine.Vector3(0,1,0)};
  NavalPhysicsQueries.Invalidate();Check(NavalPhysicsQueries.HasSupport(origin,direction,2,ship,radius),"Pose/damage invalidation must see new support");
  UnityEngine.Physics.All=new UnityEngine.RaycastHit[]{UnityEngine.Physics.Nearest};UnityEngine.Physics.Nearest=new UnityEngine.RaycastHit{collider=new UnityEngine.Collider{transform=foreign},normal=new UnityEngine.Vector3(0,1,0)};
  NavalPhysicsQueries.Invalidate();f=UnityEngine.Physics.FullCalls;Check(NavalPhysicsQueries.HasSupport(origin,direction,2,ship,radius)&&UnityEngine.Physics.FullCalls==f+1,"Foreign nearest preserves full fallback and own deck behind it");
  UnityEngine.Physics.All=new UnityEngine.RaycastHit[]{UnityEngine.Physics.Nearest};UnityEngine.Time.frameCount++;
  Check(!NavalPhysicsQueries.HasSupport(origin,direction,2,ship,radius),"Foreign deck alone is not own support");
 }
 Console.WriteLine("PASS: actual HasSupport method with mocked casts: ray/sphere misses, exact memo, invalidation, foreign-hit fallback and frame rollover. Not native PhysX.");
 }
}

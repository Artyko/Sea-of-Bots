using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int order){}}
 public class MonoBehaviour {public Transform transform=new Transform();}
 public struct Vector3 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 up {get{return new Vector3(0,1,0);}}
  public static Vector3 operator -(Vector3 a,Vector3 b){return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);}
  public static Vector3 operator *(Vector3 a,float b){return new Vector3(a.x*b,a.y*b,a.z*b);}
  public static float Distance(Vector3 a,Vector3 b){var d=a-b;return (float)Math.Sqrt(d.x*d.x+d.y*d.y+d.z*d.z);}
 }
 public class Transform {public Vector3 position;public Vector3 InverseTransformPoint(Vector3 point){return point;}public T GetComponent<T>() where T:class{return null;}}
 public class CharacterController {}
 public static class Time {public static float unscaledTime;}
 public static class Physics {
  public static readonly Dictionary<string,bool> Ignored=new Dictionary<string,bool>();
  static string Key(CharacterController a,CharacterController b){int x=a.GetHashCode(),y=b.GetHashCode();return Math.Min(x,y)+":"+Math.Max(x,y);}
  public static void IgnoreCollision(CharacterController a,CharacterController b,bool ignore){Ignored[Key(a,b)]=ignore;}
  public static bool Passing(CharacterController a,CharacterController b){bool value;return Ignored.TryGetValue(Key(a,b),out value)&&value;}
 }
}
public sealed class Character {public Transform transform=new Transform();public bool IsPlayerTeam=true;public bool IsAlive(){return true;}}
public sealed class NavalRepairSailor {public float FootOffset;public CharacterController BodyController=new CharacterController();}
public sealed class NavalHull {public Transform Root=new Transform();public NavalShipDefinition Definition;}
public sealed class NavalRamDamage {public NavalHull Hull;}
public sealed class NavalTestWorld {public static NavalTestWorld Active;public NavalHull PlayerHull;public Transform EnemyBoat;}
class CrewPassageLifecycleChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Call(NavalCrewPassage passage,string method){typeof(NavalCrewPassage).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(passage,null);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 var hull=new NavalHull{Definition=ship};var crew=new Character[]{new Character(),new Character(),new Character{IsPlayerTeam=false}};
 var workers=new NavalRepairSailor[]{new NavalRepairSailor(),new NavalRepairSailor(),new NavalRepairSailor()};
 float y=NavalWalkRoutes.SurfaceY(-1,ship.HoldFloor);
 crew[0].transform.position=new Vector3(.2f,y,-1);crew[1].transform.position=new Vector3(-.2f,y,-1);crew[2].transform.position=new Vector3(0,y,-1);
 var passage=new NavalCrewPassage();passage.Initialize(hull,crew,workers);Time.unscaledTime+=1;Call(passage,"LateUpdate");
 Check(Physics.Passing(workers[0].BodyController,workers[1].BodyController),"Meeting friendly bodies receive stair pass");
 Check(!Physics.Passing(workers[0].BodyController,workers[2].BodyController),"Opponent never receives friendly pass");
 Physics.Ignored.Clear();NavalCrewPassage.Prepare(workers[0].BodyController);
 Check(Physics.Passing(workers[0].BodyController,workers[1].BodyController),"Controller re-enable resets are repaired before Move");
 crew[0].transform.position=new Vector3(1.5f,.825f,4);crew[1].transform.position=new Vector3(1.6f,.825f,4);
 NavalCrewPassage.Prepare(workers[0].BodyController);Check(!Physics.Passing(workers[0].BodyController,workers[1].BodyController),"Leaving stair restores physical collisions");
 crew[0].transform.position=new Vector3(.2f,y,-1);crew[1].transform.position=new Vector3(-.2f,y,-1);NavalCrewPassage.Prepare(workers[0].BodyController);
 Call(passage,"OnDestroy");Check(!Physics.Passing(workers[0].BodyController,workers[1].BodyController),"Removing ship restores collision pairs");
 }
 Console.WriteLine("PASS: actual crew passage code in headless shim: meeting, enemy protection, controller-toggle reapply, exit restoration and cleanup on all profiles. No PhysX test.");
 }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine {
 public class Object {}
 public class MonoBehaviour:Object {public Transform transform=new Transform();}
 public struct Vector3 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public float magnitude {get{return (float)Math.Sqrt(x*x+y*y+z*z);}}
  public static Vector3 operator +(Vector3 a,Vector3 b){return new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
 }
 public struct Quaternion {public static Quaternion identity {get{return new Quaternion();}}}
 public class Transform:Object {public Vector3 localPosition;public Quaternion localRotation;public void SetParent(Transform parent,bool keepWorld){}}
 public class GameObject:Object {public Transform transform=new Transform();public GameObject(string name){}}
 public class Material:Object {}
}
public sealed class NavalHullVoxel {public Vector3 LocalCenter;public Transform transform=new Transform();}
public sealed class NavalHull {
 public NavalShipDefinition Definition=NavalShipDefinition.Galleon;public List<NavalHullVoxel> Voxels=new List<NavalHullVoxel>();
 public void AddDecorationBox(Transform root,Material mat,Vector3 point,Vector3 size,bool solid){Voxels.Add(new NavalHullVoxel{LocalCenter=point});}
}
public sealed class NavalRepairableMechanism {
 public bool Functional=true;
 public NavalRepairableMechanism(NavalHull hull,List<NavalHullVoxel> cells,bool pump){}
}
public sealed class NavalRepairSailor {public int Cancelled;public void CancelPumpDuty(){Cancelled++;}}
public partial class NavalBilgeSystem:MonoBehaviour {
 private NavalHull hull=new NavalHull();private bool playerPump;
 public Vector3 PumpPost {get{return new Vector3(0,hull.Definition.HoldFloor,-4.9f);}}
 public void ReleasePlayer(){playerPump=false;}
}
class PumpStationChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){var system=new NavalBilgeSystem();var ship=NavalShipDefinition.Galleon;
 var build=typeof(NavalBilgeSystem).GetMethod("BuildPumpStation",BindingFlags.Instance|BindingFlags.NonPublic);
 build.Invoke(system,new object[]{ship.HoldFloor,null,null});build.Invoke(system,new object[]{.825f,null,null});
 Check(system.PumpCount==2&&system.OpenPumpSlots==2,"Two independently claimable physical stations");
 var lower=system.PumpAt(0);var middle=system.PumpAt(1);
 Check(system.NearestPump(new Vector3(0,.825f,-4.8f),true)==middle,"Middle sailor uses middle pump");
 var first=new NavalRepairSailor();middle.Operator=first;
 Check(system.OpenPumpSlots==1&&system.NearestPump(new Vector3(0,.825f,-4.8f),true)==lower,"Another sailor reserves different pump");
 var second=new NavalRepairSailor();lower.Operator=second;
 Check(system.OpenPumpSlots==0&&system.NearestPump(new Vector3(0,.825f,-4.8f),true)==null,"Cannot double-claim pumps");
 middle.Mechanism.Functional=false;
 typeof(NavalBilgeSystem).GetMethod("StopStation",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(system,new object[]{middle});
 Check(first.Cancelled==1&&second.Cancelled==0&&lower.Operator==second,"Destroyed middle pump releases only its own operator");
 lower.Operator=null;Check(system.NearestPump(new Vector3(0,.825f,-4.8f),true)==lower,"Surviving lower pump remains usable");
 Console.WriteLine("PASS: actual station construction/selection code in headless shim, two owners, nearest deck, no duplication and independent damage fallback. Not a Unity test.");
 }
}

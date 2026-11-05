using System;
namespace UnityEngine {
 public struct Vector3 {} public struct Quaternion {} public class Collider {} public enum QueryTriggerInteraction {Ignore}
 public static class Physics {public static int Candidates,Calls;public static int OverlapBoxNonAlloc(Vector3 c,Vector3 h,Collider[] b,Quaternion r,int mask,QueryTriggerInteraction q){Calls++;return Math.Min(Candidates,b.Length);}}
}
class OverlapBufferHarness {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 public static void Main(){var buffer=new UnityEngine.Collider[64];UnityEngine.Physics.Candidates=220;int n=NavalOverlapQueries.Box(new UnityEngine.Vector3(),new UnityEngine.Vector3(),new UnityEngine.Quaternion(),-1,ref buffer);Check(n==220&&buffer.Length==256,"More than 64 self colliders must be fully returned before filtering");var retained=buffer;NavalOverlapQueries.Box(new UnityEngine.Vector3(),new UnityEngine.Vector3(),new UnityEngine.Quaternion(),-1,ref buffer);Check(Object.ReferenceEquals(retained,buffer),"Reuse grown buffer");UnityEngine.Physics.Candidates=0;Check(NavalOverlapQueries.Box(new UnityEngine.Vector3(),new UnityEngine.Vector3(),new UnityEngine.Quaternion(),-1,ref buffer)==0,"Use actual hit count");UnityEngine.Physics.Candidates=5000;Check(NavalOverlapQueries.Box(new UnityEngine.Vector3(),new UnityEngine.Vector3(),new UnityEngine.Quaternion(),-1,ref buffer)==-1&&buffer.Length==4096,"Bound pathological scene size safely");Console.WriteLine("PASS: mocked overlap buffer saturation, growth, reuse and bounded overflow (not Native PhysX).");}
}

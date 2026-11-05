using UnityEngine;
// Grow before filtering self-colliders: a full buffer is not evidence of an obstruction.
public static class NavalOverlapQueries {
 public static int Box(Vector3 centre,Vector3 half,Quaternion rotation,int mask,ref Collider[] buffer){
  while(true){int count=Physics.OverlapBoxNonAlloc(centre,half,buffer,rotation,mask,QueryTriggerInteraction.Ignore);if(count<buffer.Length)return count;if(buffer.Length>=4096)return -1;System.Array.Resize(ref buffer,buffer.Length*2);}
 }
}

using UnityEngine;
public static class NavalRayQueries {
 public static int Ray(Vector3 origin,Vector3 direction,float length,int mask,ref RaycastHit[] buffer){while(true){int n=Physics.RaycastNonAlloc(origin,direction,buffer,length,mask,QueryTriggerInteraction.Ignore);if(n<buffer.Length)return n;if(buffer.Length>=4096)return -1;System.Array.Resize(ref buffer,buffer.Length*2);}}
}

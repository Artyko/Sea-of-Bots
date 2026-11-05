using UnityEngine;
public static class NavalShellEvasionMath {
 public static bool Threat(float x,float y,float z,float vx,float vy,float vz,float radius){float v=vx*vx+vz*vz;if(v<.01f)return false;float t=-(x*vx+z*vz)/v;if(t<=0||t>12)return false;float dx=x+vx*t,dz=z+vz*t,dy=y+vy*t-4.905f*t*t;return dx*dx+dz*dz<radius*radius&&System.Math.Abs(dy)<15;}
}
public sealed class NavalShellEvasion {
 private float reviewAt,activeUntil,legUntil;private int side=1;public bool Active {get{return Time.time<activeUntil;}}private RaycastHit[] sight=new RaycastHit[32];
 public Vector3 Course(Transform own,NavalHull hull,NavalStations controls,float speed,Vector3 course){
  if(!hull.Definition.Destroyer)return course;
  if(Time.time>=reviewAt){reviewAt=Time.time+.3f;foreach(var shell in NavalCannonball.Active){
   if(shell==null||shell.InWater||shell.Source==own)continue;Vector3 delta=shell.transform.position-own.position;
   if(delta.sqrMagnitude>600*600)continue;Vector3 velocity=shell.ObservedVelocity-own.forward*speed;
   if(!NavalShellEvasionMath.Threat(delta.x,delta.y,delta.z,velocity.x,velocity.y,velocity.z,hull.Definition.HullBeam*.6f+12))continue;
   Vector3 eye=own.position+Vector3.up*6,ray=shell.transform.position-eye;int n=NavalRayQueries.Ray(eye,ray.normalized,ray.magnitude,PhysicsManager.GetEnvironmentLayerMask(),ref sight);bool visible=n>=0;for(int i=0;i<n;i++)if(sight[i].collider!=null&&!sight[i].collider.transform.IsChildOf(own)){visible=false;break;}if(!visible)continue;
   if(!Active)Debug.Log("[NavalPrototype] Destroyer zigzag under observed shell fire: ship="+own.name);activeUntil=Time.time+12;break;
  }}
  if(!Active||controls.RescueStop||controls.Torpedoes!=null&&controls.Torpedoes.SalvoInProgress)return course;
  if(Time.time>=legUntil){legUntil=Time.time+Random.Range(6f,9f);side=-side;}
  Vector3 candidate=Quaternion.Euler(0,side*18,0)*course;
  if(NavalWorldMap.Active!=null&&NavalWorldMap.Active.ShoreClearance(own.position,candidate,own,hull.Definition.HullLength+30)<hull.Definition.HullLength*.5f+12)return course;
  return candidate;
 }
}

using UnityEngine;
public partial class NavalStations {
 private Collider[] mountObstacles=new Collider[64];private float[] appliedYaw,appliedPitch;
 private bool ClearOpenGun(int gun,Quaternion rotation){
  Vector3 centre=guns[gun].TransformPoint(barrels[gun].localPosition+rotation*new Vector3(0,0,hull.Definition.GunMuzzleZ*.5f));Quaternion world=guns[gun].rotation*rotation;
  int n=NavalOverlapQueries.Box(centre,new Vector3(.5f,.5f,hull.Definition.GunMuzzleZ*hull.WorldScale*hull.Definition.GunFixtureScale*.5f+.2f),world,PhysicsManager.GetEnvironmentLayerMask(),ref mountObstacles);if(n<0)return false;
  for(int i=0;i<n;i++)if(mountObstacles[i]!=null&&!mountObstacles[i].transform.IsChildOf(guns[gun])){gunStatus[gun]="traverse blocked by "+mountObstacles[i].name+" local="+boat.InverseTransformPoint(mountObstacles[i].bounds.center);return false;}return true;
 }
 private void ApplyOpenGunAim(int gun,Quaternion rotation){if(appliedYaw==null){appliedYaw=new float[GunCount];appliedPitch=new float[GunCount];}
  if(ClearOpenGun(gun,rotation)){barrels[gun].localRotation=rotation;appliedYaw[gun]=yaw[gun];appliedPitch[gun]=pitch[gun];NavalPhysicsQueries.Invalidate();}
  else {yaw[gun]=appliedYaw[gun];pitch[gun]=appliedPitch[gun];}
 }
 private RaycastHit[] fireClearHits=new RaycastHit[64];
 private bool ClearOwnGunLine(int gun){Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0,0,hull.Definition.GunMuzzleZ));int n=NavalRayQueries.Ray(muzzle,barrels[gun].forward,hull.Definition.HullLength,PhysicsManager.GetEnvironmentLayerMask(),ref fireClearHits);if(n<0){gunStatus[gun]="muzzle query overflow";return false;}
  for(int i=0;i<n;i++)if(fireClearHits[i].collider!=null&&fireClearHits[i].collider.transform.IsChildOf(boat)&&!fireClearHits[i].collider.transform.IsChildOf(guns[gun])){gunStatus[gun]="muzzle blocked by "+fireClearHits[i].collider.name;return false;}return true;
 }
}

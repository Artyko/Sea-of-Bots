using UnityEngine;
public static class NavalShipContact
{
    // Ship poses are authoritative/kinematic; native contacts cannot stop their commanded movement.
    // Sweep the solid hull footprint before committing either translation or rotation.
    public static bool Constrain(Vector3 start,Quaternion rotation,ref Vector3 next,ref Quaternion nextRotation,Transform other,Transform self=null)
    {
        if(other==null)return false;
        // Once completely below the other hull, a wreck no longer blocks surface travel.
        float scaleA=self!=null?Mathf.Abs(self.lossyScale.x)/2f:1f,scaleB=Mathf.Abs(other.lossyScale.x)/2f;
        float height=9.5f*Mathf.Max(scaleA,scaleB);
        float minY=Mathf.Min(start.y,next.y)-other.position.y,maxY=Mathf.Max(start.y,next.y)-other.position.y;
        if(minY>=height||maxY<=-height)return false;
        float yaw=rotation.eulerAngles.y,turn=Mathf.DeltaAngle(yaw,nextRotation.eulerAngles.y);
        float fraction=NavalShipContactMath.SafeFraction(start.x,start.z,yaw,next.x,next.z,turn,
            other.position.x,other.position.z,other.eulerAngles.y,scaleA,scaleB);
        if(fraction>=1f)return false;
        // Preserve buoyancy while blocking horizontal penetration and hull rotation.
        float y=next.y;next=Vector3.Lerp(start,next,fraction);next.y=y;
        nextRotation=Quaternion.Slerp(rotation,nextRotation,fraction);return true;
    }
}

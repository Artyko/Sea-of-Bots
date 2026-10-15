using UnityEngine;
using System.Collections.Generic;
public static class NavalRigContact
{
    private struct Hit
    {public NavalHullVoxel Cell;public Vector3 Point;public bool Attached;}
    private static readonly List<Hit> hits=new List<Hit>();
    private static bool Touches(Vector3 ship,Quaternion rotation,Vector3 centre,Quaternion cellRotation,Vector3 halfSize)
    {
        Quaternion inverse=Quaternion.Inverse(rotation),relative=inverse*cellRotation;
        Vector3 p=inverse*(centre-ship);
        Vector3 right=relative*Vector3.right,up=relative*Vector3.up,forward=relative*Vector3.forward;
        Vector3 extent=new Vector3(Mathf.Abs(right.x)*halfSize.x+Mathf.Abs(up.x)*halfSize.y+Mathf.Abs(forward.x)*halfSize.z,
            Mathf.Abs(right.y)*halfSize.x+Mathf.Abs(up.y)*halfSize.y+Mathf.Abs(forward.y)*halfSize.z,
            Mathf.Abs(right.z)*halfSize.x+Mathf.Abs(up.z)*halfSize.y+Mathf.Abs(forward.z)*halfSize.z);
        return NavalRigImpactMath.TouchesHull(p.x,p.y,p.z,extent.x,extent.y,extent.z);
    }
    private static void Scan(NavalRamDamage rig,Vector3 bodyPosition,Quaternion bodyRotation,
        Vector3 rigPosition,Quaternion rigRotation,bool movingRig)
    {
        Quaternion delta=rigRotation*Quaternion.Inverse(rig.transform.rotation);
        foreach(NavalHullVoxel cell in rig.RigCells)
        {
            if(cell==null||cell.Missing)continue;
            bool attached=cell.transform.parent==rig.transform;
            // Detached assemblies move through their Rigidbody, not through the old ship transform.
            if(movingRig&&!attached)continue;
            Vector3 centre=cell.transform.position;Quaternion rotation=cell.transform.rotation;
            if(movingRig){centre=rigPosition+delta*(centre-rig.transform.position);rotation=delta*rotation;}
            if(!Touches(bodyPosition,bodyRotation,centre,rotation,cell.transform.lossyScale*0.5f))continue;
            hits.Add(new Hit{Cell=cell,Point=centre,Attached=attached});
        }
    }
    public static float Resolve(NavalRamDamage mover,Vector3 start,Quaternion rotation,ref Vector3 next,ref Quaternion nextRotation,NavalRamDamage other)
    {
        if(mover==null||other==null)return 1f;
        Vector3 separation=other.transform.position-start;separation.y=0f;
        if(separation.sqrMagnitude>50f*50f)return 1f;
        Vector3 relative=mover.Velocity-other.Velocity;relative.y=0f;
        float speed=relative.magnitude+Mathf.Abs(mover.AngularRate-other.AngularRate)*Mathf.Deg2Rad*6f;
        if(speed<0.05f)return 1f; // Wave bobbing alone must not shred a stationary rig.
        int steps=NavalRigImpactMath.SweepSteps(Vector3.Distance(start,next),Quaternion.Angle(rotation,nextRotation));
        float retained=1f;bool stopped=false;
        for(int i=0;i<=steps;i++)
        {
            float t=(float)i/steps;Vector3 p=Vector3.Lerp(start,next,t);Quaternion q=Quaternion.Slerp(rotation,nextRotation,t);
            hits.Clear();
            Scan(other,p,q,other.transform.position,other.transform.rotation,false);
            Scan(mover,other.transform.position,other.transform.rotation,p,q,true);
            bool wood=false;Vector3 impact=Vector3.zero;NavalRamDamage impactedHull=null;
            foreach(Hit hit in hits)
            {
                if(hit.Cell.Missing)continue;
                retained=Mathf.Min(retained,NavalRigImpactMath.RetainedSpeed(hit.Cell.RigFabric,hit.Attached));
                if(!hit.Cell.RigFabric&&hit.Attached)
                {wood=true;impact=hit.Point;impactedHull=hit.Cell.Owner==mover.Hull?other:mover;}
                // Remove the actual swept cells, including trigger-only cloth, rather than
                // depending on Unity's response to transform-driven static colliders.
                hit.Cell.SetMissing(true);
            }
            if(wood)
            {
                if(impactedHull!=null)impactedHull.RigImpact(impact,speed);
                next=p;nextRotation=q;stopped=true;break;
            }
        }
        if(retained<1f)
        {
            NavalShipVisuals a=mover.GetComponent<NavalShipVisuals>(),b=other.GetComponent<NavalShipVisuals>();
            if(a!=null)a.RefreshRig();if(b!=null)b.RefreshRig();
        }
        // A mast arrests this frame's advance, then its unsupported top can fall clear.
        return stopped?Mathf.Min(retained,0.72f):retained;
    }
}

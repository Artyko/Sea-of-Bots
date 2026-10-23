using UnityEngine;
using System.Collections.Generic;

// One kinematic actor owns the existing voxel shapes. Buoyancy/navigation remain authoritative.
public sealed class NavalShipPhysics : MonoBehaviour
{
    private NavalHull hull;
    private Rigidbody body;
    private NavalHullColliders colliderChunks;
    private float auditAt;
    private int auditRevision=-1,auditCount=-1;
    private string audit="";
    private readonly Dictionary<NavalHullVoxel,int> states=new Dictionary<NavalHullVoxel,int>();
    private readonly HashSet<NavalHullVoxel> pending=new HashSet<NavalHullVoxel>();
    private int attached,loose,staticShapes,invalid;
    private bool fullAudit=true;
    public void MarkDirty(NavalHullVoxel cell){NavalPhysicsQueries.Invalidate();pending.Add(cell);}
    public void RequestAudit(){fullAudit=true;}
    public Rigidbody Body {get{return body;}}
    public void Initialize(NavalHull owner)
    {
        hull=owner;
        // Create before the hull shapes, avoiding an initial static actor per voxel.
        body=gameObject.AddComponent<Rigidbody>();
        body.isKinematic=true;body.useGravity=false;
        body.interpolation=RigidbodyInterpolation.None;
        body.collisionDetectionMode=CollisionDetectionMode.Discrete;
        // Animated cloth, moving guns and damage must not recalculate mass properties.
        body.automaticCenterOfMass=false;body.centerOfMass=Vector3.zero;
        body.automaticInertiaTensor=false;body.inertiaTensor=Vector3.one;body.inertiaTensorRotation=Quaternion.identity;
    }
    public void SetPose(Vector3 position,Quaternion rotation)
    {
        using(NavalPerformance.Measure(NavalPerfArea.HullPose))
        {
            // Move the kinematic actor directly, then explicitly publish its current pose.
            // Rigidbody setters need not update Transform until the next physics step.
            // Rider carry, crew deck carry and local queries must see this frame's pose.
            if(body!=null){
                bool changed=false;
                if(!body.position.Equals(position)){body.position=position;changed=true;}
                if(!body.rotation.Equals(rotation)){body.rotation=rotation;changed=true;}
                if(changed){body.PublishTransform();NavalPhysicsQueries.Invalidate();}
            }else {transform.SetPositionAndRotation(position,rotation);NavalPhysicsQueries.Invalidate();}
        }
    }
    public void FinishConstruction()
    {
        colliderChunks=gameObject.AddComponent<NavalHullColliders>();colliderChunks.Initialize(hull);
        NavalPerformance.SyncTransforms();Audit();
        Debug.Log("[NavalPrototype] Exact collider merging: "+colliderChunks.BatchedCount+" source boxes -> "+colliderChunks.ActiveCount+" merged boxes");
        Debug.Log("[NavalPrototype] Kinematic voxel compound: "+hull.Definition.Name+" | "+audit);
    }
    private void AddState(int state,int sign)
    {
        if((state&1)!=0)attached+=sign;if((state&2)!=0)loose+=sign;
        if((state&4)!=0)staticShapes+=sign;if((state&8)!=0)invalid+=sign;
    }
    private void AuditCell(NavalHullVoxel cell)
    {
        int previous;if(states.TryGetValue(cell,out previous))AddState(previous,-1);
        int state=0;
        Collider collider=cell!=null?cell.PhysicsCollider:null;
        if(collider==null)state=8;
        else
        {
            Rigidbody owner=collider.attachedRigidbody;
            if(cell.Missing&&collider.enabled)state|=8;
            if(cell.ColliderBatched)
            {state|=1;if(collider.enabled||cell.Missing||cell.Detached)state|=8;}
            else if(cell.Detached)
            {state|=2;if(owner==body||collider.enabled&&(owner==null||owner.isKinematic))state|=8;}
            else if(owner==body)state|=1;
            else {if(owner==null)state|=4;state|=8;}
        }
        states[cell]=state;AddState(state,1);
    }
    private void Audit()
    {
        if(fullAudit||auditCount!=hull.Voxels.Count)
        {
            states.Clear();attached=loose=staticShapes=invalid=0;
            foreach(NavalHullVoxel cell in hull.Voxels)AuditCell(cell);
        }
        else foreach(NavalHullVoxel cell in pending)AuditCell(cell);
        pending.Clear();fullAudit=false;
        int problems=invalid;
        if(body==null||!body.isKinematic||body.useGravity||body.interpolation!=RigidbodyInterpolation.None)problems++;
        audit="К:"+attached+" | Д:"+loose+" | S:"+staticShapes+" | !:"+problems;
        auditRevision=hull.Revision;auditCount=hull.Voxels.Count;auditAt=Time.unscaledTime+1f;
        if(problems>0)Debug.LogWarning("[NavalPrototype] Hull physics audit: "+audit);
    }
    public string Diagnostics
    {
        get
        {
            if(fullAudit||auditCount<0||Time.unscaledTime>=auditAt&&(pending.Count>0||auditRevision!=hull.Revision||auditCount!=hull.Voxels.Count))Audit();
            return audit+(colliderChunks!=null?" | "+colliderChunks.BatchedCount+">"+colliderChunks.ActiveCount:"");
        }
    }
}

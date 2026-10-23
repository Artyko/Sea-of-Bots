using UnityEngine;
public partial class NavalRepairSailor
{
    private static Collider[] clearanceHits=new Collider[32];
    private static RaycastHit[] repairSightHits=new RaycastHit[32];
    private bool ClearBody(Vector3 local)
    {
        CharacterController body=Controller();float height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 bottom=boat.TransformPoint(local)+Vector3.up*.45f,top=boat.TransformPoint(local)+Vector3.up*(height-.4f);
        int count;
        while(true)
        {
            count=Physics.OverlapCapsuleNonAlloc(bottom,top,.38f,clearanceHits,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<clearanceHits.Length)break;System.Array.Resize(ref clearanceHits,clearanceHits.Length*2);
        }
        bool clear=true;
        for(int i=0;i<count;i++)if(clearanceHits[i]!=null&&!clearanceHits[i].transform.IsChildOf(robot.transform))clear=false;
        System.Array.Clear(clearanceHits,0,count);return clear;
    }
    private bool RepairSightClear(Vector3 hand,Vector3 offset,NavalRepairableMechanism equipment=null)
    {
        int count;
        while(true)
        {
            count=Physics.RaycastNonAlloc(hand,offset.normalized,repairSightHits,Mathf.Max(0f,offset.magnitude-.35f),PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<repairSightHits.Length)break;System.Array.Resize(ref repairSightHits,repairSightHits.Length*2);
        }
        bool clear=true;
        for(int i=0;i<count;i++)if(repairSightHits[i].collider!=null&&!repairSightHits[i].collider.transform.IsChildOf(robot.transform))
        {
            NavalHullVoxel hit=equipment!=null?NavalVoxelHit.Resolve(repairSightHits[i].collider,repairSightHits[i].point):null;
            if(equipment==null){
                // Adjacent shell/deck edge inside the final 0.65 m belongs to the same patch.
                // Never ignore equipment or an intervening wall farther along the ray.
                if((repairSightHits[i].point-(hand+offset)).sqrMagnitude<=.65f*.65f){
                    var edge=NavalVoxelHit.Resolve(repairSightHits[i].collider,repairSightHits[i].point);
                    if(edge!=null&&edge.Owner==hull&&!edge.RepairableEquipment&&!edge.ManagedRig)continue;
                }
                clear=false;
            }else if(hit==null||hit.RepairMechanism!=equipment)clear=false;
        }
        System.Array.Clear(repairSightHits,0,count);return clear;
    }
}

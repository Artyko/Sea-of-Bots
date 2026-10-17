using UnityEngine;
public partial class NavalRepairSailor
{
    private static Collider[] clearanceHits=new Collider[32];
    private static RaycastHit[] repairSightHits=new RaycastHit[32];
    private bool ClearBody(Vector3 local)
    {
        CharacterController body=Controller();float height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 bottom=boat.TransformPoint(local)+boat.up*.45f,top=boat.TransformPoint(local)+boat.up*(height-.4f);
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
    private bool RepairSightClear(Vector3 hand,Vector3 offset)
    {
        int count;
        while(true)
        {
            count=Physics.RaycastNonAlloc(hand,offset.normalized,repairSightHits,Mathf.Max(0f,offset.magnitude-.35f),PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<repairSightHits.Length)break;System.Array.Resize(ref repairSightHits,repairSightHits.Length*2);
        }
        bool clear=true;
        for(int i=0;i<count;i++)if(repairSightHits[i].collider!=null&&!repairSightHits[i].collider.transform.IsChildOf(robot.transform))clear=false;
        System.Array.Clear(repairSightHits,0,count);return clear;
    }
}

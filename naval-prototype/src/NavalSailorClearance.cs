using UnityEngine;
public partial class NavalRepairSailor
{
    private static Collider[] clearanceHits=new Collider[32];
    private static RaycastHit[] repairSightHits=new RaycastHit[32];
    private bool FriendlyCrewShape(Collider shape){
        if(shape==null)return false;var character=shape.GetComponentInParent<Character>();if(character==null)return false;
        foreach(var worker in hull.RepairWorkers)if(worker!=null&&worker.robot==character&&character.IsPlayerTeam==robot.IsPlayerTeam)return true;
        return false;
    }
    private CollisionFlags movementContacts;
    private void NavigationCapsule(Vector3 local,out Vector3 bottom,out Vector3 top,out float radius)
    {
        var body=Controller();radius=NavigationRadius;Vector3 foot=boat.TransformPoint(local);
        if(body!=null&&body.enabled&&body.bounds.size.y>1f){
            Bounds bounds=body.bounds;Vector3 offset=foot-(robot.transform.position-Vector3.up*footOffset),centre=bounds.center+offset;
            bottom=centre+Vector3.up*(-bounds.extents.y+radius+.03f);top=centre+Vector3.up*(bounds.extents.y-radius);
        }else {bottom=foot+Vector3.up*(radius+.03f);top=foot+Vector3.up*Mathf.Max(radius+.03f,2.6f-radius);}
        // Planning points lie on the deck plane, while an upright capsule on
        // a tilted deck rests above that plane. Do not classify the supporting deck as a wall.
        float cosine=Mathf.Max(.5f,Mathf.Abs(boat.up.y));
        Vector3 clearanceLift=Vector3.up*(radius*(1f/cosine-1f)+.04f);
        bottom+=clearanceLift;top+=clearanceLift;
    }
    private bool ClearBody(Vector3 local)
    {
        Vector3 bottom,top;float radius;NavigationCapsule(local,out bottom,out top,out radius);
        int count;
        while(true)
        {
            count=Physics.OverlapCapsuleNonAlloc(bottom,top,radius,clearanceHits,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<clearanceHits.Length)break;System.Array.Resize(ref clearanceHits,clearanceHits.Length*2);
        }
        bool clear=true;
        for(int i=0;i<count;i++)if(clearanceHits[i]!=null&&!clearanceHits[i].transform.IsChildOf(robot.transform)&&!FriendlyCrewShape(clearanceHits[i]))clear=false;
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
        for(int i=0;i<count;i++)if(repairSightHits[i].collider!=null&&!repairSightHits[i].collider.transform.IsChildOf(robot.transform)&&!FriendlyCrewShape(repairSightHits[i].collider))
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

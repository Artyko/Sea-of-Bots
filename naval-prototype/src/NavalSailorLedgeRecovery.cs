using UnityEngine;
public partial class NavalRepairSailor
{
    private int ledgeWaypoints;
    private bool LedgePathClear(Vector3 from,Vector3 to)
    {
        CharacterController body=Controller();
        float height=body!=null?Mathf.Max(1.8f,body.height*Mathf.Abs(body.transform.lossyScale.y)):2.6f;
        Vector3 bottom=boat.TransformPoint(from)+boat.up*.45f,top=boat.TransformPoint(from)+boat.up*(height-.4f);
        Vector3 movement=boat.TransformVector(to-from);
        foreach(Collider c in Physics.OverlapCapsule(bottom+movement,top+movement,.38f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            if(!c.transform.IsChildOf(robot.transform))return false;
        if(movement.magnitude<.001f)return true;
        foreach(RaycastHit hit in Physics.CapsuleCastAll(bottom,top,.38f,movement.normalized,movement.magnitude,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
            if(!hit.collider.transform.IsChildOf(robot.transform))return false;
        return true;
    }
    private bool TryPlanLedgeRecovery(Vector3 goal)
    {
        ledgeWaypoints=0;
        if(!NavalLedgeRecoveryMath.Needed(feet.y,hull.WorldScale))return false;
        Vector3 inside=new Vector3(NavalLedgeRecoveryMath.InwardX(feet.x),feet.y,NavalLedgeRecoveryMath.InwardZ(feet.z));
        foreach(float shift in new float[]{0f,-.6f,.6f,-1.2f,1.2f})
        {
            Vector3 upper=inside;upper.z=Mathf.Clamp(upper.z+shift,-5.3f,5.3f);
            RaycastHit hit;Vector3 origin=boat.TransformPoint(upper)+boat.up*.15f;
            float distance=Mathf.Max(.3f,(upper.y-.825f)*hull.WorldScale+.4f);
            if(!Physics.Raycast(origin,-boat.up,out hit,distance,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)||
                !hit.collider.transform.IsChildOf(boat)||Vector3.Dot(hit.normal,boat.up)<.6f)continue;
            Vector3 lower=boat.InverseTransformPoint(hit.point);
            // Require the actual deck, not another rail, gun or the stair opening.
            if(Mathf.Abs(lower.y-.825f)*hull.WorldScale>.12f||!LedgePathClear(feet,upper)||!LedgePathClear(upper,lower))continue;
            route.Add(upper);route.Add(lower);ledgeWaypoints=2;
            Append(NavalRepairRoutes.Plan(new NavalWalkPoint(lower.x,lower.y,lower.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition.Compartments,hull.Definition.StairWidth,hull.Floor));
            Debug.Log("[NavalPrototype] Crew ledge recovery: "+feet+" -> "+lower);
            return true;
        }
        return false;
    }
}

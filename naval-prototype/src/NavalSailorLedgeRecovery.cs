using UnityEngine;
public partial class NavalRepairSailor
{
    private int ledgeWaypoints;
    private bool LedgePathClear(Vector3 from,Vector3 to)
    {
        // Use the same full-size capsule as ordinary navigation. The old
        // 38 cm probe accepted gaps that an 80 cm sailor could never traverse.
        return ClearBody(to)&&NavigationClear(from,to);
    }
    private bool TryPlanLedgeRecovery(Vector3 goal)
    {
        ledgeWaypoints=0;
        var destination=new NavalWalkPoint(goal.x,goal.y,goal.z);
        if(NavalCapitalAADeck.Upper(destination,hull.Definition)||NavalRaisedBridge.Upper(destination,hull.Definition)||
           NavalRaisedBridge.Flight(new NavalWalkPoint(feet.x,feet.y,feet.z),hull.Definition))return false;
        // A missed moving-deck carry used to leave a routed sailor at a fixed
        // world position while the ship sailed away. The ordinary ledge path
        // then tried to walk dozens of local metres back and was rebuilt every
        // frame. This is not an overboard rescue (those sailors are handled by
        // NavalSailorRescue); restore only a routed, deck-height body to a clear
        // weather-deck muster point and continue through the normal planner.
        if(NavalLedgeRecoveryMath.Detached(feet.x,feet.y,feet.z,hull.Definition.HullHalfWidth(feet.z),
            hull.Definition.WeatherDeck,hull.WorldScale))
        {
            float recoveredZ=NavalLedgeRecoveryMath.RecoveryZ(feet.z);
            float recoveredX=NavalLedgeRecoveryMath.RecoveryX(feet.x,hull.Definition.HullHalfWidth(recoveredZ),hull.WorldScale);
            Vector3 recovered=new Vector3(recoveredX,hull.Definition.WeatherDeck,recoveredZ);
            Vector3 lost=feet;feet=recovered;route.Clear();stuck=0f;moveSpeed=0f;HoldPosition();
            Append(NavalShipRoutes.Plan(new NavalWalkPoint(recovered.x,recovered.y,recovered.z),
                new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition));
            Debug.Log("[NavalPrototype] Crew moving-deck attachment recovered: ship="+boat.name+" lost="+lost+" restored="+recovered+" duty="+Status);
            return true;
        }
        float deck=NavalLedgeRecoveryMath.DeckBelow(feet.y,hull.Floor,hull.Definition.WeatherDeck,hull.Definition.DeckCount);
        NavalWalkPoint footPoint=new NavalWalkPoint(feet.x,feet.y,feet.z);
        if(NavalShipRoutes.InStairwell(footPoint,hull.Definition)||NavalStairTraffic.InPassage(footPoint,hull.Definition)||!NavalLedgeRecoveryMath.Needed(feet.y-(deck-.825f),hull.WorldScale))return false;
        Vector3 inside=new Vector3(NavalLedgeRecoveryMath.InwardX(feet.x),feet.y,NavalLedgeRecoveryMath.InwardZ(feet.z));
        // A sailor is most often standing on a cargo rim.  Step toward the
        // clear centre aisle before trying longitudinal hops that can merely
        // land on the next crate in the row.
        float centre=feet.x<0f?.65f:-.65f;
        foreach(float inward in new float[]{centre,centre*.5f,0f,-centre*.5f,-centre})foreach(float shift in new float[]{0f,-.6f,.6f,-1.2f,1.2f})
        {
            Vector3 upper=inside;upper.x=Mathf.Clamp(upper.x+inward,-2.1f,2.1f);upper.z=Mathf.Clamp(upper.z+shift,-5.3f,5.3f);
            if(new Vector2(upper.x-feet.x,upper.z-feet.z).magnitude*hull.WorldScale<.6f)continue;
            RaycastHit hit;Vector3 origin=boat.TransformPoint(upper)+boat.up*.15f;
            float distance=Mathf.Max(.3f,(upper.y-deck)*hull.WorldScale+.4f);
            if(!Physics.Raycast(origin,-boat.up,out hit,distance,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)||
                !hit.collider.transform.IsChildOf(boat)||Vector3.Dot(hit.normal,boat.up)<.6f)continue;
            Vector3 lower=boat.InverseTransformPoint(hit.point);
            // Require the actual deck, not another rail, gun or the stair opening.
            if(Mathf.Abs(lower.y-deck)*hull.WorldScale>.12f||!LedgePathClear(feet,upper)||!LedgePathClear(upper,lower))continue;
            route.Add(upper);route.Add(lower);ledgeWaypoints=2;
            Append(NavalShipRoutes.Plan(new NavalWalkPoint(lower.x,lower.y,lower.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition));
            Debug.Log("[NavalPrototype] Crew ledge recovery: "+feet+" -> "+lower);
            return true;
        }
        return false;
    }
}

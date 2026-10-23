using UnityEngine;
public partial class NavalRepairSailor
{
    public bool PumpAssignment,FireAssignment;
    public bool ChoosingNextTask {get{return nextTaskPending;}}
    public bool CanReceiveSupport(bool fire)
    {
        return robot!=null&&robot.IsAlive()&&!stranded&&!Defending&&!Rescuing&&!Assaulting&&!FinalAttackDuty&&!HandlingSails&&
            (nextTaskPending||!MaintenanceDuty&&!HasJob)&&Time.time>=(fire?fireRetry:pumpRetry);
    }
    private readonly System.Collections.Generic.Dictionary<Vector3,float> travelCosts=new System.Collections.Generic.Dictionary<Vector3,float>();
    private float travelCostsAt;private Vector3 travelCostsStart;private Vector3 travelCostsScale;
    public float SupportTravelCost(Vector3 goal)
    {
        Vector3 start=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset),previous=start;float cost=0;
        Vector3 scale=boat.lossyScale;
        if(!scale.Equals(travelCostsScale)||Time.time>=travelCostsAt||boat.TransformVector(start-travelCostsStart).sqrMagnitude>.25f){travelCosts.Clear();travelCostsAt=Time.time+.25f;travelCostsStart=start;travelCostsScale=scale;}
        float cached;if(travelCosts.TryGetValue(goal,out cached))return cached;
        foreach(var p in NavalShipRoutes.Plan(new NavalWalkPoint(start.x,start.y,start.z),new NavalWalkPoint(goal.x,goal.y,goal.z),hull.Definition))
        {Vector3 next=new Vector3(p.X,p.Y,p.Z);cost+=Vector3.Scale(next-previous,scale).magnitude;previous=next;}
        if(travelCosts.Count>=64)travelCosts.Clear();travelCosts[goal]=cost;return cost;
    }
}

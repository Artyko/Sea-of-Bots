using UnityEngine;
public partial class NavalRepairSailor
{
    private NavalStations sailDuty;
    private float sailSettled,sailRetry,sailElapsed;
    private Vector3 sailStand;
    private int sailReplans;
    private NavalSailHaulingAnimation sailHauling;
    private readonly System.Collections.Generic.List<Vector3> rejectedSailStands=new System.Collections.Generic.List<Vector3>(4);
    public bool AtSailControls {get{return sailDuty!=null&&robot!=null&&robot.IsAlive()&&SailReach(feet);}}
    private bool SailReach(Vector3 stand)
    {
        Vector3 delta=sailDuty.SailRopePoint-boat.TransformPoint(stand);
        Vector3 horizontal=Vector3.ProjectOnPlane(delta,boat.up);
        if(!NavalSailWorkLayout.InReach(horizontal.sqrMagnitude,(stand.y-hull.Definition.WeatherDeck)*hull.WorldScale))return false;
        Vector3 hand=boat.TransformPoint(stand)+boat.up*1.2f;return RepairSightClear(hand,sailDuty.SailRopePoint-hand);
    }
    private bool PlanSailApproach()
    {
        float best=float.PositiveInfinity;bool found=false;
        for(int x=-1;x<=1;x++)for(int z=-1;z<=1;z++)
        {
            Vector3 point=sailDuty.SailPost+new Vector3(x*.55f,0,z*.75f)/hull.WorldScale;
            bool rejected=false;foreach(var previous in rejectedSailStands)if((point-previous).sqrMagnitude<.001f)rejected=true;
            if(rejected||!Supported(point)||!ClearBody(point)||!SailReach(point))continue;
            float cost=SupportTravelCost(point);if(cost<best){best=cost;sailStand=point;found=true;}
        }
        if(!found)return false;route.Clear();stuck=0;PlanTo(sailStand);return true;
    }
    public bool HandlingSails {get{return sailDuty!=null;}}
    public bool CanHandleSails {get{return robot!=null&&robot.IsAlive()&&!stranded&&!Rescuing&&!Defending&&!Assaulting&&
        !HandlingSails&&(nextTaskPending||!MaintenanceDuty)&&leak==null&&Time.time>=sailRetry;}}
    public bool CanBorrowForRecoverySails {get{
        if(robot==null||!robot.IsAlive()||stranded||Rescuing||Defending||Assaulting||HandlingSails||fireJob!=null||FinalAttackDuty||Time.time<sailRetry)return false;
        return NavalSailRecoveryPolicy.MayBorrow(RepairingCannon,waitingForRepair,returning,pumpDuty,hull.CombatCritical,hull.UrgentTrim,
            bailState!=0,BucketWater>0,RepairingLeak,OtherLeakRepairer(),route.Count>0);
    }}
    public void StartRecoverySails(NavalStations stations)
    {
        if(!stations.ReverseManeuver||!CanBorrowForRecoverySails||stations.SailOperator!=null)return;
        Release();route.Clear();returning=changingPost=false;sailRetry=0;StartSails(stations);
    }
    public void StartSails(NavalStations stations)
    {
        if(!CanHandleSails||stations.SailOperator!=null)return;
        Release();route.Clear();returning=changingPost=false;
        sailDuty=stations;stations.SailOperator=this;sailSettled=0;
        Debug.Log("[NavalPrototype] Sail hand assigned: "+boat.name+" recovery="+stations.ReverseManeuver+" from="+feet);
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
        sailElapsed=0;sailReplans=0;rejectedSailStands.Clear();
        if(!PlanSailApproach()){sailRetry=Time.time+5;ReleaseSails();BeginReturn();}
    }
    private void ReleaseSails()
    {
        if(sailDuty!=null&&sailDuty.SailOperator==this)sailDuty.SailOperator=null;
        if(sailHauling!=null)sailHauling.StopHauling();
        sailDuty=null;sailSettled=0;if(hammer!=null)hammer.gameObject.SetActive(true);
    }
    private bool TickSails(float dt)
    {
        if(sailDuty==null)return false;
        bool emergency=!FinalAttackDuty&&!sailDuty.LastAttackApproach&&!sailDuty.ReverseManeuver&&(hull.HasRepairDemand||hull.CombatCritical||hull.UrgentTrim||hull.Fire!=null&&hull.Fire.HasFire);
        if(emergency&&!sailDuty.RescueStop||sailDuty.ManualSails&&!sailDuty.RescueStop||sailDuty.PlayerStation==sailDuty.SailStation||hull.Sunk)
        {ReleaseSails();BeginReturn();return true;}
        sailElapsed+=dt;if(sailElapsed>45f){sailRetry=Time.time+5;ReleaseSails();BeginReturn();return true;}
        robot.DisableAI();
        if(route.Count>0&&AtSailControls)route.Clear();
        if(route.Count>0)
        {
            var movement=FollowRoute(dt,4f);
            if(movement==RouteResult.Fell){stranded=true;ReleaseSails();route.Clear();return true;}
            if(movement==RouteResult.Blocked)
            {
                rejectedSailStands.Add(sailStand);
                if(++sailReplans<=3&&PlanSailApproach())return true;
                sailRetry=Time.time+5;ReleaseSails();BeginReturn();
            }
            return true;
        }
        if(!AtSailControls){if(!PlanSailApproach()){sailRetry=Time.time+5;ReleaseSails();BeginReturn();}return true;}
        HoldPosition();Walking(false,dt);if(hammer!=null)hammer.gameObject.SetActive(false);
        if(sailHauling==null){sailHauling=robot.gameObject.AddComponent<NavalSailHaulingAnimation>();sailHauling.Initialize(model);}
        sailHauling.Haul(sailDuty.SailRopePoint);
        Vector3 facing=sailDuty.SailRopePoint-robot.transform.position;facing.y=0;
        if(facing.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.LookRotation(facing);
        sailSettled=sailDuty.SailOperatorSettled?sailSettled+dt:0;
        if(sailSettled>=.8f){ReleaseSails();BeginReturn();}
        return true;
    }
}

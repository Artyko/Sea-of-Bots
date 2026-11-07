using System.Collections.Generic;
using UnityEngine;

public partial class NavalRepairSailor
{
    private int capitalSlot=-1,capitalStage,capitalStep;
    private readonly List<Vector3> capitalPath=new List<Vector3>();
    private float capitalStuck,capitalRetry;
    public string CapitalAccessStatus {get{return capitalStage==1?"Going to turret hatch":capitalStage==2?"Entering turret":capitalStage==3?"Seated inside turret":capitalStage==4?"Leaving turret":"Turret crew";}}
    public bool CapitalReserved {get{return capitalStage>0;}}
    public bool CapitalSeated {get{return capitalStage==3;}}
    private void ClearCapitalAccess(NavalStations controls)
    {
        if(capitalSlot>=0){home=capitalStage==4?feet:NavalTurretAccessLayout.Approach(hull.Definition,capitalSlot);controls.RequestCapitalAccess(capitalSlot,false);}
        route.Clear();returning=changingPost=false;escapePending=false;
        capitalSlot=-1;capitalStage=0;capitalStep=0;capitalPath.Clear();SetMainBatterySeated(false);Walking(false,0);
    }
    public void TickCapitalStation(NavalStations controls,int slot,float dt)
    {
        if(robot==null||!robot.IsAlive())return;
        if(stranded){ClearCapitalAccess(controls);return;}
        Vector3 seat=controls.CapitalSeat(slot),approach=NavalTurretAccessLayout.Approach(hull.Definition,slot);
        if(capitalStage==4){TickCapitalExit(controls,dt);return;}
        if(Time.time<capitalRetry){HoldPosition();return;}
        if(capitalStage==0){
            capitalSlot=slot;SpareMaintenance=false;escapePending=false;Release();route.Clear();returning=changingPost=false;
            if(AtMainBatteryPost(seat)){capitalStage=3;}
            else {capitalStage=1;SetMainBatterySeated(false);SetPost(approach,true);}
        }
        if(capitalStage==3){
            controls.RequestCapitalAccess(slot,false);home=seat;feet=seat;route.Clear();returning=changingPost=false;
            Walking(false,dt);HoldPosition();SetMainBatterySeated(true);return;
        }
        controls.RequestCapitalAccess(slot,true);
        if(capitalStage==1){
            if(!AtStation(approach)){Tick(dt,false);return;}
            Walking(false,dt);if(!controls.CapitalDoorReady(slot))return;
            route.Clear();returning=changingPost=false;capitalPath.Clear();capitalPath.AddRange(NavalTurretAccessLayout.Entry(hull.Definition,slot));capitalStep=0;capitalStage=2;capitalStuck=0;
        }
        if(capitalStage==2&&FollowCapitalPath(dt)){
            capitalStage=3;controls.RequestCapitalAccess(slot,false);home=seat;feet=seat;HoldPosition();SetMainBatterySeated(true);
        }
    }
    public void TickCapitalExit(NavalStations controls,float dt)
    {
        if(capitalStage==0)return;
        if(robot==null||!robot.IsAlive()||stranded){ClearCapitalAccess(controls);return;}
        int slot=capitalSlot;controls.RequestCapitalAccess(slot,true);SetMainBatterySeated(false);
        if(capitalStage==1){ClearCapitalAccess(controls);return;}
        if(capitalStage==3){feet=controls.CapitalSeat(slot);HoldPosition();if(!controls.CapitalDoorReady(slot))return;}
        if(capitalStage!=4){
            var entry=NavalTurretAccessLayout.Entry(hull.Definition,slot);capitalPath.Clear();
            int last=capitalStage==2?Mathf.Min(capitalStep,entry.Count-1):entry.Count-1;
            for(int i=last;i>=0;i--)capitalPath.Add(entry[i]);
            capitalPath.Add(NavalTurretAccessLayout.Approach(hull.Definition,slot));capitalStep=0;capitalStage=4;capitalStuck=0;route.Clear();returning=changingPost=false;
        }
        if(FollowCapitalPath(dt)){ClearCapitalAccess(controls);capitalRetry=Time.time+1f;}
    }
    private bool FollowCapitalPath(float dt)
    {
        if(capitalStep>=capitalPath.Count){Walking(false,dt);return true;}
        Vector3 goal=capitalPath[capitalStep];float before=boat.TransformVector(goal-feet).magnitude;
        if(capitalSlot==3&&Mathf.Abs(goal.y-feet.y)*hull.WorldScale>.02f&&
            new Vector2(goal.x-feet.x,goal.z-feet.z).magnitude*hull.WorldScale<.4f)
        {feet=Vector3.MoveTowards(feet,goal,2.4f*dt/hull.WorldScale);HoldPosition();}
        else Move(goal,dt,capitalStep==capitalPath.Count-1);
        Walking(true,dt);
        Vector3 delta=boat.TransformVector(goal-feet);
        // A full capsule spans several descending treads; its feet cannot
        // reach each tread's nominal height until it advances outward.
        bool descending=capitalStage==4&&capitalStep+1<capitalPath.Count&&capitalPath[capitalStep+1].y<goal.y;
        float heightTolerance=descending?Mathf.Max(.32f,NavigationRadius+.4f):.32f;
        bool exteriorEnd=capitalStage==4&&capitalStep==capitalPath.Count-1&&NavalPhysicsQueries.HasLocalSupport(hull,feet);
        float horizontalTolerance=exteriorEnd?NavigationRadius+1.25f:descending?NavigationRadius+.2f:.16f;
        if(exteriorEnd)heightTolerance=.4f;
        bool verticalX=capitalSlot==3&&capitalStep>0&&
            new Vector2(goal.x-capitalPath[capitalStep-1].x,goal.z-capitalPath[capitalStep-1].z).sqrMagnitude<.00001f&&
            Mathf.Abs(goal.y-capitalPath[capitalStep-1].y)*hull.WorldScale>.15f;
        if(verticalX){horizontalTolerance=.12f;heightTolerance=.08f;}
        if(new Vector2(delta.x,delta.z).sqrMagnitude<horizontalTolerance*horizontalTolerance&&Mathf.Abs(delta.y)<heightTolerance){capitalStep++;capitalStuck=0;return capitalStep>=capitalPath.Count;}
        capitalStuck=NavalRouteProgress.Step(capitalStuck,before,delta.magnitude,dt,false,false);
        if(capitalStuck>6f){
            capitalStuck=0;Debug.Log("[NavalPrototype] Turret hatch path blocked: ship="+boat.name+" slot="+capitalSlot+" stage="+capitalStage+" feet="+feet+" target="+goal);
            // Back out through the reached treads before retrying; never teleport through armour.
            if(capitalStage==2){var prefix=new List<Vector3>();for(int i=Mathf.Min(capitalStep-1,capitalPath.Count-1);i>=0;i--)prefix.Add(capitalPath[i]);prefix.Add(NavalTurretAccessLayout.Approach(hull.Definition,capitalSlot));capitalPath.Clear();capitalPath.AddRange(prefix);capitalStep=0;capitalStage=4;capitalRetry=Time.time+3f;}
        }
        return false;
    }
}

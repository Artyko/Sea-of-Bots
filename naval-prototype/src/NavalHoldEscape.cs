using System.Collections.Generic;
using UnityEngine;

public partial class NavalSailorRescue
{
    public string Status {get{return escapeShip!=null?"Escaping flooded hold":climbing?"Climbing rescue ladder":"Swimming to ladder";}}
    private NavalHull escapeShip,exitedWreck;
    private Vector3 escapeFeet;
    private float escapeStuck,escapeRetry,escapeDrySince=-1f;
    private readonly List<NavalWalkPoint> escapeRoute=new List<NavalWalkPoint>();
    private NavalHull InteriorShip(Vector3 point)
    {
        if(home!=exitedWreck&&Inside(home,point))return home;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
            if(ladder!=null&&ladder.Hull!=exitedWreck&&Inside(ladder.Hull,point))return ladder.Hull;
        return null;
    }
    private bool Inside(NavalHull ship,Vector3 point)
    {
        if(ship==null||ship.Root==null)return false;
        Vector3 local;
        NavalShipPhysics physics=ship.PhysicsBody;
        if(physics==null)local=ship.Root.InverseTransformPoint(point);
        else if(!physics.TryWaterLocalPoint(point,out local))return false;
        return Mathf.Abs(local.x)<=ship.Definition.HullHalfWidth(local.z)+.1f/ship.WorldScale&&NavalHoldEscapeRules.InHold(local.x,local.y,local.z,ship.Floor,ship.Definition.WeatherDeck);
    }
    private void BeginHoldEscape(NavalHull ship)
    {
        escapeShip=ship;chosen=null;climbing=false;escapeStuck=0f;escapeRetry=0f;escapeDrySince=-1f;
        escapeFeet=ship.Root.InverseTransformPoint(robot.transform.position-ship.Root.up*worker.FootOffset);
        PlanHoldEscape();worker.BoardingWalk(true);
    }
    private void PlanHoldEscape()
    {
        escapeRoute.Clear();
        var definition=escapeShip.Definition;
        var start=new NavalWalkPoint(escapeFeet.x,escapeFeet.y,escapeFeet.z);
        var landing=new NavalWalkPoint(1.65f,definition.WeatherDeck,1.1f);
        if(definition.Destroyer)
        {
            float stair=escapeFeet.z<0?-20.5f:20.5f;
            landing=new NavalWalkPoint((escapeFeet.x<0?-5.6f:5.6f)/definition.Scale,
                definition.WeatherDeck,(stair+5f)/definition.Scale);
        }
        escapeRoute.AddRange(NavalSupplyRoutes.Plan(start,landing,definition));
    }
    private void EscapeHold(float dt)
    {
        Transform ship=escapeShip.Root;if(ship==null){escapeShip=null;return;}
        // Carry with the hull, then use the controller for every route step.
        // Do not apply open-sea buoyancy under a solid upper deck.
        Place(ship.TransformPoint(escapeFeet)+ship.up*worker.FootOffset);
        NavalTestWorld world=NavalTestWorld.Active;
        bool under=HeadUnder(world);
        if(under)escapeDrySince=-1f;else if(escapeDrySince<0f)escapeDrySince=Time.time;
        bool stable=Mathf.Abs(escapeFeet.y-escapeShip.Floor)<.2f||escapeFeet.y>=.65f;
        if(NavalHoldSafety.CanResume(under,escapeShip.Sunk,AirRemaining(world),escapeDrySince<0f?0f:Time.time-escapeDrySince,stable))
        {FinishHoldEscape();return;}
        if(Time.time<escapeRetry)return;
        if(escapeRoute.Count==0)PlanHoldEscape();
        NavalWalkPoint waypoint=escapeRoute[0];Vector3 target=new Vector3(waypoint.X,waypoint.Y,waypoint.Z);
        Vector3 movementTarget=escapeShip.Bilge!=null?escapeShip.Bilge.PassageTarget(escapeFeet,target,worker,dt):target;
        Vector3 offset=ship.TransformVector(movementTarget-escapeFeet),before=escapeFeet;
        Vector3 look=offset;look.y=0f;
        if(look.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),360f*dt);
        bool stairs=NavalShipRoutes.InStairwell(new NavalWalkPoint(escapeFeet.x,escapeFeet.y,escapeFeet.z),escapeShip.Definition)||NavalShipRoutes.InStairwell(new NavalWalkPoint(target.x,target.y,target.z),escapeShip.Definition);
        float speed=worker.DeckWalkSpeed*(stairs?0.8f:1f);
        Vector3 step=Vector3.ClampMagnitude(offset,speed*dt);
        CharacterController body=worker.BodyController;
        if(body!=null&&body.enabled){NavalCrewPassage.Prepare(body);body.Move(step+Vector3.down*.3f*dt);}else robot.transform.position+=step;
        escapeFeet=ship.InverseTransformPoint(robot.transform.position-ship.up*worker.FootOffset);
        worker.BoardingMoved(ship.TransformVector(escapeFeet-before).magnitude);
        if((escapeRoute.Count>1||escapeFeet.y>=0.65f)&&(stairs?
            NavalWalkRoutes.Reached(escapeFeet.x-target.x,escapeFeet.y-target.y,escapeFeet.z-target.z,escapeShip.WorldScale):
            NavalRouteArrival.Reached(escapeFeet.x,escapeFeet.y,escapeFeet.z,target.x,target.y,target.z,escapeRoute.Count==1)))
        {
            escapeRoute.RemoveAt(0);escapeStuck=0f;
            if(escapeRoute.Count==0)
            {
                FinishHoldEscape();
            }
        }
        else
        {
            escapeStuck=NavalRouteProgress.Step(escapeStuck,ship.TransformVector(target-before).magnitude,ship.TransformVector(target-escapeFeet).magnitude,dt,false,worker.WorkingOnPassage);
            if(escapeStuck>3f){escapeStuck=0f;escapeRetry=Time.time+1f;PlanHoldEscape();}
            // A genuine fall through the hull switches back to sea rescue.
            if(!NavalHoldEscapeRules.InHold(escapeFeet.x,Mathf.Min(escapeFeet.y,0.59f),escapeFeet.z,escapeShip.Floor))
            {if(escapeShip.Sunk)exitedWreck=escapeShip;escapeShip=null;escapeRoute.Clear();worker.BoardingWalk(false);scanAt=0f;}
        }
    }
    private void FinishHoldEscape()
    {
        NavalHull landed=escapeShip;escapeShip=null;escapeRoute.Clear();worker.BoardingWalk(false);
        if(landed.Sunk)
        {
            exitedWreck=landed;scanAt=0f;escapeDrySince=-1f;
            Debug.Log("[NavalPrototype] Sailor escaped wreck: sailor="+robot.GetInstanceID()+" ship="+landed.Root.name);
            return;
        }
        worker.EndRescue();
        if(landed!=home)
        {
            var world=NavalTestWorld.Active;
            if(world!=null&&world.AcceptConvoySurvivor(home,landed,robot,worker))
            {
                home=landed;scanAt=0f;escapeDrySince=-1f;
                return;
            }
            NavalBoarder boarder=GetComponent<NavalBoarder>();
            if(boarder==null){boarder=gameObject.AddComponent<NavalBoarder>();boarder.Initialize(robot,worker,home);}
            boarder.AdoptShip(landed);
        }
        scanAt=0f;escapeDrySince=-1f;
    }

}

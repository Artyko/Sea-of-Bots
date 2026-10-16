using System.Collections.Generic;
using UnityEngine;

public partial class NavalSailorRescue
{
    public string Status {get{return escapeShip!=null?"Escaping flooded hold":"Swimming to ladder";}}
    private NavalHull escapeShip;
    private Vector3 escapeFeet;
    private float escapeStuck,escapeRetry;
    private readonly List<NavalWalkPoint> escapeRoute=new List<NavalWalkPoint>();
    private NavalHull InteriorShip(Vector3 point)
    {
        if(Inside(home,point))return home;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
            if(ladder!=null&&Inside(ladder.Hull,point))return ladder.Hull;
        return null;
    }
    private bool Inside(NavalHull ship,Vector3 point)
    {
        if(ship==null||ship.Root==null)return false;
        Vector3 local=ship.Root.InverseTransformPoint(point);
        return NavalHoldEscapeRules.InHold(local.x,local.y,local.z);
    }
    private void BeginHoldEscape(NavalHull ship)
    {
        escapeShip=ship;chosen=null;climbing=false;escapeStuck=0f;escapeRetry=0f;
        escapeFeet=ship.Root.InverseTransformPoint(robot.transform.position-ship.Root.up*worker.FootOffset);
        PlanHoldEscape();worker.BoardingWalk(true);
    }
    private void PlanHoldEscape()
    {
        escapeRoute.Clear();
        escapeRoute.AddRange(NavalRepairRoutes.Plan(new NavalWalkPoint(escapeFeet.x,escapeFeet.y,escapeFeet.z),new NavalWalkPoint(1.65f,0.825f,1.1f),escapeShip.Definition.Compartments,escapeShip.Definition.StairWidth));
    }
    private void EscapeHold(float dt)
    {
        Transform ship=escapeShip.Root;if(ship==null){escapeShip=null;return;}
        // Carry with the hull, then use the controller for every route step.
        // Do not apply open-sea buoyancy under a solid upper deck.
        Place(ship.TransformPoint(escapeFeet)+ship.up*worker.FootOffset);
        if(Time.time<escapeRetry)return;
        if(escapeRoute.Count==0)PlanHoldEscape();
        NavalWalkPoint waypoint=escapeRoute[0];Vector3 target=new Vector3(waypoint.X,waypoint.Y,waypoint.Z);
        if(escapeShip.Bilge!=null)escapeShip.Bilge.RequestPassage(escapeFeet,target);
        Vector3 offset=ship.TransformVector(target-escapeFeet),before=escapeFeet;
        Vector3 look=offset;look.y=0f;
        if(look.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(look),360f*dt);
        bool stairs=NavalWalkRoutes.InStairwell(escapeFeet.x,escapeFeet.y,escapeFeet.z,escapeShip.Definition.StairWidth*.5f+.175f)||NavalWalkRoutes.InStairwell(target.x,target.y,target.z,escapeShip.Definition.StairWidth*.5f+.175f);
        float speed=worker.DeckWalkSpeed*(stairs?0.8f:0.65f);
        Vector3 step=Vector3.ClampMagnitude(offset,speed*dt);
        CharacterController body=robot.GetComponentInChildren<CharacterController>();
        if(body!=null&&body.enabled)body.Move(step+Vector3.down*.3f*dt);else robot.transform.position+=step;
        escapeFeet=ship.InverseTransformPoint(robot.transform.position-ship.up*worker.FootOffset);
        worker.BoardingMoved(ship.TransformVector(escapeFeet-before).magnitude);
        if((escapeRoute.Count>1||escapeFeet.y>=0.65f)&&NavalRouteArrival.Reached(escapeFeet.x,escapeFeet.y,escapeFeet.z,target.x,target.y,target.z,escapeRoute.Count==1))
        {
            escapeRoute.RemoveAt(0);escapeStuck=0f;
            if(escapeRoute.Count==0)
            {
                NavalHull landed=escapeShip;escapeShip=null;worker.BoardingWalk(false);worker.EndRescue();
                if(landed!=home)
                {
                    NavalBoarder boarder=GetComponent<NavalBoarder>();
                    if(boarder==null){boarder=gameObject.AddComponent<NavalBoarder>();boarder.Initialize(robot,worker,home);}
                    boarder.AdoptShip(landed);
                }
                scanAt=0f;
            }
        }
        else
        {
            if(Vector3.Distance(before,target)-Vector3.Distance(escapeFeet,target)<dt*.05f)escapeStuck+=dt;else escapeStuck=0f;
            if(escapeStuck>3f){escapeStuck=0f;escapeRetry=Time.time+1f;PlanHoldEscape();}
            // A genuine fall through the hull switches back to sea rescue.
            if(!NavalHoldEscapeRules.InHold(escapeFeet.x,Mathf.Min(escapeFeet.y,0.59f),escapeFeet.z))
            {escapeShip=null;escapeRoute.Clear();worker.BoardingWalk(false);scanAt=0f;}
        }
    }
}

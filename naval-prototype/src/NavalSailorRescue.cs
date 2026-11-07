using UnityEngine;
using System.Reflection;
[DefaultExecutionOrder(350)]
public partial class NavalSailorRescue : MonoBehaviour
{
    private Character robot;
    private NavalRepairSailor worker;
    private NavalHull home;
    private NavalSeaLadder chosen;
    private int side,stage;
    private bool climbing;
    private Vector3 feet;
    private float scanAt,headOffset;
    private NavalDrowning drowning;
    private NavalBoarder cachedBoarder;
    private NavalBoarder Boarder {get{if(cachedBoarder==null)cachedBoarder=GetComponent<NavalBoarder>();return cachedBoarder;}}
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public void Initialize(Character sailor,NavalRepairSailor repair,NavalHull ship)
    {
        robot=sailor;worker=repair;home=ship;
        FirstPersonMover mover=robot as FirstPersonMover;Transform head=mover!=null?mover.GetHeadTransform():null;
        headOffset=head!=null?head.position.y-robot.transform.position.y:0.8f;
    }
    public bool AwaitingHomeRescue {get{return robot!=null&&robot.IsAlive()&&worker!=null&&worker.Rescuing&&escapeShip==null&&!home.Sunk&&(chosen==null||chosen.Hull==home);}}
    private void ZeroVelocity()
    {FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);}
    public bool HasAirForWork(float travelSeconds,float workSeconds){
        var world=NavalTestWorld.Active;if(world==null)return false;
        Vector3 foot=robot.transform.position-Vector3.up*worker.FootOffset;
        Vector3 local=home.Root.InverseTransformPoint(foot);
        float exit=HoldExitSeconds(home,local,false);
        return AirRemaining(world)>NavalHoldSafety.AirReserve(exit)+Mathf.Max(0,travelSeconds)+workSeconds+.5f;
    }
    private float HoldExitSeconds(NavalHull ship,Vector3 local,bool stairs)
    {
        float extraDeck=ship.Definition.DeckCount>2&&local.y<.825f?
            ship.Definition.UpperDeckOffset*ship.WorldScale/Mathf.Max(1f,worker.DeckWalkSpeed*.8f)+6f:0f;
        if(ship.Definition.Destroyer)
            return NavalHoldSafety.DestroyerExitSeconds(local.x,local.z,ship.WorldScale,
                (ship.Definition.WeatherDeck-ship.Floor)*ship.WorldScale,worker.DeckWalkSpeed);
        return extraDeck+NavalHoldSafety.ExitSeconds(local.x,local.y,local.z,ship.Floor,ship.WorldScale,worker.DeckWalkSpeed,stairs);
    }
    public void CheckForWater()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(world==null||!world.SimulationActive||robot==null||!robot.IsAlive()||worker.Rescuing)return;
        NavalBoarder boarder=Boarder;if(boarder!=null&&boarder.InFlight)return;
        Vector3 foot=robot.transform.position-Vector3.up*worker.FootOffset;
        NavalHull interior=InteriorShip(foot);
        if(interior!=null)
        {
            bool headUnder=HeadUnder(world);
            if(!interior.Sunk&&!headUnder)return;
            Vector3 local=interior.Root.InverseTransformPoint(foot);
            bool stairs=NavalWalkRoutes.InStairwell(local.x,local.y,local.z,interior.Definition.StairWidth*.5f+.175f,interior.Floor);
            float exitSeconds=HoldExitSeconds(interior,local,stairs);
            float air=AirRemaining(world);
            if(!NavalHoldSafety.MustEscape(headUnder,interior.Sunk,air,exitSeconds)||
                !interior.Sunk&&interior==home&&NavalHoldSafety.CanFinishPatch(air,exitSeconds,worker.ActiveHullPatchSeconds))return;
        }
        else
        {
            float depth=world.WaterLevelAt(foot)-foot.y;
            Vector3 local=home.Root.InverseTransformPoint(foot);
            bool outside=local.y<home.Definition.WeatherDeck-.3f/home.WorldScale&&
                (Mathf.Abs(local.x)>home.Definition.HullHalfWidth(local.z)+.15f/home.WorldScale||Mathf.Abs(local.z)>BoatDimensions.HalfLength);
            // Below-deck sailors outside the shell cannot walk back through the armour.
            // Route them to a boarding ladder even when the wave trough is shallow.
            if(depth<.9f&&!outside)return;
            bool standing=SupportedOnShip(foot);
            if(!HeadUnder(world)&&standing&&!outside)return;
        }
        worker.BeginRescue();if(boarder!=null)boarder.SuspendForRescue();scanAt=0f;climbing=false;chosen=null;
        if(interior!=null)
        {
            Debug.Log("[NavalPrototype] Hold evacuation: air="+AirRemaining(world).ToString("F1")+" sinking="+interior.Sunk);
            BeginHoldEscape(interior);
        }
    }
    private bool SupportedOnShip(Vector3 foot)
    {
        Vector3 origin=foot+Vector3.up*.3f;
        if(NavalPhysicsQueries.HasSupport(origin,Vector3.down,.7f,home.Root,.12f))return true;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
            if(ladder!=null&&ladder.Hull!=home&&NavalPhysicsQueries.HasSupport(origin,Vector3.down,.7f,ladder.Hull.Root,.12f))return true;
        return false;
    }
    private bool HeadUnder(NavalTestWorld world)
    {Vector3 top=NavalDrowning.HeadTop(robot);return world.WaterLevelAt(top)-top.y>.05f;}
    private float AirRemaining(NavalTestWorld world)
    {if(drowning==null)drowning=world.GetComponent<NavalDrowning>();return drowning!=null?drowning.AirRemaining(robot):NavalBreathClock.Capacity;}

    private Vector3 landingPoint;
    private int landingRevision;
    private static bool TryLanding(NavalSeaLadder ladder,int which,out Vector3 landing)
    {
        var hull=ladder.Hull;
        var ship=hull.Definition;
        float scale=hull.WorldScale;
        float sign=which==0?-1f:1f;
        float deck=ship.WeatherDeck;
        for(int i=0;i<3;i++)for(int along=0;along<3;along++)
        {
            float x=ladder.DeckX(which)-sign*i*.65f/scale;
            float z=ship.LadderZ+(along==0?0f:along==1?.65f:-.65f)/scale;
            Vector3 origin=ladder.transform.TransformPoint(new Vector3(x,ship.BoardingY+.35f/scale,z));
            RaycastHit hit;
            if(!Physics.Raycast(origin,-ladder.transform.up,out hit,(ship.BoardingY-deck)*scale+1.05f,
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))continue;
            if(!hit.collider.transform.IsChildOf(ladder.transform))continue;
            Vector3 local=ladder.transform.InverseTransformPoint(hit.point);
            if(Mathf.Abs(local.y-deck)*scale>.45f)continue;
            landing=local+Vector3.up*(.035f/scale);
            return true;
        }
        landing=Vector3.zero;
        return false;
    }
    private static float LadderRoute(NavalSeaLadder ladder,int side,Vector3 local,out NavalSwimPoint next)
    {
        var ship=ladder.Hull.Definition;
        float sign=side==0?-1f:1f;
        float width=ship.HullHalfWidth(0f)+.35f/ladder.Hull.WorldScale;
        return NavalSwimPath.RouteToLadder(new NavalSwimPoint(local.x,local.z),
            new NavalSwimPoint(ladder.OuterX(side)+sign*.45f/ladder.Hull.WorldScale,ship.LadderZ),
            width,.55f/ladder.Hull.WorldScale,out next);
    }
    private void SelectLadder()
    {
        chosen=null;float nearest=float.MaxValue;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
        {
            if(ladder==null||ladder.Hull.Sunk)continue;
            var world=NavalTestWorld.Active;
            if(world!=null&&world.ConvoyMission)
            {
                var controls=NavalStations.ForShip(ladder.Hull.Root);
                if(controls==null||controls.HostileToPlayer==robot.IsPlayerTeam)continue;
            }
            Vector3 local=ladder.transform.InverseTransformPoint(robot.transform.position);
            for(int i=0;i<2;i++)
            {
                if(!ladder.Usable(i))continue;
                float sign=i==0?-1f:1f,scale=ladder.Hull.WorldScale;
                float dx=local.x-ladder.OuterX(i)-sign*.45f/scale;
                float dz=local.z-ladder.Hull.Definition.LadderZ;
                if((dx*dx+dz*dz)*scale*scale>=nearest*nearest)continue;
                NavalSwimPoint next;
                float distance=LadderRoute(ladder,i,local,out next);
                distance*=ladder.Hull.WorldScale;
                Vector3 landing;
                if(distance<nearest&&TryLanding(ladder,i,out landing))
                {nearest=distance;chosen=ladder;side=i;landingPoint=landing;landingRevision=ladder.Hull.Revision;}
            }
        }
    }
    private void Place(Vector3 position)
    {
        CharacterController controller=worker.BodyController;
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        robot.transform.position=position;if(enabled)controller.enabled=true;ZeroVelocity();
    }
    private void LateUpdate()
    {
        using(NavalPerformance.Measure(NavalPerfArea.RescueLate)) LateUpdateMeasured();
    }
    private void LateUpdateMeasured()
    {
        CheckForWater();NavalTestWorld world=NavalTestWorld.Active;
        if(!worker.Rescuing||robot==null||!robot.IsAlive()||world==null||!world.SimulationActive)return;
        float dt=Mathf.Min(Time.deltaTime,0.05f);robot.DisableAI();ZeroVelocity();
        // A flooded hold is still inside a ship. Never steer through its hull
        // toward an exterior sea ladder or lift straight into the upper deck.
        if(escapeShip==null&&!climbing)
        {
            NavalHull interior=InteriorShip(robot.transform.position-Vector3.up*worker.FootOffset);
            if(interior!=null)BeginHoldEscape(interior);
        }
        if(escapeShip!=null){EscapeHold(dt);return;}
        if(climbing)
        {
            if(chosen==null||!chosen.Usable(side)||
                chosen.Hull.Revision!=landingRevision&&!TryLanding(chosen,side,out landingPoint))
            {climbing=false;chosen=null;scanAt=0f;return;}
            landingRevision=chosen.Hull.Revision;
            if(world.ConvoyMission)chosen.Hull.RescuePickupUntil=Time.time+1f;
            float outside=chosen.OuterX(side)+(side==0?-.35f:.35f)/chosen.Hull.WorldScale;
            Vector3 target=stage==0?new Vector3(outside,feet.y,chosen.Hull.Definition.LadderZ):stage==1?new Vector3(outside,chosen.Hull.Definition.BoardingY,chosen.Hull.Definition.LadderZ):
                stage==2?new Vector3(landingPoint.x,chosen.Hull.Definition.BoardingY,landingPoint.z):landingPoint;
            feet=Vector3.MoveTowards(feet,target,3.8f*dt/chosen.Hull.WorldScale);Place(chosen.transform.TransformPoint(feet)+chosen.transform.up*worker.FootOffset);
            if((feet-target).sqrMagnitude<0.0001f)
            {
                if(stage<3){stage++;return;}
                NavalHull landed=chosen.Hull;climbing=false;worker.EndRescue();
                if(landed!=home)
                {
                    if(world.AcceptConvoySurvivor(home,landed,robot,worker))home=landed;
                    else
                    {
                        NavalBoarder boarder=Boarder;if(boarder==null){boarder=gameObject.AddComponent<NavalBoarder>();cachedBoarder=boarder;boarder.Initialize(robot,worker,home);}
                        boarder.AdoptShip(landed);
                    }
                }
                Debug.Log("[NavalPrototype] Sailor boarded rescue ladder: sailor="+robot.GetInstanceID()+" ship="+landed.Root.name+" transferred="+(home==landed));
                chosen=null;
            }
            return;
        }
        if(Time.time>=scanAt||chosen!=null&&!chosen.Usable(side))
        {scanAt=Time.time+0.75f;SelectLadder();}
        Vector3 direction=Vector3.zero;
        if(chosen!=null)
        {
            if(world.ConvoyMission&&chosen.Hull!=home&&
                (chosen.transform.position-robot.transform.position).sqrMagnitude<180f*180f)
                chosen.Hull.RescuePickupUntil=Time.time+1f;
            Vector3 local=chosen.transform.InverseTransformPoint(robot.transform.position);NavalSwimPoint next;
            LadderRoute(chosen,side,local,out next);
            Vector3 destination=chosen.transform.TransformPoint(new Vector3(next.X,local.y,next.Z));
            direction=destination-robot.transform.position;direction.y=0f;
            if(Vector2.Distance(new Vector2(local.x,local.z),new Vector2(chosen.OuterX(side),chosen.Hull.Definition.LadderZ))*chosen.Hull.WorldScale<1.5f)
            {
                climbing=true;stage=0;feet=chosen.transform.InverseTransformPoint(robot.transform.position-chosen.transform.up*worker.FootOffset);
                Debug.Log("[NavalPrototype] Sailor starting rescue climb: sailor="+robot.GetInstanceID()+" ship="+chosen.Hull.Root.name+" side="+side);
                return;
            }
        }
        float sea=world.WaterLevelAt(robot.transform.position);
        float targetY=sea-headOffset+0.2f;
        float lift=Mathf.Clamp((targetY-robot.transform.position.y)*5f,-2f,4f);
        Vector3 step=(Vector3.ClampMagnitude(direction,1f)*Mathf.Min(2.3f,worker.DeckWalkSpeed)+Vector3.up*lift)*dt;
        CharacterController body=worker.BodyController;
        if(body!=null&&body.enabled)body.Move(step);else robot.transform.position+=step;
        if(direction.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(direction),180f*dt);
    }
}

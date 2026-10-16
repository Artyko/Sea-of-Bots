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
    private float scanAt,headOffset,maintenanceSubmergedAt=-1f;
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public void Initialize(Character sailor,NavalRepairSailor repair,NavalHull ship)
    {
        robot=sailor;worker=repair;home=ship;
        FirstPersonMover mover=robot as FirstPersonMover;Transform head=mover!=null?mover.GetHeadTransform():null;
        headOffset=head!=null?head.position.y-robot.transform.position.y:0.8f;
    }
    private void ZeroVelocity()
    {FirstPersonMover mover=robot as FirstPersonMover;if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);}
    public void CheckForWater()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(world==null||!world.SimulationActive||robot==null||!robot.IsAlive()||worker.Rescuing)return;
        NavalBoarder boarder=GetComponent<NavalBoarder>();if(boarder!=null&&boarder.InFlight)return;
        Vector3 foot=robot.transform.position-Vector3.up*worker.FootOffset;
        float depth=world.WaterLevelAt(foot)-foot.y;
        if(depth<0.9f){maintenanceSubmergedAt=-1f;return;}
        RaycastHit support;
        bool standing=Physics.Raycast(foot+Vector3.up*0.25f,Vector3.down,out support,0.55f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        if(depth<worker.FootOffset+headOffset)
        {maintenanceSubmergedAt=-1f;if(standing)return;}
        else if(maintenanceSubmergedAt<0f)maintenanceSubmergedAt=Time.time;
        // A short repair/pump task may finish using the existing 30-second air
        // reserve. Do not interrupt it immediately just because the head dips.
        if(worker.MaintenanceDuty&&InteriorShip(foot)!=null&&standing&&
            (depth<worker.FootOffset+headOffset||maintenanceSubmergedAt>=0f&&Time.time-maintenanceSubmergedAt<10f))return;
        worker.BeginRescue();if(boarder!=null)boarder.SuspendForRescue();scanAt=0f;climbing=false;chosen=null;
        NavalHull interior=InteriorShip(foot);if(interior!=null)BeginHoldEscape(interior);
    }
    private bool Landing(NavalSeaLadder ladder,int which)
    {
        Vector3 point=new Vector3(which==0?-2.2f:2.2f,0.88f,ladder.Hull.Definition.LadderZ);RaycastHit hit;
        Vector3 world=ladder.transform.TransformPoint(point);
        return Physics.Raycast(world+ladder.transform.up*0.4f,-ladder.transform.up,out hit,0.8f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(ladder.transform)&&
            Mathf.Abs(ladder.transform.InverseTransformPoint(hit.point).y-0.825f)<0.2f;
    }
    private void SelectLadder()
    {
        chosen=null;float nearest=float.MaxValue;
        foreach(NavalSeaLadder ladder in NavalSeaLadder.All)
        {
            if(ladder==null)continue;Vector3 local=ladder.transform.InverseTransformPoint(robot.transform.position);
            for(int i=0;i<2;i++)
            {
                if(!ladder.Usable(i)||!Landing(ladder,i))continue;
                NavalSwimPoint next;
                float distance=NavalSwimPath.Route(new NavalSwimPoint(local.x,local.z),new NavalSwimPoint(i==0?-3.7f:3.7f,ladder.Hull.Definition.LadderZ),out next);
                if(distance<nearest){nearest=distance;chosen=ladder;side=i;}
            }
        }
    }
    private void Place(Vector3 position)
    {
        CharacterController controller=robot.GetComponentInChildren<CharacterController>();
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        robot.transform.position=position;if(enabled)controller.enabled=true;ZeroVelocity();
    }
    private void LateUpdate()
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
            if(chosen==null||!chosen.Usable(side)||!Landing(chosen,side))
            {climbing=false;chosen=null;scanAt=0f;return;}
            float sign=side==0?-1f:1f;
            Vector3 target=stage==0?new Vector3(sign*3.7f,feet.y,chosen.Hull.Definition.LadderZ):stage==1?new Vector3(sign*3.7f,chosen.Hull.Definition.AboveDeckY(2.55f),chosen.Hull.Definition.LadderZ):
                stage==2?new Vector3(sign*2.2f,chosen.Hull.Definition.AboveDeckY(2.55f),chosen.Hull.Definition.LadderZ):new Vector3(sign*2.2f,0.86f,chosen.Hull.Definition.LadderZ);
            feet=Vector3.MoveTowards(feet,target,1.3f*dt);Place(chosen.transform.TransformPoint(feet)+chosen.transform.up*worker.FootOffset);
            if((feet-target).sqrMagnitude<0.0001f)
            {
                if(stage<3){stage++;return;}
                NavalHull landed=chosen.Hull;climbing=false;worker.EndRescue();
                if(landed!=home)
                {
                    NavalBoarder boarder=GetComponent<NavalBoarder>();if(boarder==null){boarder=gameObject.AddComponent<NavalBoarder>();boarder.Initialize(robot,worker,home);}
                    boarder.AdoptShip(landed);
                }
                chosen=null;
            }
            return;
        }
        if(Time.time>=scanAt||chosen==null||!chosen.Usable(side))
        {scanAt=Time.time+0.75f;SelectLadder();}
        Vector3 direction=Vector3.zero;
        if(chosen!=null)
        {
            Vector3 local=chosen.transform.InverseTransformPoint(robot.transform.position);NavalSwimPoint next;
            NavalSwimPath.Route(new NavalSwimPoint(local.x,local.z),new NavalSwimPoint(side==0?-3.7f:3.7f,chosen.Hull.Definition.LadderZ),out next);
            Vector3 destination=chosen.transform.TransformPoint(new Vector3(next.X,local.y,next.Z));
            direction=destination-robot.transform.position;direction.y=0f;
            if(local.y> -1.6f&&local.y<2.7f&&Vector2.Distance(new Vector2(local.x,local.z),new Vector2(side==0?-3.7f:3.7f,chosen.Hull.Definition.LadderZ))*chosen.Hull.WorldScale<1.1f)
            {climbing=true;stage=0;feet=chosen.transform.InverseTransformPoint(robot.transform.position-chosen.transform.up*worker.FootOffset);return;}
        }
        float sea=world.WaterLevelAt(robot.transform.position);
        float targetY=sea-headOffset+0.2f;
        float lift=Mathf.Clamp((targetY-robot.transform.position.y)*5f,-2f,4f);
        Vector3 step=(Vector3.ClampMagnitude(direction,1f)*2.3f+Vector3.up*lift)*dt;
        CharacterController body=robot.GetComponentInChildren<CharacterController>();
        if(body!=null&&body.enabled)body.Move(step);else robot.transform.position+=step;
        if(direction.sqrMagnitude>0.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(direction),180f*dt);
    }
}

using UnityEngine;
public partial class NavalRepairSailor
{
    private GameObject accessFrame;
    private Material accessWood;
    private Vector3 accessStand,accessApproach;
    private int accessStage; // 1 approach, 2 up, 3 step aboard, 4 work, 5 step out, 6 down
    private float accessStuck;
    private bool AccessDescending {get{return accessStage>=5;}}
    private bool ClearBody(Vector3 local)
    {
        CharacterController body=Controller();float height=body!=null?Mathf.Max(1.8f,body.bounds.size.y):2.6f;
        Vector3 bottom=boat.TransformPoint(local)+boat.up*.45f,top=boat.TransformPoint(local)+boat.up*(height-.4f);
        return !Physics.CheckCapsule(bottom,top,.38f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
    }
    private Vector3 AccessApproach(Vector3 stand,NavalHullVoxel hole)
    {
        Vector3 direction=stand-hole.LocalCenter;direction.y=0f;
        if(direction.sqrMagnitude<.01f)direction=Vector3.back;
        return new Vector3(stand.x,BoatDimensions.HoldFloor,stand.z)+direction.normalized*.52f;
    }
    private bool RaisedPoint(NavalHullVoxel hole,Vector3 basePoint,out Vector3 point)
    {
        point=basePoint;
        float horizontal=new Vector2(hole.LocalCenter.x-point.x,hole.LocalCenter.z-point.z).magnitude*hull.WorldScale;
        point.y=NavalRepairAccessMath.StandY(hole.LocalCenter.y,hull.WorldScale,horizontal);
        if(float.IsInfinity(point.y)||point.y<=BoatDimensions.HoldFloor+.05f||point.y>.2f)return false;
        Vector3 approach=AccessApproach(point,hole);
        return ClearBody(approach)&&ClearBody(point+Vector3.up*.12f)&&ReachFrom(point,hole);
    }
    private void PrepareAccess(Vector3 stand,NavalHullVoxel hole)
    {
        accessStand=stand;accessApproach=AccessApproach(stand,hole);accessStage=1;accessStuck=0f;
    }
    private void AccessCube(Vector3 center,Vector3 size,bool solid)
    {
        GameObject obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name="Repair step ladder";
        obj.layer=PhysicsManager.GetEnvironmentLayer();obj.transform.SetParent(accessFrame.transform,false);obj.transform.localPosition=center;obj.transform.localScale=size;
        obj.GetComponent<Renderer>().sharedMaterial=accessWood;
        if(!solid){Collider c=obj.GetComponent<Collider>();c.enabled=false;Destroy(c);}
    }
    private void DeployAccess()
    {
        accessFrame=new GameObject("Temporary repair platform");accessFrame.transform.SetParent(boat,false);
        accessWood=new Material(Shader.Find("Unlit/Color"));accessWood.color=new Color(.58f,.4f,.2f);
        float floor=BoatDimensions.HoldFloor,height=accessStand.y-floor;
        AccessCube(new Vector3(accessStand.x,accessStand.y-.04f,accessStand.z),new Vector3(.58f,.08f,.58f),true);
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
            AccessCube(new Vector3(accessStand.x+x*.24f,floor+height*.5f,accessStand.z+z*.24f),new Vector3(.06f,height,.06f),false);
        int steps=Mathf.Max(2,Mathf.CeilToInt(height*hull.WorldScale/.28f));
        Vector3 offset=accessApproach-accessStand;offset.y=0f;
        for(int i=0;i<steps;i++)AccessCube(new Vector3(accessStand.x+offset.x*.8f,floor+(i+1)*height/steps,accessStand.z+offset.z*.8f),new Vector3(.3f,.045f,.12f),false);
        Debug.Log("[NavalPrototype] Repair access deployed at "+accessStand+" for hole "+leak.LocalCenter);
    }
    private void RemoveAccess()
    {
        if(accessFrame!=null){accessFrame.SetActive(false);Destroy(accessFrame);}if(accessWood!=null)Destroy(accessWood);
        accessFrame=null;accessWood=null;accessStage=0;accessStuck=0f;
    }
    private bool TickAccess(float dt)
    {
        if(accessStage==0||accessStage==4||accessStage==1&&route.Count>0)return false;
        if(accessStage==1){DeployAccess();accessStage=2;}
        Vector3 target=accessStage==2||accessStage==5?new Vector3(accessApproach.x,accessStand.y+.12f,accessApproach.z):accessStage==3?accessStand:accessApproach;
        float before=Vector3.Distance(feet,target),oldY=feet.y;
        if(accessStage==2||accessStage==6)
        {
            Vector3 facing=boat.TransformVector(accessStand-accessApproach);facing.y=0f;
            if(facing.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.RotateTowards(robot.transform.rotation,Quaternion.LookRotation(facing),360f*dt);
        }
        Move(target,dt,true);
        if(gait!=null&&(accessStage==2||accessStage==6))gait.Moved(Mathf.Abs(feet.y-oldY)*hull.WorldScale);
        if(Vector3.Distance(feet,target)<(accessStage==5?.06f:.15f))
        {
            accessStuck=0f;
            if(accessStage==2)accessStage=3;
            else if(accessStage==3)accessStage=4;
            else if(accessStage==5)accessStage=6;
            else {RemoveAccess();ContinueRepair();}
        }
        else
        {
            if(before-Vector3.Distance(feet,target)<dt*.05f)accessStuck+=dt;else accessStuck=0f;
            if(accessStuck>4f)
            {
                if(leak!=null)retryAfter[leak]=Time.time+10f;
                RemoveAccess();ContinueRepair();
            }
        }
        return true;
    }
}

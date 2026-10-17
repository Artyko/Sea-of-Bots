using UnityEngine;

public partial class NavalRepairSailor
{
    private int bailState; // 1 approach, 2 scoop, 3 carry, 4 pour
    private float bailTime,bailRetry;
    private Vector3 bailPickup,bailOutlet;
    private readonly NavalBucketLoad bucketLoad=new NavalBucketLoad();
    private NavalBucketVisual bucket;
    public float BucketWater {get{return bucketLoad.Amount;}}
    private string BailStatus {get{return bailState==1?"Going to water":bailState==2?"Scooping water":bailState==3?"Carrying bucket":"Pouring overboard";}}
    private void BailRoute(Vector3 goal)
    {
        route.Clear();stuck=0f;
        PlanTo(goal);
    }
    private bool StartBailing()
    {
        if(!hull.CombatCritical||hull.Flood.Total<0.12f||Time.time<bailRetry)return false;
        float best=-1f;Vector3 pickup=Vector3.zero;
        for(int i=0;i<hull.Flood.Water.Length;i++)
        {
            float depth=hull.Flood.Depth(i);if(depth<0.015f)continue;
            Vector3 point=new Vector3(hull.Flood.X(i)<0f?-2f:2f,hull.Floor,Mathf.Clamp(hull.Flood.Z(i),-5f,5f));
            if(Mathf.Abs(point.x-hull.Flood.X(i))>1.2f)continue;
            float score=depth/(1f+Vector3.Distance(feet,point));
            if(score<=best)continue;
            Vector3 bottom=boat.TransformPoint(point)+boat.up*0.5f;
            if(Physics.CheckCapsule(bottom,bottom+boat.up*1.3f,0.38f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))continue;
            if(score>best){best=score;pickup=point;}
        }
        if(best<0f){bailRetry=Time.time+3f;return false;}
        bailPickup=pickup;bailOutlet=new Vector3(pickup.x<0f?-2.05f:2.05f,0.825f,2.6f);
        waitingForRepair=false;returning=false;changingPost=false;bailState=1;bailTime=0f;BailRoute(pickup);
        if(bucket==null){bucket=gameObject.AddComponent<NavalBucketVisual>();bucket.Initialize();}
        if(hammer!=null)hammer.gameObject.SetActive(false);
        return true;
    }
    private bool TickBailing(float dt,bool allowed)
    {
        // Start no new bucket trips outside the shared critical state. A full
        // bucket already being carried is poured out before resuming other work.
        if(!hull.CombatCritical&&BucketWater<=0f)
        {
            if(bailState!=0){CancelBailing();route.Clear();BeginReturn();}
            return false;
        }
        if(bailState==0)
        {
            if(!allowed||leak!=null)return false;
            bool otherRepair=false;
            foreach(NavalRepairSailor worker in hull.RepairWorkers)if(worker!=null&&worker!=this&&worker.RepairingLeak)otherRepair=true;
            bool urgent=hull.CombatCritical&&otherRepair;
            if(!urgent)
            {
                // Give repair jobs first choice; accepted patches retain their priority.
                if(Time.time<scanAt&&hull.HasActiveLeaks)return false;
                TryFindLeak();if(leak!=null)return false;
            }
            feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
            if(!StartBailing())return false;
        }
        robot.DisableAI();if(route.Count==0)HoldPosition();
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,4f);
            if(movement==RouteResult.Blocked||movement==RouteResult.Fell)
            {CancelBailing();route.Clear();bailRetry=Time.time+8f;BeginReturn();return true;}
        }
        else
        {
            Walking(false,dt);bailTime+=dt;
            if(bailState==1){bailState=2;bailTime=0f;}
            else if(bailState==2&&bailTime>=1.2f)
            {
                bucketLoad.Scoop(hull.Flood,bailPickup.x,bailPickup.z,NavalBucketLoad.DefaultCapacity,1.5f);
                if(BucketWater<=0f){CancelBailing();BeginReturn();return true;}
                bailState=3;bailTime=0f;BailRoute(bailOutlet);
            }
            else if(bailState==3){bailState=4;bailTime=0f;}
            else if(bailState==4&&bailTime>=1.5f)
            {
                bucketLoad.Dump();CancelBailing();route.Clear();returning=false;
                // Look for another leak or bucket immediately from the current position.
                if(!TickBailing(0f,allowed)&&leak==null)BeginReturn();return true;
            }
        }
        if(bucket!=null)
        {
            Vector3 held=feet+Vector3.up*0.8f+boat.InverseTransformDirection(robot.transform.forward)*0.55f;
            float tilt=0f;
            if(bailState==2)held.y=feet.y+0.15f+0.65f*Mathf.Abs(Mathf.Cos(bailTime/1.2f*Mathf.PI));
            if(bailState==4)
            {
                float t=Mathf.Clamp01(bailTime/0.6f),side=bailOutlet.x<0f?-1f:1f;
                held=Vector3.Lerp(held,new Vector3(side*3.35f,2.4f,bailOutlet.z),t);tilt=side*110f*Mathf.Clamp01((bailTime-0.5f)/0.4f);
            }
            bucket.Pose(boat,held,tilt,BucketWater>0f,bailState==4&&bailTime>0.8f);
        }
        return true;
    }
    private void CancelBailing()
    {
        if(BucketWater>0f&&hull!=null)bucketLoad.Return(hull.Flood,feet.x,feet.z);
        bucketLoad.Dump();bailState=0;
        if(bucket!=null)bucket.Hide();
        if(hammer!=null)hammer.gameObject.SetActive(true);
    }
}

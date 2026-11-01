using UnityEngine;
public partial class NavalRepairSailor
{
    private NavalFireTarget fireJob;
    private Vector3 fireStand,fireSource;
    private float fireTime,fireRetry,fireWater,fireElapsed;
    private int fireStage;
    private int fireCursor,fireReplans;
    private float firePriorityCheck;
    private NavalFireTarget priorityAmmunitionFire;
    private static readonly float[] fireWorkCorridors={1.35f,.9f,1.8f};
    public bool FightingFire {get{return fireJob!=null;}}
    public bool OwnsAmmunitionFire(NavalFireTarget target)
    {return target!=null&&NavalAmmunitionFireAssignment.KeepOwner(priorityAmmunitionFire==target,target.Claim==this,target.Active);}
    public bool AvailableForAmmunitionFire(NavalFireTarget target)
    {
        bool interruptingOrdinary=fireJob!=null&&fireJob.Powder==null;
        return target!=null&&target.Active&&target.Powder!=null&&(CanReceiveSupport(true)||interruptingOrdinary);
    }
    public bool AssignAmmunitionFire(NavalFireTarget target)
    {
        bool interruptingOrdinary=fireJob!=null&&fireJob.Powder==null;
        if(!AvailableForAmmunitionFire(target)||target.Claim!=null&&target.Claim!=this)return false;
        if(interruptingOrdinary)
        {
            if(fireJob.Claim==this)fireJob.Claim=null;
            fireJob=null;fireStage=0;route.Clear();stuck=0f;
        }
        priorityAmmunitionFire=target;target.Claim=this;FireAssignment=true;return true;
    }
    public bool RelinquishAmmunitionFire(NavalFireTarget target)
    {
        if(target==null||target.Claim!=this)return false;
        // CancelFire clears the current fireJob claim, but a generic owner can
        // still have only the priority reservation. Release the explicit
        // target first so the replacement can claim it in this same review.
        target.Claim=null;
        CancelFire();route.Clear();stuck=0f;fireRetry=Time.time+.5f;
        // The displaced generic firefighter is immediately available for a
        // different flame, while the gun owner takes the live fuse.
        nextTaskPending=true;return true;
    }
    public void ClearAmmunitionFireAssignment()
    {
        // Ownership is sticky for the lifetime of the fuse. Combat station
        // allocation can change every support review; it must not transfer an
        // already responding gunner's target to each newly assigned gunner.
        if(OwnsAmmunitionFire(priorityAmmunitionFire)){FireAssignment=true;return;}
        if(priorityAmmunitionFire!=null&&priorityAmmunitionFire!=fireJob&&priorityAmmunitionFire.Claim==this)priorityAmmunitionFire.Claim=null;
        priorityAmmunitionFire=null;
    }
    private string FireStatus {get{return fireStage==1?"Getting firefighting water":fireStage==2?"Filling fire bucket":fireStage==3?"Going to fire":"Extinguishing fire";}}
    private bool FireReach(Vector3 stand,NavalFireTarget target)
    {
        Vector3 hand=boat.TransformPoint(stand)+boat.up*1.4f,point=boat.TransformPoint(target.Point),delta=point-hand;
        if(delta.sqrMagnitude>36)return false;
        int count;
        while(true)
        {
            count=Physics.RaycastNonAlloc(hand,delta.normalized,repairSightHits,delta.magnitude,
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<repairSightHits.Length)break;System.Array.Resize(ref repairSightHits,repairSightHits.Length*2);
        }
        bool clear=true;
        for(int i=0;i<count;i++)
        {
            var hit=repairSightHits[i];if(hit.collider.transform.IsChildOf(robot.transform))continue;
            // The burning board/barrel is itself solid; that endpoint is the intended target.
            if((hit.point-point).sqrMagnitude<.81f||NavalVoxelHit.Resolve(hit.collider,hit.point)==target.Cell)continue;
            clear=false;
        }
        System.Array.Clear(repairSightHits,0,count);return clear;
    }
    private bool FireWorkPoint(NavalFireTarget target,out Vector3 stand)
    {
        // Bow fires are attacked from the open rear quarter of the semi-open
        // main mount. Generic offsets put the sailor inside its side shield.
        if(hull.Definition.SteamPowered&&target.Point.z>3.55f)
        {
            float side=target.Point.x<0?-1f:1f;
            stand=new Vector3(side*1.35f,hull.Definition.WeatherDeck,3.40f);
            if(FireReach(stand,target)&&ClearBody(stand)&&Supported(stand))return true;
        }
        float workDeck=NavalFireWorkDeck.ForTarget(target.Point.y,hull.Definition);
        for(int deck=0;deck<hull.Definition.DeckCount;deck++)
        {
            float floor=deck==0?hull.Definition.WeatherDeck:deck==1?hull.Floor:.825f;
            if(Mathf.Abs(floor-workDeck)>.01f)continue;
            for(int sideIndex=0;sideIndex<2;sideIndex++)for(int corridor=0;corridor<fireWorkCorridors.Length;corridor++)for(int step=-1;step<=1;step++)
            {
                float nearSide=target.Point.x<0?-1f:1f;float side=sideIndex==0?nearSide:-nearSide;
                int offset=(step+1+fireReplans)%3-1;
                stand=new Vector3(side*fireWorkCorridors[corridor],floor,Mathf.Clamp(target.Point.z+offset*.55f,-5.1f,5.1f));
                if(FireReach(stand,target)&&ClearBody(stand)&&Supported(stand))return true;
            }
        }
        stand=Vector3.zero;return false;
    }
    private void CancelFire()
    {
        if(fireJob!=null&&fireJob.Claim==this)fireJob.Claim=null;
        if(fireWater>0&&hull!=null&&feet.y<hull.Definition.WeatherDeck)hull.Flood.Add(hull.Flood.Index(feet.x,feet.z),fireWater);
        fireWater=0;fireJob=null;fireStage=0;priorityAmmunitionFire=null;
        if(bucket!=null)bucket.Hide();ShowHammer(true);
    }
    private bool TickFire(float dt,bool allowed)
    {
        NavalFireSystem system=hull.Fire;if(system==null)return false;
        if(fireJob!=null&&fireJob.Powder==null&&Time.time>=firePriorityCheck)
        {
            firePriorityCheck=Time.time+1f;
            if(system.UnclaimedPowder)
            {
                for(int i=0;i<system.TargetCount;i++)
                {
                    var target=system.TargetAt(i);Vector3 stand;
                    if(target.Powder==null||!target.Active||!system.CanClaim(target,this)||!FireWorkPoint(target,out stand))continue;
                    fireJob.Claim=null;fireJob=target;target.Claim=this;fireStand=stand;fireElapsed=0;
                    if(fireWater>0){route.Clear();PlanTo(fireStand);fireStage=3;fireTime=0;}
                    break;
                }
            }
        }
        if(fireJob!=null&&(!fireJob.Active||hull.CombatCritical&&!system.Urgent))
        {CancelFire();route.Clear();BeginReturn();return false;}
        if(fireJob==null)
        {
            bool ammunitionEmergency=priorityAmmunitionFire!=null&&priorityAmmunitionFire.Active;
            if(!FireAssignment||!ammunitionEmergency&&!system.MayAssign(this))return false;
            bool interrupt=ammunitionEmergency; // The gun owner leaves the firing post immediately when its ammunition lights.
            if((!allowed&&!interrupt)||leak!=null||pumpDuty&&!interrupt||bailState!=0||FinalAttackDuty||Time.time<fireRetry)return false;
            // Do not steal the only active leak worker. An unassigned hand can still fight a fuse.
            if(!system.Urgent&&NavalMaintenancePriority.DeferOrdinaryFire(hull.HasRepairDemand,OtherLeakRepairer()))return false;
            fireRetry=Time.time+1f;feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);
            if(ammunitionEmergency)
            {
                Vector3 priorityStand;
                if(system.CanClaim(priorityAmmunitionFire,this)&&FireWorkPoint(priorityAmmunitionFire,out priorityStand))
                {fireJob=priorityAmmunitionFire;fireStand=priorityStand;}
                else
                {if(priorityAmmunitionFire.Claim==this)priorityAmmunitionFire.Claim=null;priorityAmmunitionFire=null;ammunitionEmergency=false;}
            }
            int checkedTargets=0;
            for(int pass=0;pass<2&&fireJob==null&&checkedTargets<4;pass++)for(int step=0;step<system.TargetCount&&checkedTargets<4;step++)
            {
                int i=(fireCursor+step)%system.TargetCount;
                var target=system.TargetAt(i);
                if(!target.Active||!system.CanClaim(target,this)||(pass==0)!=(target.Powder!=null))continue;
                checkedTargets++;
                Vector3 stand;if(!FireWorkPoint(target,out stand))continue;
                fireJob=target;fireStand=stand;target.Claim=this;break;
            }
            fireCursor+=4;
            if(fireJob==null)return false;
            Debug.Log("[NavalPrototype] Firefighter assigned: ship="+hull.Root.name+" sailor="+GetInstanceID()+" target="+fireJob.Point+" stand="+fireStand+" powder="+(fireJob.Powder!=null)+" owningGunner="+ammunitionEmergency);
            if(interrupt)ReleasePump();
            waitingForRepair=returning=changingPost=false;route.Clear();stuck=0;fireStage=fireWater>0?3:1;fireTime=fireElapsed=0;
            fireReplans=0;rejectedFireSources.Clear();
            if(fireWater>0)PlanTo(fireStand);
            else if(!PlanFireWater()){Debug.Log("[NavalPrototype] Firefighter has no reachable water: ship="+hull.Root.name+" sailor="+GetInstanceID());CancelFire();fireRetry=Time.time+2;return false;}
            if(bucket==null){bucket=gameObject.AddComponent<NavalBucketVisual>();bucket.Initialize();}
            ShowHammer(false);
        }
        fireElapsed+=dt;
        if(fireElapsed>35f){CancelFire();route.Clear();fireRetry=Time.time+5;BeginReturn();return true;}
        robot.DisableAI();
        if(route.Count>0)
        {
            // A fire stand is a line-of-sight work area, not a pin-sized
            // station. If a gunner or shield stops the capsule slightly short
            // but the flames are already reachable, start dousing instead of
            // pushing for eight seconds and throwing the full bucket away.
            if(fireStage==3&&route.Count==1&&FireReach(feet,fireJob))route.Clear();
        }
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,4f);
            if(movement==RouteResult.Fell)
            {stranded=true;CancelFire();route.Clear();return true;}
            if(movement==RouteResult.Blocked)
            {
                feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);stuck=0;
                if(++fireReplans<=3)
                {
                    if(fireStage<=2){rejectedFireSources.Add(fireSource);if(PlanFireWater())return true;}
                    Vector3 stand;if(fireStage>=3&&FireWorkPoint(fireJob,out stand))
                    {fireStand=stand;route.Clear();PlanTo(stand);return true;}
                }
                CancelFire();route.Clear();fireRetry=Time.time+2;BeginReturn();return true;
            }
        }
        else
        {
            HoldPosition();Walking(false,dt);fireTime+=dt;
            if(fireStage==1){fireStage=2;fireTime=0;}
            else if(fireStage==2&&fireTime>=1.2f)
            {
                if(!FillFireWater())
                {rejectedFireSources.Add(fireSource);if(!PlanFireWater()){CancelFire();BeginReturn();}return true;}
                fireStage=3;fireTime=0;route.Clear();PlanTo(fireStand);
            }
            else if(fireStage==3){fireStage=4;fireTime=0;}
            else if(fireStage==4&&fireTime>=.8f)
            {
                Vector3 point=boat.TransformPoint(fireJob.Point),hand=robot.transform.position+Vector3.up*.4f;
                bool reached=Vector3.Distance(hand,point)<6f&&FireReach(feet,fireJob);
                // The main mount can train across a previously clear hose line.
                // Keep the filled bucket and move to another supported point
                // instead of pouring into the shield or through the deck.
                if(!reached&&++fireReplans<=3)
                {
                    Vector3 alternative;
                    if(FireWorkPoint(fireJob,out alternative))
                    {fireStand=alternative;route.Clear();PlanTo(alternative);fireStage=3;fireTime=0;return true;}
                }
                if(reached)system.Douse(point,fireWater);
                else if(feet.y<hull.Definition.WeatherDeck)hull.Flood.Add(hull.Flood.Index(feet.x,feet.z),fireWater);
                Debug.Log("[NavalPrototype] Firefighter poured: ship="+hull.Root.name+" sailor="+GetInstanceID()+" target="+fireJob.Point+" litres="+fireWater.ToString("F2")+" reached="+reached);
                fireWater=0;CancelFire();fireRetry=0;route.Clear();BeginReturn();return true;
            }
        }
        Vector3 held=feet+Vector3.up*(.8f/hull.WorldScale)+boat.InverseTransformDirection(robot.transform.forward)*(.55f/hull.WorldScale);
        if(fireStage==2)held=fireSource+new Vector3(fireSourceCell<0?Mathf.Sign(fireSource.x)*.7f:0,fireSourceCell<0?-.2f:.12f/hull.WorldScale,0);
        if(fireStage==4&&fireJob!=null)held=Vector3.Lerp(held,fireJob.Point,Mathf.Clamp01(fireTime/.8f)*.5f);
        bucket.Pose(boat,held,fireStage==4?70f:0f,fireWater>0,fireStage==4);
        return true;
    }
}

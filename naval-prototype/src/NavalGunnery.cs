using UnityEngine;

public partial class NavalStations
{
    private static readonly NavalShipRegistry<Transform,NavalStations> ships=new NavalShipRegistry<Transform,NavalStations>();
    private void RegisterShip(){ships.Register(boat,this);}
    private void UnregisterShip(){if(!object.ReferenceEquals(boat,null))ships.Remove(boat,this);}
    private Transform[] aimShip;
    private Vector3[] aimLocal;
    private Vector2[] shotError;
    private int[] aimKind,lastKind;
    private float[] aimRetry,visibilityAt;
    private bool[] visibilityCache;
    private NavalHullVoxel[] aimCell;
    private void InitializeGunnery(int count)
    {
        aimShip=new Transform[count];aimLocal=new Vector3[count];shotError=new Vector2[count];
        visibilityAt=new float[count];visibilityCache=new bool[count];
        aimKind=new int[count];lastKind=new int[count];aimRetry=new float[count];aimCell=new NavalHullVoxel[count];
        for(int i=0;i<count;i++){aimKind[i]=lastKind[i]=-1;}
    }
    private bool TryAimSolution(int gun,Vector3 target,out float wantedYaw,out float angle)
    {
        wantedYaw=angle=0f;
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
        Vector3 delta=target-muzzle,flat=delta;flat.y=0f;float elevation;
        if(!NavalBattleMath.FiringAngle(flat.magnitude,delta.y,32f,out elevation))return false;
        // Gravity is world vertical even when this ship rolls or pitches.
        Vector3 launch=flat.normalized*Mathf.Cos(elevation*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(elevation*Mathf.Deg2Rad);
        Vector3 local=boat.InverseTransformDirection(launch);
        wantedYaw=Mathf.DeltaAngle(gun%2==0?-90f:90f,Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg);
        angle=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
        return Mathf.Abs(wantedYaw)<=30f&&angle>=-5f&&angle<=45f;
    }
    private Vector3 Lead(int gun,Vector3 point,Vector3 velocity)
    {
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
        Vector3 predicted=point;
        for(int i=0;i<3;i++)
        {
            Vector3 delta=predicted-muzzle;float horizontal=new Vector2(delta.x,delta.z).magnitude,angle;
            if(!NavalBattleMath.FiringAngle(horizontal,delta.y,32f,out angle))break;
            predicted=point+velocity*NavalGunneryMath.FlightTime(horizontal,angle);
        }
        return predicted;
    }
    private bool ReachableLead(int gun,Vector3 point,Vector3 velocity,out Vector3 predicted)
    {
        predicted=Lead(gun,point,velocity);float y,p;
        if(TryAimSolution(gun,predicted,out y,out p))return true;
        // Near maximum range, exact interception may be impossible while the
        // ship itself is in range. Use the greatest reachable lead instead.
        Vector3 full=predicted;predicted=point;
        if(!TryAimSolution(gun,point,out y,out p))return false;
        float low=0f,high=1f;
        for(int i=0;i<10;i++)
        {
            float t=(low+high)*.5f;Vector3 probe=Vector3.Lerp(point,full,t);
            if(TryAimSolution(gun,probe,out y,out p)){low=t;predicted=probe;}else high=t;
        }
        return true;
    }
    private Vector3 ShotDirection(int gun,Vector3 direction)
    {
        if(firingRobot==null)return direction;
        Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
        return Quaternion.AngleAxis(shotError[gun].x,Vector3.up)*Quaternion.AngleAxis(shotError[gun].y,right)*direction;
    }
    private bool VisibleMechanism(int gun,NavalStations victim,int station,NavalHullVoxel cell)
    {
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
        Vector3 view=victim.boat.InverseTransformPoint(muzzle);
        if(!NavalTargetVisibility.Broadside(view.x,view.z))return false;
        Vector3 offset=cell.transform.position-muzzle;
        RaycastHit[] hits=Physics.RaycastAll(muzzle,offset.normalized,offset.magnitude+.05f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits,delegate(RaycastHit a,RaycastHit b){return a.distance.CompareTo(b.distance);});
        foreach(RaycastHit hit in hits)
        {
            if(hit.collider.transform.IsChildOf(boat))continue;
            NavalHullVoxel first=hit.collider.GetComponent<NavalHullVoxel>();
            return first!=null&&victim.mechanisms[station].Contains(first)&&!first.Missing&&!first.Detached;
        }
        return false;
    }
    private bool LockedTargetVisible(int gun,Transform target)
    {
        // During reload reuse LOS briefly; recheck every ready frame before firing.
        if(Time.time<readyAt[gun]&&Time.time<visibilityAt[gun])return visibilityCache[gun];
        visibilityCache[gun]=ComputeLockedTargetVisible(gun,target);visibilityAt[gun]=Time.time+.2f;
        return visibilityCache[gun];
    }
    private bool ComputeLockedTargetVisible(int gun,Transform target)
    {
        if(aimKind[gun]!=1&&aimKind[gun]!=3)return true;
        NavalStations victim;if(!ships.TryGet(target,out victim)||victim==null||aimCell[gun]==null)return false;
        int station=0;
        if(aimKind[gun]==1)for(int i=1;i<=victim.GunCount;i++)if(victim.mechanisms[i].Contains(aimCell[gun])){station=i;break;}
        return victim.Functional(station)&&victim.mechanisms[station].Contains(aimCell[gun])&&VisibleMechanism(gun,victim,station,aimCell[gun]);
    }
    private bool NearMast(float z)
    {foreach(float mast in hull.Definition.MastZ)if(Mathf.Abs(z-mast)<.35f)return true;return false;}
    private bool Feature(int gun,NavalStations victim,int kind,out NavalHullVoxel selected)
    {
        selected=null;float best=float.MaxValue;
        if(kind==1||kind==3)
        {
            int targetSide=victim.TargetSide(boat.position),preferred=targetSide+(victim.GunCount>2&&Random.value>.5f?2:0);
            int first=kind==3?0:targetSide,last=kind==3?0:victim.GunCount;
            for(int station=first;station<=last;station+=2)
            {
                if(!victim.Functional(station))continue;
                foreach(NavalHullVoxel cell in victim.mechanisms[station])
                    if(cell!=null&&!cell.Missing&&!cell.Detached)
                    {
                        float distance=(cell.transform.position-boat.position).sqrMagnitude*(station==preferred?.7f:1f);
                        if(distance<best&&VisibleMechanism(gun,victim,station,cell)){best=distance;selected=cell;}
                    }
            }
            return selected!=null;
        }
        float along=Random.Range(-4.8f,4.8f),height=Random.Range(1.5f,6.5f);
        float side=victim.boat.InverseTransformPoint(boat.position).x<0f?-1f:1f;
        foreach(NavalHullVoxel cell in victim.hull.Voxels)
        {
            if(cell==null||cell.Missing||cell.Detached)continue;
            Vector3 p=cell.LocalCenter;float score;
            if(kind==0)
            {
                if(!cell.HydraulicShell||p.x*side<BoatDimensions.HalfWidth-.3f)continue;
                Vector3 world=cell.transform.position;
                float water=ocean.Height(world.x,world.z)+.12f;
                score=Mathf.Abs(world.y-water)*5f+Mathf.Abs(p.z-along);
            }
            else
            {
                if(!cell.ManagedRig||cell.RigFabric||Mathf.Abs(p.x)>.3f||!victim.NearMast(p.z)||p.y<1.1f)continue;
                score=Mathf.Abs(p.y-height);
            }
            if(score<best){best=score;selected=cell;}
        }
        return selected!=null;
    }
    private bool SelectAim(int gun,Transform target,Vector3 velocity)
    {
        NavalStations victim;if(!ships.TryGet(target,out victim)||victim==null)return false;
        bool[] available=new bool[4];NavalHullVoxel[] cells=new NavalHullVoxel[4];
        for(int kind=0;kind<4;kind++)
        {
            if(!Feature(gun,victim,kind,out cells[kind]))continue;
            Vector3 predicted;
            available[kind]=ReachableLead(gun,cells[kind].transform.position,velocity,out predicted);
        }
        int chosen=NavalGunneryMath.Choose(Random.value,lastKind[gun],available);
        if(chosen<0)
        {
            // At the edge of range the randomly chosen feature may be farther
            // away than the nearest waterline plank. Try reachable hull cells.
            float nearest=float.MaxValue;
            foreach(NavalHullVoxel cell in victim.hull.Voxels)
            {
                if(cell==null||cell.Missing||cell.Detached||!cell.HydraulicShell)continue;
                Vector3 world=cell.transform.position;
                float height=world.y-ocean.Height(world.x,world.z);
                if(height<-.3f||height>1.5f)continue;
                float candidateDistance=(world-boat.position).sqrMagnitude;if(candidateDistance>=nearest)continue;
                Vector3 predicted;if(!ReachableLead(gun,world,velocity,out predicted))continue;
                nearest=candidateDistance;cells[0]=cell;chosen=0;
            }
            if(chosen<0)return false;
        }
        aimKind[gun]=chosen;aimCell[gun]=cells[chosen];aimShip[gun]=target;visibilityAt[gun]=0f;
        aimLocal[gun]=target.InverseTransformPoint(cells[chosen].transform.position);
        // Draw once per shot, so aiming stays stable while the cannon reloads.
        float distance=Vector3.Distance(boat.position,target.position);
        shotError[gun]=Random.insideUnitCircle*(Mathf.Atan2(NavalGunneryMath.Spread(distance),Mathf.Max(1f,distance))*Mathf.Rad2Deg);
        return true;
    }
    public bool AimAtShipAndFire(int gun,Transform target,Vector3 velocity,float dt,Character sailor)
    {
        if(target==null||!Functional(gun+1)||PlayerStation==gun+1)return false;
        if(aimShip[gun]!=target||aimCell[gun]==null||aimCell[gun].Missing||aimCell[gun].Detached||!LockedTargetVisible(gun,target))
        {
            aimShip[gun]=null;
            if(Time.time<aimRetry[gun]||!SelectAim(gun,target,velocity))
            {if(Time.time>=aimRetry[gun])aimRetry[gun]=Time.time+.35f;return false;}
        }
        Vector3 point;
        if(!ReachableLead(gun,target.TransformPoint(aimLocal[gun]),velocity,out point))
        {
            // A target can move out of the gun arc. Reconsider reachable parts.
            aimShip[gun]=null;aimRetry[gun]=Time.time;return false;
        }
        if(!AimAndFire(gun,point,dt,sailor))return false;
        lastKind[gun]=aimKind[gun];aimShip[gun]=null;aimRetry[gun]=0f;
        Debug.Log("[NavalPrototype] AI target: "+new string[]{"waterline","cannon","mast","wheel"}[lastKind[gun]]);
        return true;
    }
}

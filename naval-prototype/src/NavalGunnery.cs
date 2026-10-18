using UnityEngine;
using System.Collections.Generic;

public partial class NavalStations
{
    private static readonly NavalShipRegistry<Transform,NavalStations> ships=new NavalShipRegistry<Transform,NavalStations>();
    public static NavalStations ForShip(Transform ship)
    {NavalStations controls;return ship!=null&&ships.TryGet(ship,out controls)?controls:null;}
    private void RegisterShip(){ships.Register(boat,this);}
    private void UnregisterShip(){if(!object.ReferenceEquals(boat,null))ships.Remove(boat,this);}
    private static readonly string[] targetNames={"waterline","cannon","mast","wheel"};
    private Transform[] aimShip;
    private Vector3[] aimLocal;
    private Vector2[] shotError;
    private int[] aimKind,lastKind;
    private float[] aimRetry,visibilityAt;
    private bool[] visibilityCache;
    private NavalHullVoxel[] aimCell;
    private readonly bool[] featureAvailable=new bool[4];
    private readonly NavalHullVoxel[] featureCells=new NavalHullVoxel[4];
    private readonly List<NavalHullVoxel> shellTargets=new List<NavalHullVoxel>(),mastTargets=new List<NavalHullVoxel>();
    private int targetIndexCount=-1;
    private readonly List<NavalHullVoxel> rigTargets=new List<NavalHullVoxel>();
    private readonly NavalHullVoxel[] hullProbes=new NavalHullVoxel[8];
    private readonly float[] hullProbeScores=new float[8];
    private static RaycastHit[] sightHits=new RaycastHit[64];
    private struct FeatureCandidate {public NavalHullVoxel Cell;public int Station;public float Score;}
    private sealed class CandidateOrder:IComparer<FeatureCandidate>
    {public int Compare(FeatureCandidate a,FeatureCandidate b){return a.Score.CompareTo(b.Score);}}
    private static readonly CandidateOrder candidateOrder=new CandidateOrder();
    private readonly List<FeatureCandidate> featureCandidates=new List<FeatureCandidate>();
    private void PrepareTargetIndex()
    {
        if(targetIndexCount==hull.Voxels.Count)return;
        targetIndexCount=hull.Voxels.Count;shellTargets.Clear();mastTargets.Clear();rigTargets.Clear();
        foreach(NavalHullVoxel cell in hull.Voxels)
        {
            if(cell.HydraulicShell)shellTargets.Add(cell);
            if(cell.ManagedRig)rigTargets.Add(cell);
            Vector3 p=cell.LocalCenter;
            if(cell.ManagedRig&&!cell.RigFabric&&Mathf.Abs(p.x)<=.3f&&NearMast(p.z)&&p.y>=1.1f)mastTargets.Add(cell);
        }
    }
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
    // Range feasibility only; yaw acquisition is the helmsman's separate responsibility.
    public bool CanReachRange(Vector3 point)
    {
        for(int gun=0;gun<GunCount;gun++)if(Functional(gun+1))
        {
            Vector3 delta=point-barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
            if(NavalEngagementRange.BallisticReach(new Vector2(delta.x,delta.z).magnitude,delta.y))return true;
        }
        return false;
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
    private NavalHullVoxel FirstVisibleCell(int gun,NavalHullVoxel target,bool rigTriggers=false)
    {
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f)),delta=target.transform.position-muzzle;
        int count;
        while(true)
        {
            count=Physics.RaycastNonAlloc(muzzle,delta.normalized,sightHits,delta.magnitude+.08f,PhysicsManager.GetEnvironmentLayerMask(),rigTriggers?QueryTriggerInteraction.Collide:QueryTriggerInteraction.Ignore);
            if(count<sightHits.Length)break;System.Array.Resize(ref sightHits,sightHits.Length*2);
        }
        int nearest=-1;float distance=float.PositiveInfinity;
        for(int i=0;i<count;i++)if(sightHits[i].collider!=null&&!sightHits[i].collider.transform.IsChildOf(boat)&&sightHits[i].distance<distance)
        {nearest=i;distance=sightHits[i].distance;}
        NavalHullVoxel first=nearest>=0?NavalVoxelHit.Resolve(sightHits[nearest].collider,sightHits[nearest].point):null;
        System.Array.Clear(sightHits,0,count);return first;
    }
    private bool AboveWater(NavalHullVoxel cell)
    {Vector3 p=cell.transform.position;return p.y>=ocean.Height(p.x,p.z)+.03f;}
    private bool HullVisible(int gun,NavalStations victim)
    {
        victim.PrepareTargetIndex();
        for(int i=0;i<8;i++){hullProbes[i]=null;hullProbeScores[i]=float.PositiveInfinity;}
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
        Matrix4x4 hullMatrix=victim.boat.localToWorldMatrix;
        foreach(NavalHullVoxel cell in victim.shellTargets)
        {
            if(cell==null||cell.Missing||cell.Detached)continue;
            int bin=(cell.LocalCenter.x<0?0:4)+Mathf.Clamp((int)((cell.LocalCenter.z+6f)/3f),0,3);
            Vector3 world=hullMatrix.MultiplyPoint3x4(cell.LocalCenter);
            float score=-world.y*1000f+(world-muzzle).sqrMagnitude*.001f;
            if(score<hullProbeScores[bin]){hullProbeScores[bin]=score;hullProbes[bin]=cell;}
        }
        for(int i=0;i<8;i++)if(hullProbes[i]!=null&&AboveWater(hullProbes[i]))
        {
            NavalHullVoxel first=FirstVisibleCell(gun,hullProbes[i]);
            if(first!=null&&first.Owner==victim.hull&&!first.ManagedRig&&AboveWater(first))return true;
        }
        return false;
    }
    private bool OnlyRigVisible(int gun,NavalStations victim)
    {
        if(HullVisible(gun,victim))return false;
        NavalHullVoxel unused;
        return !Feature(gun,victim,1,out unused)&&!Feature(gun,victim,3,out unused);
    }
    private bool RigFeature(int gun,NavalStations victim,Vector3 velocity,out NavalHullVoxel selected)
    {
        selected=null;float best=float.PositiveInfinity;
        for(int i=0;i<8;i++){hullProbes[i]=null;hullProbeScores[i]=float.PositiveInfinity;}
        foreach(NavalHullVoxel cell in victim.rigTargets)
        {
            if(cell==null||cell.Missing||cell.Detached)continue;
            Vector3 world=cell.transform.position;
            int bin=(cell.RigFabric?4:0)+(cell.LocalCenter.z<0?2:0)+(cell.LocalCenter.y>4?1:0);
            float score=-world.y*1000f+(world-boat.position).sqrMagnitude*.001f;
            if(score<hullProbeScores[bin]){hullProbes[bin]=cell;hullProbeScores[bin]=score;}
        }
        for(int i=0;i<8;i++)
        {
            NavalHullVoxel cell=hullProbes[i];if(cell==null||!AboveWater(cell))continue;
            float score=(cell.transform.position-boat.position).sqrMagnitude;if(score>=best)continue;
            Vector3 predicted;if(!ReachableLead(gun,cell.transform.position,velocity,out predicted))continue;
            NavalHullVoxel first=FirstVisibleCell(gun,cell,true);
            if(first==null||first.Owner!=victim.hull||!first.ManagedRig||!AboveWater(first))continue;
            selected=cell;best=score;
        }
        return selected!=null;
    }
    private bool VisibleMechanism(int gun,NavalStations victim,int station,NavalHullVoxel cell)
    {
        if(!AboveWater(cell))return false;
        Vector3 muzzle=barrels[gun].TransformPoint(new Vector3(0f,0f,1.38f));
        Vector3 view=victim.boat.InverseTransformPoint(muzzle);
        if(!NavalTargetVisibility.Broadside(view.x,view.z))return false;
        Vector3 offset=cell.transform.position-muzzle;
        int count;
        while(true)
        {
            count=Physics.RaycastNonAlloc(muzzle,offset.normalized,sightHits,offset.magnitude+.05f,
                PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore);
            if(count<sightHits.Length)break;
            sightHits=new RaycastHit[sightHits.Length*2];
        }
        int nearest=-1;float distance=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            RaycastHit hit=sightHits[i];
            if(hit.collider.transform.IsChildOf(boat)||hit.distance>=distance)continue;
            distance=hit.distance;nearest=i;
        }
        if(nearest>=0)
        {
            // Merged boxes contain fixed hull only; moving/repairable mechanisms are excluded.
            // An obstruction from this set cannot be the selected cannon or helm.
            if(sightHits[nearest].collider.GetComponent<NavalMergedCollider>()!=null)return false;
            NavalHullVoxel first=NavalVoxelHit.Resolve(sightHits[nearest].collider,sightHits[nearest].point);
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
        NavalStations victim;if(!ships.TryGet(target,out victim)||victim==null||aimCell[gun]==null)return false;
        if(aimKind[gun]==0)return AboveWater(aimCell[gun])||HullVisible(gun,victim);
        if(aimKind[gun]!=1&&aimKind[gun]!=3&&aimKind[gun]!=2)return true;
        if(aimKind[gun]==2)
        {
            NavalHullVoxel first=FirstVisibleCell(gun,aimCell[gun],true);
            return first!=null&&first.Owner==victim.hull&&first.ManagedRig&&AboveWater(first)&&OnlyRigVisible(gun,victim);
        }
        int station=0;
        if(aimKind[gun]==1)for(int i=1;i<=victim.GunCount;i++)if(victim.mechanisms[i].Contains(aimCell[gun])){station=i;break;}
        return victim.Functional(station)&&victim.mechanisms[station].Contains(aimCell[gun])&&VisibleMechanism(gun,victim,station,aimCell[gun]);
    }
    private bool NearMast(float z)
    {foreach(float mast in hull.Definition.MastZ)if(Mathf.Abs(z-mast)<.35f)return true;return false;}
    private bool Feature(int gun,NavalStations victim,int kind,out NavalHullVoxel selected)
    {
        selected=null;float best=float.MaxValue;victim.PrepareTargetIndex();
        if(kind==1||kind==3)
        {
            int targetSide=victim.TargetSide(boat.position),preferred=targetSide+(victim.GunCount>2&&Random.value>.5f?2:0);
            int first=kind==3?0:targetSide,last=kind==3?0:victim.GunCount;
            featureCandidates.Clear();
            for(int station=first;station<=last;station+=2)
            {
                if(!victim.Functional(station))continue;
                foreach(NavalHullVoxel cell in victim.mechanisms[station])
                    if(cell!=null&&!cell.Missing&&!cell.Detached)
                    {
                        float distance=(cell.transform.position-boat.position).sqrMagnitude*(station==preferred?.7f:1f);
                        featureCandidates.Add(new FeatureCandidate{Cell=cell,Station=station,Score=distance});
                    }
            }
            featureCandidates.Sort(candidateOrder);
            foreach(FeatureCandidate candidate in featureCandidates)
                if(VisibleMechanism(gun,victim,candidate.Station,candidate.Cell)){selected=candidate.Cell;return true;}
            return false;
        }
        float along=Random.Range(-4.8f,4.8f),height=Random.Range(1.5f,6.5f);
        float side=victim.boat.InverseTransformPoint(boat.position).x<0f?-1f:1f;
        foreach(NavalHullVoxel cell in kind==0?victim.shellTargets:victim.mastTargets)
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
        bool[] available=featureAvailable;NavalHullVoxel[] cells=featureCells;
        System.Array.Clear(available,0,available.Length);System.Array.Clear(cells,0,cells.Length);
        bool hullVisible=HullVisible(gun,victim);
        for(int kind=0;kind<4;kind++)
        {
            if(kind==2||kind==0&&!hullVisible)continue;
            if(!Feature(gun,victim,kind,out cells[kind]))continue;
            Vector3 predicted;
            available[kind]=ReachableLead(gun,cells[kind].transform.position,velocity,out predicted);
        }
        int chosen=NavalGunneryMath.Choose(Random.value,lastKind[gun],available);
        if(chosen<0&&hullVisible)
        {
            // At the edge of range the randomly chosen feature may be farther
            // away than the nearest waterline plank. Try reachable hull cells.
            float nearest=float.MaxValue;
            foreach(NavalHullVoxel cell in victim.shellTargets)
            {
                if(cell==null||cell.Missing||cell.Detached||!cell.HydraulicShell)continue;
                Vector3 world=cell.transform.position;
                float height=world.y-ocean.Height(world.x,world.z);
                if(height<-.3f||height>1.5f)continue;
                float candidateDistance=(world-boat.position).sqrMagnitude;if(candidateDistance>=nearest)continue;
                Vector3 predicted;if(!ReachableLead(gun,world,velocity,out predicted))continue;
                nearest=candidateDistance;cells[0]=cell;chosen=0;
            }
        }
        if(chosen<0&&!hullVisible&&OnlyRigVisible(gun,victim)&&RigFeature(gun,victim,velocity,out cells[2]))chosen=2;
        if(chosen<0)return false;
        aimKind[gun]=chosen;aimCell[gun]=cells[chosen];aimShip[gun]=target;visibilityAt[gun]=0f;
        aimLocal[gun]=target.InverseTransformPoint(cells[chosen].transform.position);
        // Draw once per shot, so aiming stays stable while the cannon reloads.
        float distance=Vector3.Distance(boat.position,target.position);
        shotError[gun]=Random.insideUnitCircle*(Mathf.Atan2(NavalGunneryMath.Spread(distance),Mathf.Max(1f,distance))*Mathf.Rad2Deg);
        return true;
    }
    public bool AimAtShipAndFire(int gun,Transform target,Vector3 velocity,float dt,Character sailor)
    {
        using(NavalPerformance.Measure(NavalPerfArea.Gunnery))
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
        Debug.Log("[NavalPrototype] AI target: "+targetNames[lastKind[gun]]);
        return true;
            }
    }
}

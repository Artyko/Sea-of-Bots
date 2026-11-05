using System.Collections.Generic;
using UnityEngine;
public partial class NavalRepairSailor
{
    private struct FireSourceCandidate {public Vector3 Point;public int Cell;public float Cost;}
    private readonly List<FireSourceCandidate> fireSources=new List<FireSourceCandidate>(320);
    private readonly List<Vector3> rejectedFireSources=new List<Vector3>(8);
    private int fireSourceCell=-1;
    private float FireSourceCost(Vector3 point)
    {
        var path=NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(point.x,point.y,point.z),hull.Definition);
        float cost=0;Vector3 previous=feet;
        foreach(var p in path){Vector3 next=new Vector3(p.X,p.Y,p.Z);cost+=boat.TransformVector(next-previous).magnitude;previous=next;}
        return cost;
    }
    private bool SourceRejected(Vector3 point)
    {foreach(Vector3 other in rejectedFireSources)if((point-other).sqrMagnitude<.04f)return true;return false;}
    private bool FireSourceRouteClear(Vector3 point)
    {
        var path=NavalSupplyRoutes.Plan(new NavalWalkPoint(feet.x,feet.y,feet.z),new NavalWalkPoint(point.x,point.y,point.z),hull.Definition);
        Vector3 previous=feet;
        foreach(var p in path)
        {
            Vector3 next=new Vector3(p.X,p.Y,p.Z);
            if(Mathf.Abs(previous.y-next.y)*hull.WorldScale<.2f&&!NavigationStairs(previous)&&!NavigationStairs(next)&&
                !NavigationWalkClear(previous,next))return false;
            previous=next;
        }
        return true;
    }
    private bool PlanFireWater()
    {
        feet=boat.InverseTransformPoint(robot.transform.position-Vector3.up*footOffset);fireSources.Clear();
        // Evaluate intake routes only when selecting/replanning, never during each movement frame.
        for(int i=0;i<hull.Flood.Water.Length;i++)
        {
            if(hull.Flood.Water[i]<.15f)continue;
            Vector3 point=new Vector3(hull.Flood.X(i),hull.Floor,hull.Flood.Z(i));
            if(SourceRejected(point)||Mathf.Abs(point.x)>2f||Mathf.Abs(point.z)>5f)continue;
            // Do not select bilge water through a wing tank or its ramp. The
            // v250 log repeatedly stranded firefighters above x=1.63 while
            // their nominal source was below solid ballast geometry.
            if(NavalBallastLayout.Surface(point.x,point.z,hull.Floor,hull.Definition)>hull.Floor+.02f)continue;
            fireSources.Add(new FireSourceCandidate{Point=point,Cell=i,Cost=boat.TransformVector(point-feet).sqrMagnitude});
        }
        fireSources.Sort(delegate(FireSourceCandidate a,FireSourceCandidate b){return a.Cost.CompareTo(b.Cost);});
        // Keep a bounded bilge shortlist before constructing any routes.
        if(fireSources.Count>6)fireSources.RemoveRange(6,fireSources.Count-6);
        for(int n=0;n<NavalFireWaterSupply.For(hull.Definition).Length*2;n++)
        {
            float z=NavalFireWaterSupply.For(hull.Definition)[n/2],x=NavalFireWaterSupply.SeaX(hull.Definition,z);
            Vector3 point=new Vector3(n%2==0?-x:x,hull.Definition.WeatherDeck,z);
            if(!SourceRejected(point))fireSources.Add(new FireSourceCandidate{Point=point,Cell=-1,Cost=boat.TransformVector(point-feet).sqrMagnitude});
        }
        fireSources.Sort(delegate(FireSourceCandidate a,FireSourceCandidate b){return a.Cost.CompareTo(b.Cost);});
        if(fireSources.Count>12)fireSources.RemoveRange(12,fireSources.Count-12);
        for(int n=0;n<fireSources.Count;n++){var candidate=fireSources[n];candidate.Cost=FireSourceCost(candidate.Point);fireSources[n]=candidate;}
        fireSources.Sort(delegate(FireSourceCandidate a,FireSourceCandidate b){return a.Cost.CompareTo(b.Cost);});
        // Prefer a completely clear route. If the sailor begins inside a
        // transient gun/crew cluster, still assign a supported intake and let
        // the bounded route follower escape instead of abandoning the fire.
        for(int pass=0;pass<2;pass++)foreach(var candidate in fireSources)
        {
            if(!Supported(candidate.Point)||!ClearBody(candidate.Point)||pass==0&&!FireSourceRouteClear(candidate.Point))continue;
            fireSource=candidate.Point;fireSourceCell=candidate.Cell;
            Debug.Log("[NavalPrototype] Fire water selected: ship="+hull.Root.name+" sailor="+GetInstanceID()+" source="+fireSource+" kind="+(fireSourceCell<0?"sea":"bilge")+" fallback="+(pass>0));
            route.Clear();PlanTo(fireSource);fireStage=1;fireTime=0;stuck=0;return true;
        }
        return false;
    }
    private bool FillFireWater()
    {
        // Recheck at collection time: the pump or another bucket may have drained this cell.
        fireWater=NavalFireWaterSupply.Take(hull.Flood,fireSourceCell,NavalBucketLoad.DefaultCapacity);
        return fireWater>.0001f;
    }
}

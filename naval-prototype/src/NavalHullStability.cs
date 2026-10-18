using UnityEngine;
public sealed class NavalHullStability
{
    private readonly NavalHull hull;
    private readonly NavalStabilityMath model;
    public static float Roll(Transform ship){return Mathf.Atan2(ship.right.y,ship.up.y)*Mathf.Rad2Deg;}
    public float WaveRoll {get;private set;}
    public float Waterline {get{return model.Waterline;}}
    public float RightingArm {get{return model.RightingArm;}}
    public NavalHullStability(NavalHull ship)
    {hull=ship;model=new NavalStabilityMath(ship.WorldScale,ship.Floor,ship.Definition.RimY);}
    public float Step(float dt,float actualRoll,float waveRoll)
    {
        WaveRoll=waveRoll;model.Observe(actualRoll);NavalMassMoments mass=new NavalMassMoments();
        NavalFloodGrid grid=hull.Flood;
        for(int i=0;i<grid.Water.Length;i++)if(grid.Water[i]>0)
            mass.Add(grid.Water[i],grid.X(i)*hull.WorldScale,(hull.Floor+grid.Depth(i)*.5f)*hull.WorldScale);
        if(hull.Bilge!=null)for(int i=0;i<hull.Bilge.BallastCount;i++)model.AddTank(ref mass,hull.Bilge.BallastAt(i),hull.WorldScale,hull.Floor);
        foreach(NavalRepairSailor worker in hull.RepairWorkers)if(worker!=null&&worker.BucketWater>0)
        {Vector3 p=worker.CarriedPosition;mass.Add(worker.BucketWater,p.x*hull.WorldScale,p.y*hull.WorldScale+.8f);}
        return model.Step(dt,mass,waveRoll);
    }
}

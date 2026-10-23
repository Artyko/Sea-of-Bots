using System.Collections.Generic;
using UnityEngine;
public sealed class NavalBallastUnit
{
    public readonly NavalBallastTank Tank;
    public readonly float Floor;
    private readonly NavalHull hull;
    private readonly List<NavalHullVoxel> cells=new List<NavalHullVoxel>(),outerHoles=new List<NavalHullVoxel>(),damaged=new List<NavalHullVoxel>();
    private readonly HashSet<NavalHullVoxel> outerFaces=new HashSet<NavalHullVoxel>(),innerFaces=new HashSet<NavalHullVoxel>();
    private readonly Dictionary<NavalHullVoxel,Vector3> originalSize=new Dictionary<NavalHullVoxel,Vector3>();
    private readonly NavalHydrostaticSection section=new NavalHydrostaticSection();
    private bool dirty=true;
    public NavalBallastUnit(NavalHull ship,NavalBallastTank tank,int first,float floor)
    {
        hull=ship;Tank=tank;Floor=floor;
        for(int i=first;i<ship.Voxels.Count;i++)
        {
            var cell=ship.Voxels[i];cells.Add(cell);cell.BallastOwner=this;cell.InternalBarrier=true;
            Vector3 size=cell.transform.localScale;originalSize[cell]=size;
            int faces=NavalBallastWall.Faces(tank.X,cell.LocalCenter.x,cell.LocalCenter.y,cell.LocalCenter.z,size.x,size.y,size.z,Floor,ship.WorldScale);
            if((faces&1)!=0)outerFaces.Add(cell);if((faces&2)!=0)innerFaces.Add(cell);
        }
    }
    public void Breach(NavalHullVoxel cell)
    {
        dirty=true;
        Tank.SetDamage(Tank.OuterBroken||outerFaces.Contains(cell),Tank.InnerBroken||innerFaces.Contains(cell),hull.Flood);
    }
    public void ResealIfIntact(){dirty=true;Review();}
    public void Review()
    {
        if(!dirty)return;dirty=false;outerHoles.Clear();damaged.Clear();bool inner=false;
        foreach(var cell in cells)if(cell.Missing||cell.Detached){damaged.Add(cell);if(outerFaces.Contains(cell))outerHoles.Add(cell);if(innerFaces.Contains(cell))inner=true;}
        Tank.SetDamage(outerHoles.Count>0,inner,hull.Flood);
    }
    public bool Covers(NavalHullVoxel opening)
    {
        Vector3 p=opening.LocalCenter;
        return p.x*Tank.X>0&&Mathf.Abs(p.x)>=2.9f&&Mathf.Abs(p.z-Tank.Z)<=NavalBallastLayout.Length*.5f+.15f&&
            p.y>=Floor-.15f&&p.y<=NavalBallastLayout.Surface(Tank.X,p.z,Floor,hull.WorldScale)+.15f;
    }
    public Vector3 RepairAccess(Vector3 hole,Vector3 stand)
    {
        float top=NavalBallastLayout.Surface(Tank.X,hole.z,Floor,hull.WorldScale);
        if(stand.y>=top-.1f)return new Vector3(Mathf.Clamp(hole.x,Tank.X-NavalBallastLayout.Width*.5f,Tank.X+NavalBallastLayout.Width*.5f),top+.015f,hole.z);
        return new Vector3(Mathf.Sign(Tank.X)*(Mathf.Abs(Tank.X)-NavalBallastLayout.Width*.5f-.015f),Mathf.Clamp(hole.y,Floor,top),hole.z);
    }
    public bool Connects(NavalHullVoxel opening)
    {
        Review();if(!Tank.OuterBroken||!Covers(opening))return false;
        foreach(var hole in outerHoles)
            if(Mathf.Abs(hole.LocalCenter.y-opening.LocalCenter.y)<(originalSize[hole].y+opening.transform.localScale.y)*.5f+.01f&&
               Mathf.Abs(hole.LocalCenter.z-opening.LocalCenter.z)<(originalSize[hole].z+opening.transform.localScale.z)*.5f+.01f)return true;
        return false;
    }
    public float SeaTarget(Transform boat,float sea)
    {
        Vector3 bottom=boat.TransformPoint(new Vector3(Tank.X,Floor,Tank.Z));
        return Tank.Capacity*(float)section.FractionBelow(NavalBallastLayout.Width*hull.WorldScale,NavalBallastLayout.Height,NavalHullStability.Roll(boat),sea-bottom.y);
    }
    public void MarkForRepair(float now)
    {
        Review();
        if(!Tank.Broken)return;
        hull.HasRepairDemand=true;
        foreach(var cell in damaged)
        {cell.LastWetAt=now;cell.LastWetFlow=Mathf.Max(.01f,cell.LastWetFlow);}
    }
}
public partial class NavalBilgeSystem
{
    private readonly List<NavalBallastUnit> ballast=new List<NavalBallastUnit>();
    private NavalBallastTank[] transferTanks;
    private readonly NavalBallastTrim trimPlan=new NavalBallastTrim(),pitchPlan=new NavalBallastTrim();
    private bool openBarriers;
    public int BallastCount {get{return ballast.Count;}}
    public NavalBallastTank BallastAt(int index){return ballast[index].Tank;}
    public float BallastFloor(int index){return ballast[index].Floor;}
    public float BallastVolume {get{float total=0;foreach(NavalBallastUnit u in ballast)total+=u.Tank.Total;return total;}}
    public float BallastSide(bool port)
    {float total=0;foreach(NavalBallastUnit u in ballast){if((u.Tank.X<0)==port)total+=u.Tank.Water;total+=u.Tank.SpillPending*.5f;}return total;}
    public float BallastLongitudinalMoment {get{float total=0;foreach(NavalBallastUnit u in ballast)total+=u.Tank.Total*u.Tank.Z/(BoatDimensions.HoldLength*.5f);return total;}}
    private void BuildBallast(Material wood,Material trim)
    {
        transferTanks=new NavalBallastTank[4*hull.Definition.MaintenanceDeckCount];
        for(int deck=0;deck<hull.Definition.MaintenanceDeckCount;deck++)BuildBallastDeck(wood,trim,hull.Definition.MaintenanceFloor(deck));
    }
    private void BuildBallastDeck(Material wood,Material trim,float floor)
    {
        // Larger side reservoirs have permanent end ramps inside their own footprint.
        float height=NavalBallastLayout.Height/hull.WorldScale;
        float run=NavalBallastLayout.RampRun/hull.WorldScale;
        float bodyLength=NavalBallastLayout.Length-run;
        for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
        {
            float x=side*NavalBallastLayout.X,z=end*NavalBallastLayout.Z;int first=hull.Voxels.Count;
            float bodyZ=end*(NavalBallastLayout.InnerZ+run+bodyLength*.5f);
            hull.AddDecorationBox(transform,wood,new Vector3(x,floor+height*.5f,bodyZ),new Vector3(NavalBallastLayout.Width,height,bodyLength),true);
            for(int step=0;step<NavalBallastLayout.Steps;step++)
            {
                float rise=height*(step+1)/NavalBallastLayout.Steps;
                float stepZ=end*(NavalBallastLayout.InnerZ+(step+.5f)*run/NavalBallastLayout.Steps);
                hull.AddDecorationBox(transform,trim,new Vector3(x,floor+rise*.5f,stepZ),new Vector3(NavalBallastLayout.Width,rise,run/NavalBallastLayout.Steps),true);
            }
            for(int band=-1;band<=1;band++)hull.AddDecorationBox(transform,trim,new Vector3(x,floor+height+.006f,bodyZ+band*bodyLength*.3f),new Vector3(NavalBallastLayout.Width,.012f,.08f),true);
            var tank=new NavalBallastTank(NavalBallastLayout.Capacity(hull.WorldScale),x,z);
            transferTanks[ballast.Count]=tank;ballast.Add(new NavalBallastUnit(hull,tank,first,floor));
        }
    }
    public void ResetBallastSeals(){foreach(NavalBallastUnit u in ballast)u.ResealIfIntact();}
    private void FlushBallastSpills(){foreach(NavalBallastUnit u in ballast){u.MarkForRepair(Time.time);u.Tank.Spill(hull.Flood);}}
    private int connectionRevision=-1;
    private readonly Dictionary<NavalHullVoxel,NavalBallastUnit> seaConnections=new Dictionary<NavalHullVoxel,NavalBallastUnit>();
    public NavalBallastUnit SeaConnection(NavalHullVoxel opening)
    {
        if(connectionRevision!=hull.Revision){connectionRevision=hull.Revision;seaConnections.Clear();}
        NavalBallastUnit result;if(seaConnections.TryGetValue(opening,out result))return result;
        foreach(var unit in ballast)if(unit.Connects(opening)){result=unit;break;}
        seaConnections[opening]=result;return result;
    }
    public NavalBallastUnit BallastCover(NavalHullVoxel hole)
    {foreach(var unit in ballast)if(unit.Covers(hole))return unit;return null;}
    private float DrainBallast(float amount)
    {
        float removed=0;
        bool heavyPort=hull.LeftWater>hull.RightWater;
        for(int pass=0;pass<2;pass++)foreach(NavalBallastUnit u in ballast)
        {
            if(((u.Tank.X<0)==heavyPort)!=(pass==0))continue;
            removed+=u.Tank.Drain(amount-removed);if(removed>=amount)return removed;
        }
        return removed;
    }
}

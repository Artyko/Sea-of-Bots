using System;
public sealed class NavalBallastTank
{
    public readonly float Capacity,X,Z;
    public float Water {get;private set;}
    public float SpillPending {get;private set;}
    public bool OuterBroken {get;private set;}
    public bool InnerBroken {get;private set;}
    public bool Broken {get{return OuterBroken||InnerBroken;}}
    public NavalBallastTank(float capacity,float x,float z){Capacity=capacity;X=x;Z=z;}
    public float Total {get{return Water+SpillPending;}}
    public float FillFraction {get{return Capacity>0?Math.Max(0,Math.Min(1,Water/Capacity)):0;}}
    public float Fill(NavalFloodGrid grid,int source,float requested)
    {
        if(Broken)return 0;
        float amount=Math.Min(Math.Max(0,requested),Math.Min(grid.Water[source],Capacity-Water));
        float removed=-grid.Add(source,-amount);Water+=removed;return removed;
    }
    public float Receive(float requested){if(Broken)return 0;float amount=Math.Min(Math.Max(0,requested),Capacity-Water);Water+=amount;return amount;}
    public float Drain(float requested){float amount=Math.Min(Water,Math.Max(0,requested));Water-=amount;return amount;}
    public void Breach(NavalFloodGrid grid)
    {SetDamage(OuterBroken,true,grid);}
    public void SetDamage(bool outside,bool inside,NavalFloodGrid grid)
    {OuterBroken=outside;InnerBroken=inside;if(inside){SpillPending+=Water;Water=0;Spill(grid);}}
    public float FloodFromSea(float target)
    {if(!OuterBroken||InnerBroken)return 0;float amount=Math.Max(0,Math.Min(Capacity,target)-Water);Water+=amount;return amount;}
    public void Spill(NavalFloodGrid grid)
    {
        if(SpillPending<=0)return;
        float room=0;for(int i=0;i<grid.Water.Length;i++)room+=Math.Max(0,grid.MaxCellVolume-grid.Water[i]);
        float budget=Math.Min(room,SpillPending),released=0;
        if(room<=0)return;
        for(int i=0;i<grid.Water.Length;i++)released+=grid.Add(i,budget*Math.Max(0,grid.MaxCellVolume-grid.Water[i])/room);
        SpillPending=Math.Max(0,SpillPending-released);
    }
    public void Reseal(){OuterBroken=InnerBroken=false;}
}
// Seal faces: 1 = sea-facing wall, 2 = hold-facing wall/lid/end, 0 = solid core or trim.
public static class NavalBallastWall
{
    public static int Faces(float tankX,float x,float y,float z,float sx,float sy,float sz,float floor,float scale)
    {
        float top=NavalBallastLayout.Surface(tankX,z,floor,scale),height=NavalBallastLayout.Height/scale;
        if(y>floor+height+.001f)return 0;
        bool outside=Math.Abs(x)+sx*.5f>=Math.Abs(tankX)+NavalBallastLayout.Width*.5f-.01f;
        bool inside=Math.Abs(x)-sx*.5f<=Math.Abs(tankX)-NavalBallastLayout.Width*.5f+.01f||
            y+sy*.5f>=top-.01f||Math.Abs(z)+sz*.5f>=NavalBallastLayout.Z+NavalBallastLayout.Length*.5f-.01f||
            Math.Abs(z)-sz*.5f<=NavalBallastLayout.InnerZ+.01f;
        return (outside?1:0)|(inside?2:0);
    }
}

// Unobstructed water overtopping an intact lip, reduced by the interior water level.
public static class NavalDownfloodMath
{
    public static float OverflowRate(float sea,float lip,float inside,float width)
    {
        float head=Math.Max(0,sea-Math.Max(lip,inside));
        return 1.1f*Math.Max(0,width)*head*(float)Math.Sqrt(head);
    }
}

// Downflooding through intact low openings is a trim emergency, not a repairable hole.
public sealed class NavalDownfloodEmergency
{
    private float reserve;
    public bool Active {get;private set;}
    public void Step(float dt,bool activeLeaks,float openingInflow,float heel)
    {
        if(activeLeaks){reserve=0;Active=false;return;}
        bool flooding=openingInflow>.02f&&Math.Abs(heel)>=6;
        if(flooding)reserve=8;else reserve=Math.Max(0,reserve-Math.Max(0,dt));
        Active=flooding||reserve>0&&Math.Abs(heel)>3;
    }
}

// Evaluate actual response over a settling interval and complete averaging windows.
public sealed class NavalBallastTrim
{
    private float elapsed,moved,baseline,filtered,windowSum,windowTime,settling,cooldownHeel;
    private bool initialized,trial,balancing,wasUrgent;
    public float Cooldown {get;private set;}
    public bool Ineffective {get;private set;}
    public float FilteredHeel {get{return filtered;}}
    public bool WantsTransfer {get{return balancing&&Cooldown<=0;}}
    public static bool AllowsTransfer(bool activeLeaks,float inflow,bool urgent)
    {return !activeLeaks&&(inflow<.02f||urgent);}
    public void Update(float dt,float heel,bool urgent=false)
    {
        dt=Math.Max(0,dt);Cooldown=Math.Max(0,Cooldown-dt);
        if(urgent&&!wasUrgent){Cooldown=0;Ineffective=false;trial=false;filtered=heel;}
        wasUrgent=urgent;
        if(!initialized){filtered=heel;initialized=true;}
        filtered+=(heel-filtered)*(1f-(float)Math.Exp(-dt/2.5f));
        // Hysteresis keeps waves around the entry threshold from cancelling every trial.
        if(Math.Abs(filtered)<3)balancing=false;
        else if(Math.Abs(filtered)>6||urgent&&Math.Abs(filtered)>3)balancing=true;
        if(Cooldown>0&&(filtered*cooldownHeel<0||Math.Abs(filtered-cooldownHeel)>4))
        {Cooldown=0;Ineffective=false;}
    }
    public void Observe(float dt,float amount,bool attempting)
    {
        dt=Math.Max(0,dt);
        if(!attempting||Cooldown>0){trial=false;elapsed=moved=windowSum=windowTime=0;return;}
        if(!trial)
        {baseline=Math.Abs(filtered);trial=true;elapsed=moved=windowSum=windowTime=0;settling=6;Ineffective=false;}
        moved+=Math.Max(0,amount);
        if(settling>0){settling=Math.Max(0,settling-dt);return;}
        elapsed+=dt;windowTime+=dt;windowSum+=Math.Abs(filtered)*dt;
        if(elapsed<12)return;
        float mean=windowSum/Math.Max(.001f,windowTime);
        if(moved<.5f||baseline-mean<.15f&&mean>3f)
        {Cooldown=12;cooldownHeel=filtered;Ineffective=true;trial=false;}
        else {baseline=mean;elapsed=moved=windowSum=windowTime=0;}
    }
}

// All open-hold cells on the low side are valid intakes, including the former central corridor.
public static class NavalBallastTransfer
{
    public static int Source(NavalFloodGrid grid,bool port)
    {
        int best=-1;float most=.00001f;
        for(int i=0;i<grid.Water.Length;i++)if((grid.X(i)<0)==port&&grid.Water[i]>most){best=i;most=grid.Water[i];}
        return best;
    }
    public static float Move(NavalFloodGrid grid,NavalBallastTank[] tanks,bool fromPort,float requested)
    {
        float moved=0;
        for(int pass=0;pass<16&&requested-moved>.00001f;pass++)
        {
            int source=Source(grid,fromPort);NavalBallastTank stored=null,target=null;
            if(source<0)foreach(var tank in tanks)
                if(tank!=null&&!tank.Broken&&(tank.X<0)==fromPort&&tank.Water>.00001f&&(stored==null||tank.Water>stored.Water))stored=tank;
            if(source<0&&stored==null)break;
            float z=source>=0?grid.Z(source):stored.Z,best=float.MaxValue;
            foreach(var tank in tanks)
            {
                if(tank==null||tank.Broken||(tank.X<0)==fromPort||tank.Capacity-tank.Water<.00001f)continue;
                float distance=Math.Abs(tank.Z-z);if(distance<best){target=tank;best=distance;}
            }
            if(target==null)break;
            float amount=source>=0?target.Fill(grid,source,requested-moved):target.Receive(stored.Drain(Math.Min(requested-moved,target.Capacity-target.Water)));
            moved+=amount;if(amount<=.00001f)break;
        }
        return moved;
    }
}

public static class NavalBallastLayout
{
    public const float X=2.45f,Z=3f,Width=.8f,Length=4.4f,Height=1.2f;
    public const float RampRun=1.8f;
    public const int Steps=6;
    public static float InnerZ {get{return Z-Length*.5f;}}
    public static float Capacity(float scale)
    {return Width*Height*(Length-RampRun/scale*(Steps-1f)/(2f*Steps))*scale*scale;}
    public static float Surface(float x,float z,float floor,float scale)
    {
        if(Math.Abs(x)<X-Width*.5f||Math.Abs(x)>X+Width*.5f||Math.Abs(z)<InnerZ||Math.Abs(z)>Z+Length*.5f)return floor;
        int step=Math.Max(1,Math.Min(Steps,1+(int)((Math.Abs(z)-InnerZ)/(RampRun/scale/Steps))));
        return floor+Height*step/(Steps*scale);
    }
}

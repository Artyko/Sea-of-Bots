using System.Collections.Generic;
using UnityEngine;

// Shared equipment for any definition with watertight compartments.
public partial class NavalBilgeSystem : MonoBehaviour
{
    private readonly List<NavalHullVoxel> walls=new List<NavalHullVoxel>();
    private NavalHull hull;
    private FirstPersonMover player;

    private int cachedRevision=-1;

    private readonly Dictionary<int,Vector3> apertures=new Dictionary<int,Vector3>();
    private readonly List<NavalHullVoxel> damagedWalls=new List<NavalHullVoxel>();

    public bool PlayerInteracting {get;private set;}
    private bool playerPump;
    private NavalPumpStation contextPump;
    private float nextStatusAt;
    private int pumpPlanFrame=-1,pumpObserveFrame=-1;
    private int statusKind=-1;
    private static readonly string[] pumpModes={"Авто","За борт","Левый -> правые ёмкости","Правый -> левые ёмкости","Балласт -> море","Нос -> кормовые ёмкости","Корма -> носовые ёмкости"};
    private int mode; // 0 automatic, 1 bilge then ballast overboard, 2 port->starboard, 3 reverse, 4 ballast only overboard
    public Vector3 PumpPost {get{return new Vector3(0f,hull.Floor,-4.9f);}}
    public string Status {get;private set;}
    public bool Functional {get{foreach(var station in pumps)if(station.Functional)return true;return false;}}
    public bool NeedsRepair {get{foreach(var station in pumps)if(station.Mechanism.NeedsRepair)return true;return false;}}
    public float LongitudinalList {get{return Mathf.Clamp(hull.WaterLongitudinalMoment/(BoatDimensions.HalfHoldArea*hull.VolumeScale)*15f,-15f,15f);}}
    public bool UrgentTrim {get{return Functional&&hull.UrgentTrim;}}
    private readonly NavalPumpDemand crewDemand=new NavalPumpDemand();
    public void UpdateCrewDemand(float dt)
    {
        if(!Functional||hull.Sunk){crewDemand.Reset();return;}
        crewDemand.Step(dt,RawOperatorDemand,hull.Flood.Total<.15f&&!UrgentTrim&&Mathf.Abs(LongitudinalList)<3f);
    }
    public bool WantsOperator {get{return Functional&&!hull.Sunk&&(crewDemand.Active||RawOperatorDemand);}}
    private bool RawOperatorDemand {get{return Functional&&!hull.Sunk&&(UrgentTrim||hull.Flood.Total>1f||BallastVolume>1f&&(trimPlan.WantsTransfer||Mathf.Abs(NavalHullStability.Roll(transform))>6f||Mathf.Abs(LongitudinalList)>3f));}}
    public void Initialize(NavalHull owner,FirstPersonMover observer,Material wood,Material trim,Material metal)
    {
        hull=owner;player=observer;owner.Bilge=this;
        if(!owner.Definition.Compartments)return;
        if(owner.Definition.Partitions)
        {
        float partition=NavalCompartmentMath.PartitionHeight(hull.WorldScale);
        int rows=Mathf.CeilToInt(partition/.25f);float rowHeight=partition/rows;
        for(int side=-1;side<=1;side+=2)
        {
            for(int z=0;z<48;z++)for(int y=0;y<rows;y++)
            {
                float pz=-5.875f+z*.25f,py=hull.Floor+(y+.5f)*rowHeight;
                bool opening=Mathf.Abs(Mathf.Abs(pz)-NavalCompartmentMath.DoorZ)<.625f;
                if(!opening)Wall(new Vector3(side*NavalCompartmentMath.WallX,py,pz),new Vector3(.14f,rowHeight,.25f),wood);
            }
            for(int x=0;x<6;x++)for(int y=0;y<rows;y++)
                Wall(new Vector3(side*(NavalCompartmentMath.WallX+(x+.5f)*(BoatDimensions.HoldWidth*.5f-NavalCompartmentMath.WallX)/6f),hull.Floor+(y+.5f)*rowHeight,0f),new Vector3(.25f,rowHeight,.14f),wood);
            // Low wooden coamings retain shallow water; stepped approaches allow ordinary walking.
            for(int end=-1;end<=1;end+=2)
            {
                float height=NavalCompartmentMath.CoamingHeight(owner.Definition.Scale);
                for(int z=0;z<5;z++)
                {
                    float pz=end*NavalCompartmentMath.DoorZ-.625f+(z+.5f)*.25f;
                    Wall(new Vector3(side*NavalCompartmentMath.WallX,hull.Floor+height*.5f,pz),new Vector3(.14f,height,.25f),trim);
                    for(int face=-1;face<=1;face+=2)for(int step=1;step<=3;step++)
                    {
                        float rise=height*(4-step)/4f;
                        hull.AddDecorationBox(transform,trim,new Vector3(side*NavalCompartmentMath.WallX+face*(.07f+(step-.5f)*.06f),hull.Floor+rise*.5f,pz),new Vector3(.06f,rise,.25f),true);
                    }
                }
            }
        }
        BuildShields(trim);
        }
        BuildBallast(wood,trim);
        for(int deck=0;deck<hull.Definition.MaintenanceDeckCount;deck++)BuildPumpStation(hull.Definition.MaintenanceFloor(deck),trim,metal);
    }
    private NavalHullVoxel Wall(Vector3 point,Vector3 size,Material material)
    {NavalHullVoxel c=hull.AddVoxel(transform,material,point,size,false,true);c.InternalBarrier=true;walls.Add(c);return c;}
    // Door-free openings use the same shared routes on every compartmented ship.
    public Vector3 PassageTarget(Vector3 feet,Vector3 destination,NavalRepairSailor worker=null,float dt=0f)
    {return ShieldPassage(feet,destination,worker,dt);}
    private bool Passage(float x,float z)
    {
        return Mathf.Abs(Mathf.Abs(x)-NavalCompartmentMath.WallX)<.2f&&
            Mathf.Abs(Mathf.Abs(z)-NavalCompartmentMath.DoorZ)<.625f;
    }
    private float Sill(int a,int b)
    {
        NavalFloodGrid g=hull.Flood;
        if(NavalCompartmentMath.Region(g.X(a),g.Z(a))==NavalCompartmentMath.Region(g.X(b),g.Z(b)))return float.NegativeInfinity;
        float x=(g.X(a)+g.X(b))*.5f,z=(g.Z(a)+g.Z(b))*.5f;
        float lowest=Passage(x,z)?ShieldSill(x,z):hull.Floor+NavalCompartmentMath.PartitionHeight(hull.WorldScale);
        foreach(NavalHullVoxel cell in damagedWalls)
        {
            Vector3 p=cell.LocalCenter;
            if((cell.Missing||cell.Detached)&&Mathf.Abs(p.x-x)<.3f&&Mathf.Abs(p.z-z)<.3f)lowest=Mathf.Min(lowest,p.y-cell.transform.localScale.y*.5f);
        }
        return lowest;
    }
    public void UpdateBarriers()
    {
        FlushBallastSpills();
        if(!hull.Definition.Partitions)
        {
            if(!openBarriers){for(int i=0;i<hull.Flood.Water.Length;i++)hull.Flood.RightSill[i]=hull.Flood.ForwardSill[i]=float.NegativeInfinity;openBarriers=true;}
            return;
        }
        ReviewShields();
        bool changed=cachedRevision!=hull.Revision;
        if(changed)
        {
            apertures.Clear();cachedRevision=hull.Revision;
            damagedWalls.Clear();foreach(NavalHullVoxel cell in walls)if(cell.Missing||cell.Detached)damagedWalls.Add(cell);
            NavalFloodGrid g=hull.Flood;
            for(int i=0;i<g.Water.Length;i++)
            {if(i%g.Columns+1<g.Columns)CacheSill(i,i+1,true);if(i/g.Columns+1<g.Rows)CacheSill(i,i+g.Columns,false);}
        }
        foreach(NavalHullVoxel cell in damagedWalls)
        {
            int index=hull.Flood.Index(cell.LocalCenter.x,cell.LocalCenter.z);
            if(hull.Flood.Depth(index)>cell.LocalCenter.y-hull.Floor+.05f)
            {cell.InflowRate=.05f;cell.LastWetAt=Time.time;cell.LastWetFlow=.05f;}
        }
        Matrix4x4 toWorld=transform.localToWorldMatrix;
        foreach(KeyValuePair<int,Vector3> edge in apertures)
        {
            int i=edge.Key/2;float sill=toWorld.MultiplyPoint3x4(edge.Value).y;
            if(edge.Key%2==0)hull.Flood.RightSill[i]=sill;else hull.Flood.ForwardSill[i]=sill;
        }
    }
    private void CacheSill(int a,int b,bool right)
    {
        NavalFloodGrid grid=hull.Flood;float sill=Sill(a,b);
        if(float.IsInfinity(sill))
        {if(right)grid.RightSill[a]=sill;else grid.ForwardSill[a]=sill;return;}
        apertures[a*2+(right?0:1)]=new Vector3((grid.X(a)+grid.X(b))*.5f,sill,(grid.Z(a)+grid.Z(b))*.5f);
    }
    private int Wettest(int region)
    {
        int selected=-1;float most=0f;
        for(int i=0;i<hull.Flood.Water.Length;i++)if((region<0||NavalCompartmentMath.Region(hull.Flood.X(i),hull.Flood.Z(i))==region)&&hull.Flood.Water[i]>most)
        {most=hull.Flood.Water[i];selected=i;}
        return selected;
    }
    public void Pump(float dt,bool automatic,NavalPumpStation station=null)
    {
        if(station==null)station=playerPump?playerStation:NearestPump(PumpPost,false);
        if(station==null||!station.Functional){if(station!=null)StopStation(station);return;}
        if(automatic?!WantsOperator:hull.Flood.Total+BallastVolume<=.00001f)return;
        float processed=0f;
        int selectedMode=automatic?0:mode;
        float heel=NavalHullStability.Roll(transform);
        if(selectedMode==0&&pumpPlanFrame!=Time.frameCount)
        {
            pumpPlanFrame=Time.frameCount;
            trimPlan.Update(dt,NavalBroadsideMath.Delta(hull.Stability.WaveRoll,heel),UrgentTrim);
            // Pitch is evaluated from water mass, excluding the passing wave slope.
            pitchPlan.Update(dt,LongitudinalList,Mathf.Abs(LongitudinalList)>3f);
        }
        bool longitudinal=selectedMode==5||selectedMode==6||selectedMode==0&&pitchPlan.WantsTransfer&&
            (!trimPlan.WantsTransfer||Mathf.Abs(pitchPlan.FilteredHeel)>Mathf.Abs(trimPlan.FilteredHeel));
        bool transfer=(selectedMode==2||selectedMode==3||selectedMode==5||selectedMode==6)||selectedMode==0&&(longitudinal||trimPlan.WantsTransfer)&&NavalBallastTrim.AllowsTransfer(hull.HasActiveLeaks,hull.Inflow,UrgentTrim);
        bool fromPort=selectedMode==2||selectedMode==0&&trimPlan.FilteredHeel>0;
        bool fromStern=selectedMode==6||selectedMode==0&&pitchPlan.FilteredHeel<0;
        bool attempted=transfer;
        if(transfer)
        {
            processed=longitudinal?NavalBallastTransfer.MoveLongitudinal(hull.Flood,transferTanks,fromStern,NavalMaintenanceMath.PumpTransferRate*dt):NavalBallastTransfer.Move(hull.Flood,transferTanks,fromPort,NavalMaintenanceMath.PumpTransferRate*dt);
            transfer=processed>.00001f;

        }
        if(selectedMode==0&&pumpObserveFrame!=Time.frameCount){pumpObserveFrame=Time.frameCount;trimPlan.Observe(dt,longitudinal?0:processed,attempted&&!longitudinal);pitchPlan.Observe(dt,longitudinal?processed:0,attempted&&longitudinal);}
        if(!transfer)
        {
            float budget=NavalMaintenanceMath.PumpOutRate*dt;
            if(selectedMode!=4)for(int n=0;n<8&&budget>0f;n++){int i=Wettest(-1);if(i<0)break;budget+=hull.Flood.Add(i,-budget);}
            if(budget>0f)budget-=DrainBallast(budget);
            processed=NavalMaintenanceMath.PumpOutRate*dt-budget;

        }
        if(processed>.00001f){station.LastWork=Time.time;station.Phase=Mathf.Repeat(station.Phase+dt/1.2f,1f);station.LastTransfer=transfer;}
        int kind=transfer?(longitudinal?(fromStern?7:6):(fromPort?2:3)):selectedMode==4?4:automatic&&trimPlan.Cooldown>0?5:1;
        if(kind!=statusKind||Time.unscaledTime>=nextStatusAt)
        {
            statusKind=kind;nextStatusAt=Time.unscaledTime+.2f;
            Status=transfer?"Transfer "+(longitudinal?(fromStern?"stern -> bow":"bow -> stern"):(fromPort?"port -> starboard":"starboard -> port"))+" | "+(processed/Mathf.Max(.001f,dt)).ToString("F1")+" m3/s":
                kind==4?"Ballast -> sea":kind==5?"Trim ineffective: pumping overboard":"Pumping overboard";
            Status+=" | Sealed ballast: "+BallastVolume.ToString("F1")+" m3";
        }
        if(station.Crank!=null)station.Crank.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*8f)*25f,0f,0f);
    }
    private void Update()
    {
        PlayerInteracting=false;
        foreach(var station in pumps)if(!station.Functional&&(station.Operator!=null||PlayerUsingStation(station)))StopStation(station);
        bool working=Functional;
        if(player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()){ReleasePlayer();return;}
        if(Time.timeScale==0f||NavalTestWorld.Active!=null&&NavalTestWorld.Active.ShipMenuOpen)return;
        Vector3 feet=transform.InverseTransformPoint(player.transform.position-Vector3.up*1f);
        NavalPumpStation nearest=null;float distance=float.PositiveInfinity;
        foreach(var station in pumps){float d=transform.TransformVector(feet-station.Post).sqrMagnitude;if(d<distance){distance=d;nearest=station;}}
        bool near=nearest!=null&&distance<2.5f*2.5f;
        contextPump=nearest;working=nearest!=null&&nearest.Functional;
        PlayerInteracting=near||playerPump;
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(playerPump){playerPump=false;return;}
            if(near&&working){playerStation=nearest;playerPump=true;if(nearest.Operator!=null)nearest.Operator.CancelPumpDuty();}
        }
        if(playerPump)
        {
            if(!near||nearest!=playerStation){playerPump=false;return;}
            if(Input.GetKeyDown(KeyCode.Q))mode=(mode+1)%pumpModes.Length;
            if(Input.GetKey(KeyCode.T))Pump(Mathf.Min(Time.deltaTime,.05f),false);
        }
    }
    public void ReleasePlayer(){playerPump=false;PlayerInteracting=false;}
    public bool PlayerUsing {get{return playerPump;}}
    private void OnGUI()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(world!=null&&world.PlayerHull==hull&&world.SimulationActive&&world.ViewCamera!=null&&!hull.Sunk)
            for(int i=0;i<pumps.Count;i++){var station=pumps[i];if(station.Functional&&Time.time-station.LastWork<.2f)
                NavalWorldProgressBar.Draw(world.ViewCamera,transform.TransformPoint(station.Post+new Vector3(0f,1.15f,NavalMaintenanceMath.PumpOffsetZ)),station.Phase,true,false,station.LastTransfer?"Перекачка":"Откачка",GetInstanceID()+i);}

        if(!PlayerInteracting)return;
        if(contextPump!=null&&!contextPump.Functional)
        {
            GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,620f),Screen.height-245f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,620f),30f),contextPump.Mechanism.NeedsRepair?"Помпа повреждена — восстановите её детали (удерживать R)":"Помпа разрушена — восстановление невозможно");
            return;
        }
        GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,620f),Screen.height-265f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,620f),48f),playerPump?"Помпа: держать T | Q: "+pumpModes[mode]+" | E: выйти\n"+Status:"E: помпа | Закрытый балласт: "+BallastVolume.ToString("F1")+" / "+(NavalBallastLayout.Capacity(hull.WorldScale)*BallastCount).ToString("F1")+" м³");
    }
}

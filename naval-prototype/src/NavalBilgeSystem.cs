using System.Collections.Generic;
using UnityEngine;

// Shared equipment for any definition with watertight compartments.
public partial class NavalBilgeSystem : MonoBehaviour
{
    private readonly List<NavalHullVoxel> walls=new List<NavalHullVoxel>(),pumpCells=new List<NavalHullVoxel>();
    private NavalHull hull;
    private FirstPersonMover player;
    private Transform crank;
    private int cachedRevision=-1;
    private NavalRepairableMechanism pumpMechanism;
    private readonly Dictionary<int,Vector3> apertures=new Dictionary<int,Vector3>();
    private readonly List<NavalHullVoxel> damagedWalls=new List<NavalHullVoxel>();
    public NavalRepairSailor Operator;
    public bool PlayerInteracting {get;private set;}
    private bool playerPump;
    private float pumpLastWork=-100f,pumpPhase;
    private bool lastTransfer;
    private int mode; // 0 automatic, 1 overboard, 2 port->starboard, 3 reverse
    public Vector3 PumpPost {get{return new Vector3(0f,hull.Floor,-4.9f);}}
    public string Status {get;private set;}
    public bool Functional {get{return pumpMechanism!=null&&pumpMechanism.Functional;}}
    public bool NeedsRepair {get{return pumpMechanism!=null&&pumpMechanism.NeedsRepair;}}
    private void StopBrokenPump()
    {
        ReleasePlayer();pumpLastWork=-100f;Status=pumpMechanism!=null&&pumpMechanism.Destroyed?"Pump destroyed":"Pump damaged";
        if(Operator!=null){NavalRepairSailor worker=Operator;Operator=null;worker.CancelPumpDuty();}
        if(crank!=null)crank.localRotation=Quaternion.identity;
    }
    public bool WantsOperator {get{return Functional&&!hull.Sunk&&hull.Flood.Total>1f;}}
    public void Initialize(NavalHull owner,FirstPersonMover observer,Material wood,Material trim,Material metal)
    {
        hull=owner;player=observer;owner.Bilge=this;
        if(!owner.Definition.Compartments)return;
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
        int first=hull.Voxels.Count;
        hull.AddDecorationBox(transform,metal,PumpPost+new Vector3(0f,.3f,NavalMaintenanceMath.PumpOffsetZ),new Vector3(.5f,.6f,.5f),true);
        hull.AddDecorationBox(transform,trim,PumpPost+new Vector3(0f,.65f,NavalMaintenanceMath.PumpOffsetZ),new Vector3(.8f,.12f,.15f),true);
        crank=new GameObject("Bilge pump handle").transform;crank.SetParent(transform,false);crank.localPosition=PumpPost+new Vector3(0f,.65f,NavalMaintenanceMath.PumpOffsetZ);
        // Two visible pipes lead from the pump toward side compartment inlets.
        for(int side=-1;side<=1;side+=2)
            hull.AddDecorationBox(transform,metal,new Vector3(side*1.4f,hull.Floor+.13f,-5.45f),new Vector3(2.8f,.12f,.12f),true);
        // Body, handle and inlet pipes are one damageable mechanism.
        for(int i=first;i<hull.Voxels.Count;i++){hull.Voxels[i].RepairableEquipment=true;pumpCells.Add(hull.Voxels[i]);}
        foreach(NavalHullVoxel cell in pumpCells)if(cell.LocalCenter.y>hull.Floor+.6f)cell.transform.SetParent(crank,true);
        pumpMechanism=new NavalRepairableMechanism(hull,pumpCells,true);
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
        if(!hull.Definition.Compartments)return;
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
    public void Pump(float dt,bool automatic)
    {
        if(!Functional){StopBrokenPump();return;}
        if(!WantsOperator)return;
        float processed=0f;
        int selectedMode=automatic?0:mode;
        float heel=Mathf.DeltaAngle(0f,transform.eulerAngles.z);
        bool transfer=selectedMode>=2||selectedMode==0&&!hull.HasActiveLeaks&&NavalCompartmentMath.NeedsTransfer(hull.LeftWater,hull.RightWater,heel);
        bool fromPort=selectedMode==2||selectedMode==0&&hull.LeftWater>hull.RightWater;
        int source=transfer?Wettest(fromPort?1:3):-1;
        if(transfer)
        {
            int fore=Wettest(fromPort?2:4);if(source<0||fore>=0&&hull.Flood.Water[fore]>hull.Flood.Water[source])source=fore;
            if(source>=0)
            {
                int destination=hull.Flood.Index(fromPort?2.2f:-2.2f,hull.Flood.Z(source));
                float maximum=selectedMode==0?Mathf.Abs(hull.LeftWater-hull.RightWater)*.5f:NavalMaintenanceMath.PumpTransferRate*dt;
                float moved=NavalCompartmentMath.Move(hull.Flood,source,destination,Mathf.Min(NavalMaintenanceMath.PumpTransferRate*dt,maximum));
                processed=moved;
                if(moved<=.00001f)transfer=false;
                Status="Transfer "+(fromPort?"port -> starboard":"starboard -> port")+" | "+(moved/Mathf.Max(.001f,dt)).ToString("F1")+" m3/s";
            }
            else transfer=false;
        }
        if(!transfer)
        {
            float budget=NavalMaintenanceMath.PumpOutRate*dt;
            for(int n=0;n<8&&budget>0f;n++){int i=Wettest(-1);if(i<0)break;budget+=hull.Flood.Add(i,-budget);}
            processed=NavalMaintenanceMath.PumpOutRate*dt-budget;
            Status="Pumping overboard";
        }
        if(processed>.00001f){pumpLastWork=Time.time;pumpPhase=Mathf.Repeat(pumpPhase+dt/1.2f,1f);lastTransfer=transfer;}
        if(crank!=null)crank.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*8f)*25f,0f,0f);
    }
    private void Update()
    {
        PlayerInteracting=false;
        bool working=Functional;if(!working)StopBrokenPump();
        if(player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||Time.timeScale==0f||NavalTestWorld.Active!=null&&NavalTestWorld.Active.ShipMenuOpen)return;
        Vector3 feet=transform.InverseTransformPoint(player.transform.position-Vector3.up*1f);
        bool near=Vector3.Distance(feet,PumpPost)<1.2f;
        PlayerInteracting=near||playerPump;
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(playerPump){playerPump=false;return;}
            if(near&&working){playerPump=true;if(Operator!=null){Operator.CancelPumpDuty();Operator=null;}}
        }
        if(playerPump)
        {
            if(!near){playerPump=false;return;}
            if(Input.GetKeyDown(KeyCode.Q))mode=(mode+1)%4;
            if(Input.GetKey(KeyCode.T))Pump(Mathf.Min(Time.deltaTime,.05f),false);
        }
    }
    public void ReleasePlayer(){playerPump=false;PlayerInteracting=false;}
    public bool PlayerUsing {get{return playerPump;}}
    private void OnGUI()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(world!=null&&world.PlayerHull==hull&&world.SimulationActive&&world.ViewCamera!=null&&!hull.Sunk&&Functional&&Time.time-pumpLastWork<.2f)
            NavalWorldProgressBar.Draw(world.ViewCamera,transform.TransformPoint(PumpPost+new Vector3(0f,1.15f,NavalMaintenanceMath.PumpOffsetZ)),pumpPhase,true,false,lastTransfer?"Перекачка":"Откачка",GetInstanceID());
        if(!PlayerInteracting)return;
        if(!Functional)
        {
            GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,620f),Screen.height-245f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,620f),30f),NeedsRepair?"Помпа повреждена — восстановите её детали (удерживать R)":"Помпа разрушена — восстановление невозможно");
            return;
        }
        GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,620f),Screen.height-245f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,620f),30f),playerPump?"PUMP: hold T | Q: "+new string[]{"Auto","Overboard","Port -> starboard","Starboard -> port"}[mode]+" | E: leave | "+Status:"E: bilge pump");
    }
}

using System.Collections.Generic;
using UnityEngine;

// Shared equipment for any definition with watertight compartments.
public class NavalBilgeSystem : MonoBehaviour
{
    private class Door
    {
        public Transform Pivot;public Vector3 Center;public bool Manual,Open;public float Until;
        public readonly List<NavalHullVoxel> Cells=new List<NavalHullVoxel>();
    }
    private readonly List<NavalHullVoxel> walls=new List<NavalHullVoxel>(),pumpCells=new List<NavalHullVoxel>();
    private readonly List<Door> doors=new List<Door>();
    private NavalHull hull;
    private FirstPersonMover player;
    private Transform crank;
    private int cachedRevision=-1;
    private readonly Dictionary<int,float> apertures=new Dictionary<int,float>();
    public NavalRepairSailor Operator;
    public bool PlayerInteracting {get;private set;}
    private bool playerPump;
    private float pumpLastWork=-100f,pumpPhase;
    private bool lastTransfer;
    private int mode; // 0 automatic, 1 overboard, 2 port->starboard, 3 reverse
    public readonly Vector3 PumpPost=new Vector3(0f,BoatDimensions.HoldFloor,-4.9f);
    public string Status {get;private set;}
    public bool Functional {get{foreach(NavalHullVoxel c in pumpCells)if(c==null||c.Missing||c.Detached)return false;return true;}}
    public bool WantsOperator {get{return Functional&&!hull.Sunk&&hull.Flood.Total>1f;}}
    public void Initialize(NavalHull owner,FirstPersonMover observer,Material wood,Material trim,Material metal)
    {
        hull=owner;player=observer;owner.Bilge=this;
        if(!owner.Definition.Compartments)return;
        for(int side=-1;side<=1;side+=2)
        {
            for(int z=0;z<48;z++)for(int y=0;y<11;y++)
            {
                float pz=-5.875f+z*.25f,py=BoatDimensions.HoldFloor+(y+.5f)*.25f;
                bool doorway=(Mathf.Abs(pz-NavalCompartmentMath.DoorZ)<.625f||Mathf.Abs(pz+NavalCompartmentMath.DoorZ)<.625f)&&py<BoatDimensions.HoldFloor+1.5f;
                if(!doorway)Wall(new Vector3(side*NavalCompartmentMath.WallX,py,pz),new Vector3(.14f,.25f,.25f),wood);
            }
            for(int x=0;x<6;x++)for(int y=0;y<11;y++)
                Wall(new Vector3(side*(NavalCompartmentMath.WallX+(x+.5f)*(BoatDimensions.HoldWidth*.5f-NavalCompartmentMath.WallX)/6f),BoatDimensions.HoldFloor+(y+.5f)*.25f,0f),new Vector3(.25f,.25f,.14f),wood);
            for(int end=-1;end<=1;end+=2)
            {
                Door d=new Door();d.Center=new Vector3(side*NavalCompartmentMath.WallX,BoatDimensions.HoldFloor+.75f,end*NavalCompartmentMath.DoorZ);
                d.Pivot=new GameObject("Watertight door").transform;d.Pivot.SetParent(transform,false);d.Pivot.localPosition=d.Center+Vector3.forward*.625f;
                for(int z=0;z<5;z++)for(int y=0;y<6;y++)
                {
                    NavalHullVoxel c=Wall(new Vector3(d.Center.x,BoatDimensions.HoldFloor+(y+.5f)*.25f,d.Center.z-.625f+(z+.5f)*.25f),new Vector3(.14f,.25f,.25f),trim);
                    c.transform.SetParent(d.Pivot,true);d.Cells.Add(c);
                }
                // Dark outlines on both faces remain readable in the wooden hold.
                int trimFirst=hull.Voxels.Count;
                for(int face=-1;face<=1;face+=2)
                {
                    float x=d.Center.x+face*.09f;
                    for(int edge=-1;edge<=1;edge+=2)
                    {
                        hull.AddDecorationBox(transform,metal,new Vector3(x,d.Center.y,d.Center.z+edge*.59f),new Vector3(.04f,1.5f,.08f),true);
                        hull.AddDecorationBox(transform,metal,new Vector3(x,d.Center.y+edge*.71f,d.Center.z),new Vector3(.04f,.08f,1.25f),true);
                    }
                    hull.AddDecorationBox(transform,metal,new Vector3(x+face*.035f,d.Center.y,d.Center.z-.35f),new Vector3(.08f,.08f,.24f),true);
                }
                for(int i=trimFirst;i<hull.Voxels.Count;i++)hull.Voxels[i].transform.SetParent(d.Pivot,true);
                doors.Add(d);
            }
        }
        int first=hull.Voxels.Count;
        hull.AddDecorationBox(transform,metal,PumpPost+new Vector3(0f,.3f,.35f),new Vector3(.5f,.6f,.5f),true);
        hull.AddDecorationBox(transform,trim,PumpPost+new Vector3(0f,.65f,.35f),new Vector3(.8f,.12f,.15f),true);
        for(int i=first;i<hull.Voxels.Count;i++)pumpCells.Add(hull.Voxels[i]);
        crank=new GameObject("Bilge pump handle").transform;crank.SetParent(transform,false);crank.localPosition=PumpPost+new Vector3(0f,.65f,.35f);
        foreach(NavalHullVoxel cell in pumpCells)if(cell.LocalCenter.y>BoatDimensions.HoldFloor+.6f)cell.transform.SetParent(crank,true);
        // Two visible pipes lead from the pump toward side compartment inlets.
        for(int side=-1;side<=1;side+=2)
            hull.AddDecorationBox(transform,metal,new Vector3(side*1.4f,BoatDimensions.HoldFloor+.13f,-5.45f),new Vector3(2.8f,.12f,.12f),true);
    }
    private NavalHullVoxel Wall(Vector3 point,Vector3 size,Material material)
    {NavalHullVoxel c=hull.AddVoxel(transform,material,point,size,false,true);c.InternalBarrier=true;walls.Add(c);return c;}
    public void RequestPassage(Vector3 feet,Vector3 destination)
    {
        if(feet.y>.3f||destination.y>.3f)return;
        foreach(Door d in doors)
            if(Mathf.Abs(feet.x-d.Center.x)<1.1f&&Mathf.Abs(feet.z-d.Center.z)<.95f)d.Until=Time.time+1.5f;
    }
    private bool Doorway(float x,float z,out Door door)
    {
        foreach(Door d in doors)if(Mathf.Abs(x-d.Center.x)<.2f&&Mathf.Abs(z-d.Center.z)<.625f){door=d;return true;}
        door=null;return false;
    }
    private float Sill(int a,int b)
    {
        NavalFloodGrid g=hull.Flood;
        if(NavalCompartmentMath.Region(g.X(a),g.Z(a))==NavalCompartmentMath.Region(g.X(b),g.Z(b)))return float.NegativeInfinity;
        float x=(g.X(a)+g.X(b))*.5f,z=(g.Z(a)+g.Z(b))*.5f;Door door;
        if(Doorway(x,z,out door)&&door.Open)return BoatDimensions.HoldFloor;
        float lowest=float.PositiveInfinity;
        foreach(NavalHullVoxel cell in walls)
        {
            Vector3 p=cell.LocalCenter;
            if((cell.Missing||cell.Detached)&&Mathf.Abs(p.x-x)<.3f&&Mathf.Abs(p.z-z)<.3f)lowest=Mathf.Min(lowest,p.y-.125f);
        }
        return lowest;
    }
    public void UpdateBarriers()
    {
        if(!hull.Definition.Compartments)return;
        bool changed=cachedRevision!=hull.Revision;
        foreach(Door d in doors)
        {
            bool open=d.Manual||Time.time<d.Until;
            if(open!=d.Open){d.Open=open;changed=true;d.Pivot.localRotation=Quaternion.Euler(0f,open?90f:0f,0f);}
        }
        if(changed)
        {
            apertures.Clear();cachedRevision=hull.Revision;
            NavalFloodGrid g=hull.Flood;
            for(int i=0;i<g.Water.Length;i++)
            {if(i%g.Columns+1<g.Columns)apertures[i*2]=Sill(i,i+1);if(i/g.Columns+1<g.Rows)apertures[i*2+1]=Sill(i,i+g.Columns);}
        }
        foreach(NavalHullVoxel cell in walls)
        {
            if(!cell.Missing&&!cell.Detached)continue;
            int index=hull.Flood.Index(cell.LocalCenter.x,cell.LocalCenter.z);
            if(hull.Flood.Depth(index)>cell.LocalCenter.y-BoatDimensions.HoldFloor+.05f)
            {cell.InflowRate=.05f;cell.LastWetAt=Time.time;cell.LastWetFlow=.05f;}
        }
        foreach(KeyValuePair<int,float> edge in apertures)
        {
            int i=edge.Key/2;bool right=edge.Key%2==0;int j=i+(right?1:hull.Flood.Columns);float sill=edge.Value;
            if(!float.IsInfinity(sill))sill=transform.TransformPoint(new Vector3((hull.Flood.X(i)+hull.Flood.X(j))*.5f,sill,(hull.Flood.Z(i)+hull.Flood.Z(j))*.5f)).y;
            if(right)hull.Flood.RightSill[i]=sill;else hull.Flood.ForwardSill[i]=sill;
        }
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
        if(!WantsOperator)return;
        float processed=0f;
        int selectedMode=automatic?0:mode;
        float heel=Mathf.DeltaAngle(0f,transform.eulerAngles.z);
        bool transfer=selectedMode>=2||selectedMode==0&&NavalCompartmentMath.NeedsTransfer(hull.LeftWater,hull.RightWater,heel);
        bool fromPort=selectedMode==2||selectedMode==0&&hull.LeftWater>hull.RightWater;
        int source=transfer?Wettest(fromPort?1:3):-1;
        if(transfer)
        {
            int fore=Wettest(fromPort?2:4);if(source<0||fore>=0&&hull.Flood.Water[fore]>hull.Flood.Water[source])source=fore;
            if(source>=0)
            {
                int destination=hull.Flood.Index(fromPort?2.2f:-2.2f,hull.Flood.Z(source));
                float maximum=selectedMode==0?Mathf.Abs(hull.LeftWater-hull.RightWater)*.5f:8f*dt;
                float moved=NavalCompartmentMath.Move(hull.Flood,source,destination,Mathf.Min(8f*dt,maximum));
                processed=moved;
                if(moved<=.00001f)transfer=false;
                Status="Transfer "+(fromPort?"port -> starboard":"starboard -> port")+" | "+(moved/Mathf.Max(.001f,dt)).ToString("F1")+" m3/s";
            }
            else transfer=false;
        }
        if(!transfer)
        {
            float budget=6f*dt;
            for(int n=0;n<8&&budget>0f;n++){int i=Wettest(-1);if(i<0)break;budget+=hull.Flood.Add(i,-budget);}
            processed=6f*dt-budget;
            Status="Pumping overboard";
        }
        if(processed>.00001f){pumpLastWork=Time.time;pumpPhase=Mathf.Repeat(pumpPhase+dt/1.2f,1f);lastTransfer=transfer;}
        if(crank!=null)crank.localRotation=Quaternion.Euler(Mathf.Sin(Time.time*8f)*25f,0f,0f);
    }
    private void Update()
    {
        PlayerInteracting=false;
        if(player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||Time.timeScale==0f||NavalTestWorld.Active!=null&&NavalTestWorld.Active.ShipMenuOpen)return;
        Vector3 feet=transform.InverseTransformPoint(player.transform.position-Vector3.up*1f);
        bool near=Vector3.Distance(feet,PumpPost)<1.2f;
        Door nearest=null;float best=1.2f;
        foreach(Door d in doors){float distance=Vector3.Distance(feet,d.Center-Vector3.up*.75f);if(distance<best){best=distance;nearest=d;}}
        PlayerInteracting=near||nearest!=null||playerPump;
        if(Input.GetKeyDown(KeyCode.E))
        {
            if(playerPump){playerPump=false;return;}
            if(near){playerPump=true;if(Operator!=null){Operator.CancelPumpDuty();Operator=null;}}
            else if(nearest!=null)nearest.Manual=!nearest.Manual;
        }
        if(playerPump)
        {
            if(!near){playerPump=false;return;}
            if(Input.GetKeyDown(KeyCode.Q))mode=(mode+1)%4;
            if(Input.GetKey(KeyCode.T))Pump(Mathf.Min(Time.deltaTime,.05f),false);
        }
    }
    public bool PlayerUsing {get{return playerPump;}}
    private void OnGUI()
    {
        NavalTestWorld world=NavalTestWorld.Active;
        if(world!=null&&world.PlayerHull==hull&&player!=null&&player.IsAlive()&&player.IsPlayerInputEnabled()&&!hull.Sunk&&Functional&&Time.time-pumpLastWork<.2f)
            NavalWorldProgressBar.Draw(player.GetPlayerCamera(),transform.TransformPoint(PumpPost+new Vector3(0f,1.15f,.35f)),pumpPhase,true,false,lastTransfer?"Перекачка":"Откачка");
        if(!PlayerInteracting)return;
        GUI.Box(new Rect(NavalHudLayout.ContextX(Screen.width,Screen.height,620f),Screen.height-245f,NavalHudLayout.ContextWidth(Screen.width,Screen.height,620f),30f),playerPump?"PUMP: hold T | Q: "+new string[]{"Auto","Overboard","Port -> starboard","Starboard -> port"}[mode]+" | E: leave | "+Status:"E: watertight door / bilge pump");
    }
}

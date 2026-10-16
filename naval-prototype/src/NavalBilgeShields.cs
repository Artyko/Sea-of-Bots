using System.Collections.Generic;
using UnityEngine;

public partial class NavalBilgeSystem
{
    public class FloodShield
    {
        public Transform Panel;
        public Vector3 Opening,Rack,Post;
        public int Region;
        public bool Installed;
        public NavalRepairSailor Worker;
        public float Lease,Work,Retry,PassageUntil,DrySince=-1f;
        public readonly List<NavalHullVoxel> Cells=new List<NavalHullVoxel>();
        public bool Removing;
    }
    private readonly List<FloodShield> shields=new List<FloodShield>();
    private float shieldReview;
    private bool shieldPending;
    public string FloodStatus {get;private set;}
    public string ShieldSummary {get;private set;}
    private void BuildShields(Material wood)
    {
        for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)
        {
            FloodShield s=new FloodShield();
            s.Opening=new Vector3(side*NavalCompartmentMath.WallX,hull.Floor,end*NavalCompartmentMath.DoorZ);
            s.Rack=s.Opening+new Vector3(side*.16f,0f,1.45f);
            s.Post=new Vector3(side*.95f,hull.Floor,s.Opening.z);
            s.Region=NavalCompartmentMath.Region(side*2f,s.Opening.z);
            s.Panel=new GameObject("Removable wooden flood boards").transform;s.Panel.SetParent(transform,false);
            float bottom=NavalCompartmentMath.CoamingHeight(hull.WorldScale),height=1.5f/hull.WorldScale-bottom;
            for(int z=0;z<5;z++)for(int y=0;y<4;y++)
            {
                Vector3 point=s.Opening+new Vector3(0f,bottom+(y+.5f)*height/4f,-.625f+(z+.5f)*.25f);
                NavalHullVoxel cell=hull.AddVoxel(transform,wood,point,new Vector3(.12f,height/4f,.25f),false,true);
                // Equipment manages these movable cells; the fixed hull graph must not detach a stored board.
                cell.MovableEquipment=true;cell.transform.SetParent(s.Panel,true);s.Cells.Add(cell);
            }
            shields.Add(s);PlaceShield(s,false);
        }
        FloodStatus="Отсеки: открыты";ShieldSummary="0 / "+shields.Count;
    }
    private void PlaceShield(FloodShield s,bool installed)
    {
        s.Installed=installed;
        s.Panel.localPosition=installed?Vector3.zero:s.Rack-s.Opening;
        foreach(NavalHullVoxel c in s.Cells)if(c!=null)c.GetComponent<Collider>().isTrigger=!installed;
        cachedRevision=-1;Physics.SyncTransforms();
    }
    private readonly float[] shieldDepth=new float[5],shieldInflow=new float[5];
    private float RegionDepth(int region){return shieldDepth[region];}
    private float RegionInflow(int region){return shieldInflow[region];}
    private bool RoomOccupied(FloodShield s)
    {
        float side=Mathf.Sign(s.Opening.x),end=Mathf.Sign(s.Opening.z);
        Vector3 center=new Vector3(side*2.2f,hull.Floor+1.2f/hull.WorldScale,end*3f);
        Vector3 half=Vector3.Scale(new Vector3(.78f,1.3f/hull.WorldScale,2.95f),transform.lossyScale);
        foreach(Collider c in Physics.OverlapBox(transform.TransformPoint(center),half,transform.rotation))
            if(c.GetComponentInParent<Character>()!=null)return true;
        foreach(NavalRepairSailor w in hull.RepairWorkers)
            if(w!=null&&w.HasJob&&NavalCompartmentMath.Region(w.JobPoint.x,w.JobPoint.z)==s.Region)return true;
        return false;
    }
    private bool WantFit(FloodShield s)
    {
        return !s.Installed&&Time.time>=s.Retry&&Time.time>s.PassageUntil&&
            NavalFloodResponseMath.Isolate(RegionDepth(s.Region),RegionDepth(0),RegionInflow(s.Region))&&!RoomOccupied(s);
    }
    private bool WantRemove(FloodShield s)
    {return s.Installed&&(Time.time<s.PassageUntil||Time.time>=s.Retry&&s.DrySince>=0f&&Time.time-s.DrySince>=5f);}
    private void ReviewShields()
    {
        if(Time.time<shieldReview)return;shieldReview=Time.time+.25f;
        int installed=0;shieldPending=false;
        System.Array.Clear(shieldDepth,0,shieldDepth.Length);System.Array.Clear(shieldInflow,0,shieldInflow.Length);
        for(int i=0;i<hull.Flood.Water.Length;i++)
        {
            int region=NavalCompartmentMath.Region(hull.Flood.X(i),hull.Flood.Z(i));
            shieldDepth[region]=Mathf.Max(shieldDepth[region],hull.Flood.Depth(i)*hull.WorldScale);
        }
        foreach(NavalHullVoxel c in hull.GetDamagedVoxels())if(c.HydraulicShell&&(c.Missing||c.Detached))
            shieldInflow[NavalCompartmentMath.Region(c.LocalCenter.x,c.LocalCenter.z)]+=c.InflowRate;
        foreach(FloodShield s in shields)
        {
            if(s.Worker!=null&&(!s.Worker.AvailableForShield||Time.time>s.Lease)){s.Worker=null;s.Work=0f;}
            if(s.Installed)installed++;
            bool dry=NavalFloodResponseMath.SafeToRemove(RegionDepth(s.Region),RegionDepth(0),RegionInflow(s.Region));
            if(!dry)s.DrySince=-1f;else if(s.DrySince<0f)s.DrySince=Time.time;
            if(s.Worker==null&&(WantRemove(s)||WantFit(s)))shieldPending=true;
        }
        ShieldSummary=installed+" / "+shields.Count;
        FloodStatus="Щиты: "+installed+" / "+shields.Count+" | "+(hull.HasActiveLeaks?"устранение течей":hull.Volume>.12f?"осушение отсеков":"сухо");
    }
    public bool WantsShieldWorker
    {
        get{return shieldPending;}
    }
    public FloodShield ClaimShield(NavalRepairSailor worker)
    {
        // Opening escape routes takes precedence over isolation.
        for(int pass=0;pass<2;pass++)foreach(FloodShield s in shields)
            if(s.Worker==null&&(pass==0?WantRemove(s):WantFit(s)))
            {s.Worker=worker;s.Removing=s.Installed;s.Work=0f;s.Lease=Time.time+1f;return s;}
        return null;
    }
    public void ReleaseShieldClaims(NavalRepairSailor worker)
    {foreach(FloodShield s in shields)ReleaseShield(s,worker,false);}
    public void ReleaseShield(FloodShield s,NavalRepairSailor worker,bool blocked)
    {
        if(s==null||s.Worker!=worker)return;
        s.Worker=null;s.Work=0f;if(blocked)s.Retry=Time.time+8f;
    }
    public bool WorkShield(FloodShield s,NavalRepairSailor worker,float dt)
    {
        if(s.Worker!=worker)return false;s.Lease=Time.time+1f;
        if(!s.Removing&&(RoomOccupied(s)||Time.time<s.PassageUntil||!NavalFloodResponseMath.Isolate(RegionDepth(s.Region),RegionDepth(0),RegionInflow(s.Region))))return false;
        s.Work+=dt;
        if(s.Work<(s.Removing?NavalFloodResponseMath.RemoveSeconds:NavalFloodResponseMath.FitSeconds))return true;
        if(!s.Removing)foreach(NavalHullVoxel c in s.Cells)if(c.Missing)c.SetMissing(false);
        PlaceShield(s,!s.Removing);s.Retry=Time.time+(s.Removing?NavalFloodResponseMath.ReopenDelay:0f);s.DrySince=-1f;
        s.Worker=null;s.Work=0f;return true;
    }
    private float ShieldSill(float x,float z)
    {
        float bottom=hull.Floor+NavalCompartmentMath.CoamingHeight(hull.WorldScale);
        foreach(FloodShield s in shields)if(s.Installed&&Mathf.Abs(x-s.Opening.x)<.2f&&Mathf.Abs(z-s.Opening.z)<.625f)
        {
            float level=hull.Floor+1.5f/hull.WorldScale;
            foreach(NavalHullVoxel c in s.Cells)if((c.Missing||c.Detached)&&Mathf.Abs(c.LocalCenter.z-z)<.3f)
                level=Mathf.Min(level,c.LocalCenter.y-c.transform.localScale.y*.5f);
            return Mathf.Max(bottom,level);
        }
        return bottom;
    }
    private Vector3 ShieldPassage(Vector3 feet,Vector3 goal,NavalRepairSailor worker,float dt)
    {
        if(feet.y>hull.Floor+1.6f/hull.WorldScale)return goal;
        foreach(FloodShield s in shields)
        {
            if(!s.Installed||!NavalFloodResponseMath.Crosses(feet.x,feet.z,goal.x,goal.z,s.Opening.x,s.Opening.z))continue;
            s.PassageUntil=Time.time+3f;s.DrySince=-1f;
            // The approaching sailor removes the boards himself, even during another job or evacuation.
            if(worker!=null&&Mathf.Abs(feet.x-s.Opening.x)<.85f&&Mathf.Abs(feet.z-s.Opening.z)<.8f)
            {
                if(s.Worker==null||s.Worker==worker)
                {
                    s.Worker=worker;s.Removing=true;
                    worker.ShieldPassageWork();WorkShield(s,worker,dt);
                }
                if(s.Installed){worker.WaitForPassage();return feet;}
            }
            else if(Mathf.Abs(feet.x-s.Opening.x)<.65f){if(worker!=null)worker.WaitForPassage();return feet;}
        }
        return goal;
    }
}

using UnityEngine;

public partial class NavalStations
{
    internal NavalAntiAircraft AirDefense {get;private set;}
    internal void BuildAirDefense(Material metal,Material accent)
    {
        if(!hull.Definition.SteamPowered)return;
        AirDefense=boat.gameObject.AddComponent<NavalAntiAircraft>();
        AirDefense.Initialize(hull,this,metal,accent);
    }
}

public sealed class NavalAntiAircraft : MonoBehaviour
{
    private sealed class Mount
    {
        public Vector3 Post;
        public Transform Pivot,Barrel;
        public NavalRepairableMechanism Mechanism;
        public float BaseYaw,Yaw,Pitch,NextShot,ReviewAt;
        public int Burst;
        public int CrewId;public bool Manned;
        public Vector3 Aim;
        public bool HasAim;
    }
    private Mount[] mounts;
    private NavalHull hull;
    private NavalStations controls;
    private int playerMount=-1,nearbyMount=-1;
    public string Diagnostics
    {
        get
        {
            var result=new System.Text.StringBuilder();
            for(int i=0;i<mounts.Length;i++)
            {
                var m=mounts[i];
                result.Append(" mount=").Append(i+1).Append(" functional=").Append(m.Mechanism.Functional)
                    .Append(" assigned=").Append(m.CrewId).Append(" manned=").Append(m.Manned);
            }
            return result.ToString();
        }
    }
    public void ResetCrewStatus(){foreach(var m in mounts){m.CrewId=0;m.Manned=false;}}
    public int Count {get{return mounts.Length;}}
    public bool PlayerOperating {get{return playerMount>=0;}}
    public Vector3 Post(int index){return mounts[index].Post;}
    public bool Available(int index){return index!=playerMount&&mounts[index].Mechanism.Functional;}
    public void Initialize(NavalHull owner,NavalStations stations,Material metal,Material accent)
    {
        hull=owner;controls=stations;
        mounts=new Mount[hull.Definition.CapitalShip?4:hull.Definition.Destroyer?2:3];
        float scale=hull.WorldScale;
        for(int i=0;i<mounts.Length;i++)
        {
            float side=i%2==0?-1f:1f,z=mounts.Length==2?(i==0?2.7f:-2.7f):i<2?-2.55f:2.85f;
            float edge=hull.Definition.HullHalfWidth(z);
            float x=side*(edge-2.7f/scale),y=hull.Definition.WeatherDeck;
            Vector3 basePoint=new Vector3(x,y,z);
            var mount=new Mount{Post=basePoint+new Vector3(-side*1.15f,0,-.55f)/scale,BaseYaw=side*90f,Yaw=side*90f,Pitch=20f};
            int start=hull.Voxels.Count;
            hull.AddDecorationBox(transform,accent,basePoint+Vector3.up*.035f/scale,new Vector3(1.20f,.31f,1.10f)/scale,true);
            hull.AddDecorationBox(transform,metal,basePoint+Vector3.up*.55f/scale,new Vector3(.56f,1.0f,.57f)/scale,true);
            mount.Pivot=new GameObject("AA machine gun").transform;
            mount.Pivot.SetParent(transform,false);mount.Pivot.localPosition=basePoint+Vector3.up*1.26f/scale;
            hull.AddDecorationBox(mount.Pivot,accent,new Vector3(0,.08f,.22f)/scale,new Vector3(1.02f,.74f,.13f)/scale,false);
            mount.Barrel=new GameObject("AA barrel pivot").transform;mount.Barrel.SetParent(mount.Pivot,false);
            hull.AddDecorationBox(mount.Barrel,metal,new Vector3(0,.02f,.20f)/scale,new Vector3(.36f,.36f,.74f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,metal,new Vector3(0,.02f,.98f)/scale,new Vector3(.18f,.18f,1.38f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,accent,new Vector3(0,.02f,1.61f)/scale,new Vector3(.26f,.26f,.18f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,accent,new Vector3(.28f,-.13f,.10f)/scale,new Vector3(.28f,.32f,.43f)/scale,false);
            var cells=hull.Voxels.GetRange(start,hull.Voxels.Count-start);
            foreach(var cell in cells){cell.Metallic=true;if(cell.transform.parent!=transform)cell.MovableEquipment=true;}
            mount.Mechanism=new NavalRepairableMechanism(hull,cells,false,false,"\u0417\u0435\u043d\u0438\u0442\u043d\u044b\u0439 \u043f\u0443\u043b\u0435\u043c\u0451\u0442");
            mounts[i]=mount;Pose(mount);
            Debug.Log("[NavalPrototype] AA installed: ship="+hull.Root.name+" mount="+(i+1)+" post="+mount.Post+" cells="+cells.Count);
        }
    }
    private static void Pose(Mount mount)
    {
        mount.Pivot.localRotation=Quaternion.Euler(0,mount.Yaw,0);
        mount.Barrel.localRotation=Quaternion.Euler(-mount.Pitch,0,0);
    }
    public void SetOperator(int index,Character sailor,bool manned)
    {mounts[index].CrewId=sailor.GetInstanceID();mounts[index].Manned=manned;}
    public void TickOperator(int index,NavalAirRaid raid,float dt)
    {
        if(!Available(index)||!raid.HasAircraft)return;
        using(NavalPerformance.Measure(NavalPerfArea.AirDefense))
        {
            var m=mounts[index];
            if(Time.time>=m.ReviewAt)
            {
                m.ReviewAt=Time.time+.10f;
                m.HasAim=raid.AimAtAircraft(transform,m.Pivot.position,m.BaseYaw,out m.Aim);
            }
            if(!m.HasAim)return;
            Vector3 local=transform.InverseTransformDirection(m.Aim-m.Pivot.position);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            if(Mathf.Abs(Mathf.DeltaAngle(m.BaseYaw,yaw))>115f||pitch< -8f||pitch>85f)return;
            m.Yaw=Mathf.MoveTowardsAngle(m.Yaw,yaw,150f*dt);
            m.Pitch=Mathf.MoveTowards(m.Pitch,pitch,110f*dt);Pose(m);
            if(Mathf.Abs(Mathf.DeltaAngle(m.Yaw,yaw))<3f&&Mathf.Abs(m.Pitch-pitch)<3f)Fire(m,raid);
        }
    }
    private void Fire(Mount m,NavalAirRaid raid)
    {
        if(Time.time<m.NextShot||raid==null)return;
        m.Burst++;
        m.NextShot=Time.time+(m.Burst>=5?.42f:.095f);
        if(m.Burst>=5)m.Burst=0;
        Vector3 direction=Quaternion.Euler(Random.Range(-.32f,.32f),Random.Range(-.35f,.35f),0)*m.Barrel.forward;
        raid.FireAA(m.Barrel.position+m.Barrel.forward*1.78f,direction);
    }
    public bool TickPlayer(FirstPersonMover player,NavalAirRaid raid,bool allowed)
    {
        nearbyMount=-1;
        if(playerMount>=0)
        {
            var m=mounts[playerMount];
            if(!allowed||!m.Mechanism.Functional||Input.GetKeyDown(KeyCode.E))
            {playerMount=-1;return true;}
            float yaw=Mathf.DeltaAngle(m.BaseYaw,m.Yaw)+Input.GetAxis("Mouse X")*3.5f;
            m.Yaw=m.BaseYaw+Mathf.Clamp(yaw,-115f,115f);
            m.Pitch=Mathf.Clamp(m.Pitch-Input.GetAxis("Mouse Y")*3.5f,-8f,85f);
            Pose(m);if(Input.GetMouseButton(0))Fire(m,raid);return true;
        }
        if(!allowed||controls.Operating)return false;
        int nearest=-1;float best=2.1f*2.1f;
        for(int i=0;i<Count;i++)
        {
            float distance=(player.transform.position-transform.TransformPoint(mounts[i].Post)).sqrMagnitude;
            if(distance<best&&Available(i)){nearest=i;best=distance;}
        }
        nearbyMount=nearest;
        if(nearest<0||!Input.GetKeyDown(KeyCode.E))return false;
        playerMount=nearest;return true;
    }
    private void OnGUI()
    {
        var world=NavalTestWorld.Active;
        if(world==null||!world.AirRaidMission||Event.current.type!=EventType.Repaint||hull.Sunk)return;
        Camera camera=world.ViewCamera;
        if(camera!=null&&!controls.EnemyControlled)
            for(int i=0;i<mounts.Length;i++)
            {
                var mount=mounts[i];
                if(!mount.Mechanism.Functional||(mount.Pivot.position-camera.transform.position).sqrMagnitude>45f*45f)continue;
                Vector3 p=camera.WorldToScreenPoint(mount.Pivot.position+transform.up*.7f);
                if(p.z>0&&p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height)
                    GUI.Box(new Rect(p.x-36f,Screen.height-p.y-22f,72f,21f),"\u041f\u0412\u041e "+(i+1));
            }
        if(PlayerOperating)GUI.Box(new Rect(Screen.width*.5f-185,Screen.height-90,370,40),
            "\u0417\u0435\u043d\u0438\u0442\u043d\u044b\u0439 \u043f\u0443\u043b\u0435\u043c\u0451\u0442 \u00b7 \u043c\u044b\u0448\u044c \u2014 \u043d\u0430\u0432\u043e\u0434\u043a\u0430 \u00b7 \u041b\u041a\u041c \u2014 \u043e\u0433\u043e\u043d\u044c \u00b7 E \u2014 \u0432\u044b\u0439\u0442\u0438");
        else if(nearbyMount>=0)GUI.Box(new Rect(Screen.width*.5f-140,Screen.height-90,280,34),
            "E — \u0437\u0430\u043d\u044f\u0442\u044c \u0437\u0435\u043d\u0438\u0442\u043d\u044b\u0439 \u043f\u0443\u043b\u0435\u043c\u0451\u0442");
    }
}

public partial class NavalCrewController
{
    private void AssignAirDefense(bool emergency)
    {
        var world=NavalTestWorld.Active;
        var battery=stations.AirDefense;
        if(battery==null)return;
        battery.ResetCrewStatus();
        if(emergency||world==null||world.AirRaid==null||!world.AirRaid.NeedsAirDefense)return;
        for(int mount=0;mount<battery.Count;mount++)
        {
            if(!battery.Available(mount))continue;
            int selected=-1;float best=float.PositiveInfinity;
            Vector3 post=hull.Root.TransformPoint(battery.Post(mount));
            for(int i=0;i<crew.Length;i++)
            {
                if(assigned[i]>=0||!Free(i))continue;
                if(roles[i]==400+mount){selected=i;break;}
                float distance=(crew[i].transform.position-post).sqrMagnitude;
                if(distance<best){best=distance;selected=i;}
            }
            if(selected>=0)assigned[selected]=400+mount;
        }
    }
    private bool TickAirDefenseHand(int index,float dt)
    {
        int role=assigned[index];
        if(role<400||role>=404)return false;
        var battery=stations.AirDefense;
        Vector3 post=battery.Post(role-400);
        var worker=workers[index];
        roles[index]=role;worker.SpareMaintenance=false;worker.StationDutyProtected=true;
        worker.AssignFreeCombatPost(post);worker.SetPost(post);worker.Tick(dt,false);
        bool manned=!worker.Busy&&worker.AtStation(post);
        battery.SetOperator(role-400,crew[index],manned);
        if(manned)
        {
            worker.AnchorAtPost(post);
            battery.TickOperator(role-400,NavalTestWorld.Active.AirRaid,dt);
        }
        return true;
    }
}

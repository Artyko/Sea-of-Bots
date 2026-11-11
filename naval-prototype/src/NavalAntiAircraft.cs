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

[DefaultExecutionOrder(700)]
public sealed class NavalAntiAircraft : MonoBehaviour
{
    private sealed class Mount
    {
        public Vector3 Post;
        public Transform Pivot,Barrel;
        public NavalRepairableMechanism Mechanism;
        public float BaseYaw,Yaw,Pitch,NextShot,ReviewAt,AimRange;
        public float FiringSince=-1f,LastTrigger;
        public int CrewId;public bool Manned;
        public Vector3 Aim;
        public bool HasAim,Detached;
        public System.Collections.Generic.List<NavalHullVoxel> Cells,Anchors,DeckSupports;
    }
    private Mount[] mounts;
    private NavalHull hull;
    private NavalStations controls;
    private int playerMount=-1,nearbyMount=-1;
    private float warmup=.5f,bulletInterval=.06f;
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
    public bool Available(int index){return index!=playerMount&&!mounts[index].Detached&&mounts[index].Mechanism.Functional;}
    public void Initialize(NavalHull owner,NavalStations stations,Material metal,Material accent)
    {
        hull=owner;controls=stations;
        foreach(var spider in Resources.FindObjectsOfTypeAll<MortarWalker>())
        {
            if(!spider.UseMindSpaceGrenade)continue;
            warmup=Mathf.Max(0f,spider.MachineGunWarmupTime);
            bulletInterval=Mathf.Max(.015f,spider.WaitBetweenMachineGunBullets);
            Debug.Log("[NavalPrototype] Mindspace AA profile: warmup="+warmup+" interval="+bulletInterval);
            break;
        }
        mounts=new Mount[hull.Definition.CapitalShip?4:hull.Definition.Destroyer?2:3];
        float scale=hull.WorldScale;
        for(int i=0;i<mounts.Length;i++)
        {
            float side=i%2==0?-1f:1f;
            // Separate inboard mounts from the outboard casemate line and the
            // centre stair opening. Novik keeps both side stair lanes clear.
            float z=hull.Definition.Destroyer?(i==0?26f:-26f)/scale:
                hull.Definition.CapitalShip?(i<2?-2.22f:.63f):(i<2?-2.02f:1.65f);
            float x=side*(hull.Definition.Destroyer?2.05f/scale:1.33f);
            Vector3 basePoint=new Vector3(x,hull.Definition.WeatherDeck,z);
            float approach=hull.Definition.Destroyer?(z>0?-1f:1f):1f;
            var mount=new Mount{Post=basePoint+new Vector3(0f,0f,approach*1.18f)/scale,BaseYaw=side*90f,Yaw=side*90f,Pitch=20f};
            if(hull.Definition.CapitalShip)
            {
                basePoint=new Vector3(side*1.32f,i<2?NavalRaisedBridge.Top(hull.Definition):NavalCapitalAADeck.Top(hull.Definition),
                    i<2?NavalRaisedBridge.Center(hull.Definition):NavalCapitalAADeck.Z);
                mount.Post=basePoint+Vector3.forward*(1.0f/scale);
            }
            int start=hull.Voxels.Count;
            const float lift=.32f;
            hull.AddDecorationBox(transform,metal,basePoint+Vector3.up*.12f/scale,new Vector3(.66f,.40f,.64f)/scale,true);
            hull.AddDecorationBox(transform,metal,basePoint+Vector3.up*(.55f+lift*.5f)/scale,new Vector3(.21f,.94f+lift,.22f)/scale,true);
            for(int leg=-1;leg<=1;leg+=2)
                hull.AddDecorationBox(transform,metal,basePoint+new Vector3(leg*.27f,.13f,-.10f)/scale,new Vector3(.33f,.23f,.26f)/scale,false);
            mount.Pivot=new GameObject("AA machine gun").transform;
            mount.Pivot.SetParent(transform,false);mount.Pivot.localPosition=basePoint+Vector3.up*(1.12f+lift)/scale;
            mount.Barrel=new GameObject("AA barrel pivot").transform;mount.Barrel.SetParent(mount.Pivot,false);
            hull.AddDecorationBox(mount.Barrel,metal,new Vector3(0,0,-.05f)/scale,new Vector3(.26f,.24f,.61f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,metal,new Vector3(0,.015f,.63f)/scale,new Vector3(.105f,.105f,.88f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,metal,new Vector3(0,.015f,1.09f)/scale,new Vector3(.15f,.15f,.13f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,accent,new Vector3(.24f,-.055f,-.04f)/scale,new Vector3(.29f,.24f,.38f)/scale,false);
            hull.AddDecorationBox(mount.Barrel,accent,new Vector3(0,-.035f,-.42f)/scale,new Vector3(.15f,.15f,.27f)/scale,false);
            var cells=hull.Voxels.GetRange(start,hull.Voxels.Count-start);
            foreach(var cell in cells){cell.Metallic=true;if(cell.transform.parent!=transform)cell.MovableEquipment=true;}
            mount.Cells=cells;mount.Anchors=new System.Collections.Generic.List<NavalHullVoxel>();
            foreach(var cell in cells)if(!cell.MovableEquipment&&Vector3.Distance(cell.transform.position,transform.TransformPoint(basePoint))<.5f)mount.Anchors.Add(cell);
            mount.DeckSupports=new System.Collections.Generic.List<NavalHullVoxel>();
            Vector3 foot=hull.Root.InverseTransformPoint(transform.TransformPoint(basePoint));
            for(int support=0;support<start;support++){
                var cell=hull.Voxels[support];if(cell.MovableEquipment||cell.RepairableEquipment||cell.NonPhysicalDecoration)continue;
                Vector3 size=cell.transform.localScale,p=cell.LocalCenter;
                if(Mathf.Abs(p.x-foot.x)*scale<=Mathf.Abs(size.x)*scale*.5f+.08f&&Mathf.Abs(p.z-foot.z)*scale<=Mathf.Abs(size.z)*scale*.5f+.08f&&
                   Mathf.Abs((p.y+Mathf.Abs(size.y)*.5f-foot.y)*scale)<.35f)mount.DeckSupports.Add(cell);
            }
            mount.Mechanism=new NavalRepairableMechanism(hull,cells,false,false,"\u0417\u0435\u043d\u0438\u0442\u043d\u044b\u0439 \u043f\u0443\u043b\u0435\u043c\u0451\u0442");
            mounts[i]=mount;Pose(mount);
            Debug.Log("[NavalPrototype] AA installed: ship="+hull.Root.name+" mount="+(i+1)+" post="+mount.Post+" cells="+cells.Count+" anchors="+mount.Anchors.Count+" deck_supports="+mount.DeckSupports.Count);
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
                m.HasAim=raid.AimAtAircraft(transform,m.Pivot.position,m.BaseYaw,out m.Aim,out m.AimRange);
            }
            if(!m.HasAim)return;
            Vector3 local=transform.InverseTransformDirection(m.Aim-m.Pivot.position);
            float yaw=Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg;
            float pitch=Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg;
            if(Mathf.Abs(Mathf.DeltaAngle(m.BaseYaw,yaw))>115f||pitch< -8f||pitch>85f)return;
            m.Yaw=Mathf.MoveTowardsAngle(m.Yaw,yaw,150f*dt);
            m.Pitch=Mathf.MoveTowards(m.Pitch,pitch,110f*dt);Pose(m);
            if(Mathf.Abs(Mathf.DeltaAngle(m.Yaw,yaw))<3f&&Mathf.Abs(m.Pitch-pitch)<3f)Fire(m,raid,m.AimRange);
        }
    }
    private void Fire(Mount m,NavalAirRaid raid,float range)
    {
        if(raid==null)return;
        if(m.FiringSince<0f||Time.time-m.LastTrigger>.15f)m.FiringSince=Time.time;
        m.LastTrigger=Time.time;
        if(Time.time<m.NextShot)return;
        m.NextShot=Time.time+bulletInterval;
        float spread=Mathf.Lerp(1.8f,4.8f,Mathf.InverseLerp(100f,540f,range));
        Vector2 jitter=new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f))*spread;
        Vector3 direction=Quaternion.Euler(jitter.x,jitter.y,0f)*m.Barrel.forward;
        raid.FireAA(m.Barrel.position+m.Barrel.forward*1.19f,direction);
    }
    public bool TickPlayer(FirstPersonMover player,NavalAirRaid raid,bool allowed)
    {
        controlledPlayer=player;nearbyMount=-1;
        if(playerMount>=0)
        {
            var m=mounts[playerMount];
            if(!allowed||!m.Mechanism.Functional||Input.GetKeyDown(KeyCode.E))
            {playerMount=-1;return true;}
            float yaw=Mathf.DeltaAngle(m.BaseYaw,m.Yaw)+Input.GetAxis("Mouse X")*3.5f;
            m.Yaw=m.BaseYaw+Mathf.Clamp(yaw,-115f,115f);
            m.Pitch=Mathf.Clamp(m.Pitch+Input.GetAxis("Mouse Y")*3.5f,-8f,85f);
            Pose(m);if(Input.GetMouseButton(0))Fire(m,raid,300f);return true;
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
    private int supportRevision=-1;
    private void ReviewSupport(){
        if(hull==null||mounts==null||supportRevision==hull.Revision)return;supportRevision=hull.Revision;
        var structure=GetComponent<NavalStructure>();if(structure==null)return;
        foreach(var mount in mounts){
            if(mount.Detached){bool restored=true;foreach(var cell in mount.Cells)if(cell!=null&&(cell.Missing||cell.Detached)){restored=false;break;}if(restored)mount.Detached=false;else continue;}

            bool attached=false;foreach(var cell in mount.Anchors)if(cell!=null&&!cell.Missing&&!cell.Detached){attached=true;break;}
            bool deckAttached=false;foreach(var cell in mount.DeckSupports)if(cell!=null&&!cell.Missing&&!cell.Detached){deckAttached=true;break;}
            if(attached&&deckAttached)continue;
            mount.Detached=true;mount.Manned=false;mount.HasAim=false;
            var world=NavalTestWorld.Active;Vector3 velocity=world!=null?world.ConvoyShipVelocity(hull):Vector3.zero;
            structure.DropEquipment(mount.Cells,velocity);
            Debug.Log("[NavalPrototype] AA detached: ship="+hull.Root.name+" post="+mount.Post+" reason=lost pedestal or supporting deck");
        }
    }
    private FirstPersonMover controlledPlayer;
    private readonly System.Collections.Generic.Dictionary<Renderer,bool> hiddenPlayer=new System.Collections.Generic.Dictionary<Renderer,bool>();
    private void HideOperator(){
        if(controlledPlayer==null)return;
        if(hiddenPlayer.Count==0)foreach(var renderer in controlledPlayer.GetComponentsInChildren<Renderer>(true))hiddenPlayer[renderer]=renderer.forceRenderingOff;
        foreach(var item in hiddenPlayer)if(item.Key!=null)item.Key.forceRenderingOff=true;
    }
    private void RestoreOperator(){foreach(var item in hiddenPlayer)if(item.Key!=null)item.Key.forceRenderingOff=item.Value;hiddenPlayer.Clear();}
    private Camera sightCamera;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedNearClip;

    private void LateUpdate()
    {
        ReviewSupport();
        if(PlayerOperating&&mounts[playerMount].Detached)playerMount=-1;
        if(!PlayerOperating){RestoreSightCamera();return;}
        HideOperator();
        var world=NavalTestWorld.Active;
        Camera camera=world!=null?world.ViewCamera:null;
        if(camera==null)return;
        if(sightCamera!=camera)
        {
            RestoreSightCamera();sightCamera=camera;
            savedCameraPosition=camera.transform.localPosition;
            savedCameraRotation=camera.transform.localRotation;
            savedNearClip=camera.nearClipPlane;
        }
        var mount=mounts[playerMount];
        Vector3 position=mount.Barrel.position-mount.Barrel.forward*.75f+mount.Barrel.up*.20f;
        Vector3 aim=mount.Barrel.position+mount.Barrel.forward*540f;
        camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(aim-position,mount.Pivot.up));
        camera.nearClipPlane=.05f;
    }
    private void RestoreSightCamera()
    {
        RestoreOperator();
        if(sightCamera==null)return;
        sightCamera.transform.localPosition=savedCameraPosition;
        sightCamera.transform.localRotation=savedCameraRotation;
        sightCamera.nearClipPlane=savedNearClip;
        sightCamera=null;
    }
    private void OnDisable(){RestoreSightCamera();}
    private void OnDestroy(){RestoreSightCamera();}
    private void OnGUI()
    {
        var world=NavalTestWorld.Active;
        if(world==null||world.AirRaid==null||Event.current.type!=EventType.Repaint||hull.Sunk)return;
        if(PlayerOperating)
        {
            float x=Screen.width*.5f,y=Screen.height*.5f;
            GUI.Label(new Rect(x-8f,y-12f,24f,24f),"+");
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
        if(world==null||!world.DefendsAgainstRaid(hull)||world.AirRaid==null||!world.AirRaid.HasAircraft)return;
        int occupied=0,limit=NavalStandbyWaterPolicy.AALimit(emergency,battery.Count);
        for(int mount=0;mount<battery.Count&&occupied<limit;mount++)
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
            if(selected>=0){assigned[selected]=400+mount;occupied++;}
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

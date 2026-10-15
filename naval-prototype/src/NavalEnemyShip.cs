using System.Collections.Generic;
using System.Reflection;
using ModLibrary;
using UnityEngine;

public class NavalEnemyShip : MonoBehaviour
{
    public Transform Boat { get; private set; }
    public NavalHull Hull { get; private set; }
    public Vector3 ShipVelocity { get { return Boat!=null?Boat.forward*speed:Vector3.zero; } }
    private readonly Character[] gunners=new Character[2];
    private readonly NavalRepairSailor[] gunWorkers=new NavalRepairSailor[2];
    private readonly NavalCrewController crewRoles=new NavalCrewController();
    private NavalRamDamage ram;
    private Transform target;
    private FirstPersonMover player;
    private NavalWater ocean;
    private NavalStations stations;
    private NavalShipVisuals rig;
    private NavalStructure structure;
    private Transform[] water;
    private readonly Character[] crew=new Character[3];
    private readonly bool[] recruited=new bool[3];
    private readonly NavalRepairSailor[] repairers=new NavalRepairSailor[3];
    private readonly List<Material> materials=new List<Material>();
    private float heading,speed,verticalSpeed,footOffset,retryCrew,rudder;
    private float nextTack;
    private float orbitSide=1f;
    private static readonly FieldInfo Velocity=typeof(FirstPersonMover).GetField("_velocity",BindingFlags.Instance|BindingFlags.NonPublic);
    public void Initialize(FirstPersonMover observer,Transform opponent,NavalWater sea,float feet)
    {
        player=observer;target=opponent;ocean=sea;footOffset=feet;
        Boat=new GameObject("EnemyWoodenShip").transform;Boat.SetParent(transform,false);
        Boat.localScale=Vector3.one*BoatDimensions.WorldScale;
        Vector3 facing=target.forward;facing.y=0f;facing.Normalize();
        Boat.position=target.position+facing*100f;
        Boat.rotation=Quaternion.LookRotation(-facing);heading=Boat.eulerAngles.y;
        Material wood=Mat(new Color(0.30f,0.19f,0.115f)),trim=Mat(new Color(0.66f,0.51f,0.32f));
        Material deck=Mat(new Color(0.47f,0.375f,0.255f)),plank=Mat(new Color(0.515f,0.415f,0.285f));
        Material iron=Mat(new Color(0.025f,0.025f,0.03f));
        Hull=new NavalHull();Hull.Build(Boat,wood,ocean.InteriorMaterial,deck);
        rig=Boat.gameObject.AddComponent<NavalShipVisuals>();
        rig.Initialize(Hull,wood,trim,iron,Mat(new Color(0.48f,0.15f,0.12f)),Mat(new Color(0.12f,0.085f,0.05f)),deck,plank);
        stations=gameObject.AddComponent<NavalStations>();stations.EnemyControlled=true;
        stations.Initialize(Boat,Hull,player,ocean,wood,trim,iron);
        Boat.gameObject.AddComponent<NavalSeaLadder>().Initialize(Hull,trim);
        structure=Boat.gameObject.AddComponent<NavalStructure>();structure.Initialize(Hull);
        ram=Boat.gameObject.AddComponent<NavalRamDamage>();ram.Initialize(Hull);
        Boat.gameObject.AddComponent<NavalVoxelBatch>().Initialize(Hull);
        crewRoles.Initialize(Hull,stations,gunners,gunWorkers);
        water=new Transform[Hull.Flood.Water.Length];
        for(int i=0;i<water.Length;i++)
        {
            GameObject cell=GameObject.CreatePrimitive(PrimitiveType.Cube);cell.name="EnemyFloodCell";
            cell.transform.SetParent(Boat,false);cell.GetComponent<Collider>().enabled=false;Destroy(cell.GetComponent<Collider>());
            cell.GetComponent<Renderer>().sharedMaterial=ocean.InteriorMaterial;
            water[i]=cell.transform;cell.SetActive(false);
        }
        ocean.AdditionalHull=Boat;
        nextTack=Time.time+30f;
        RecruitCrew();
    }
    private Material Mat(Color color)
    {
        Shader shader=Shader.Find("Unlit/Color");if(shader==null)shader=Shader.Find("Standard");
        Material material=new Material(shader);material.color=color;materials.Add(material);return material;
    }
    private void RecruitCrew()
    {
        EnemyFactory factory=Singleton<EnemyFactory>.Instance;if(factory==null)return;
        for(int i=0;i<3;i++)if(!recruited[i])
        {
            Vector3 position=stations.CrewPosition(i)+Vector3.up*footOffset*0.85f;
            Transform spawned=factory.SpawnEnemy(i==0?EnemyType.Spear2:EnemyType.Hammer1,position,position+Boat.forward,null);
            if(spawned==null)continue;
            Character sailor=spawned.GetComponent<Character>();
            if(sailor==null){Destroy(spawned.gameObject);continue;}
            spawned.SetParent(transform,true);spawned.localScale=Vector3.one*0.85f;
            sailor.IsPlayerTeam=false;sailor.SetIgnoreForVictoryCount(true);sailor.DisableAI();
            crew[i]=sailor;recruited[i]=true;
            repairers[i]=spawned.gameObject.AddComponent<NavalRepairSailor>();
            repairers[i].Initialize(sailor,Hull,structure,i==0?new Vector3(0f,0.825f,-4.8f):new Vector3(i==1?-1.35f:1.35f,0.825f,-1f),footOffset*0.85f);
            spawned.gameObject.AddComponent<NavalSailorRescue>().Initialize(sailor,repairers[i],Hull);
            if(i>0)
            {
                gunners[i-1]=sailor;gunWorkers[i-1]=repairers[i];
            }
        }
    }
    private bool Alive(int i){return crew[i]!=null&&crew[i].IsAlive();}
    private bool CanHelm()
    {return Alive(0)&&repairers[0]!=null&&!repairers[0].Rescuing&&!repairers[0].Assaulting;}
    private void LateUpdate()
    {
        if(player==null||target==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||Time.timeScale==0f)return;
        float dt=Mathf.Min(Time.deltaTime,0.05f);
        if(Alive(0))crew[0].GetComponent<NavalSailorRescue>().CheckForWater();
        bool rider=false;
        if(NavalTestWorld.Active==null||!NavalTestWorld.Active.IsBoarding)
            foreach(RaycastHit support in Physics.SphereCastAll(player.transform.position+Vector3.up*0.2f,0.12f,
                Vector3.down,footOffset+0.4f,PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore))
                if(support.collider.transform.IsChildOf(Boat)&&support.normal.y>0.35f){rider=true;break;}
        Vector3 riderLocal=Boat.InverseTransformPoint(player.transform.position);
        if(Time.time>=retryCrew){retryCrew=Time.time+2f;RecruitCrew();}
        structure.Refresh(Boat.forward*speed+Vector3.up*verticalSpeed);
        Hull.Step(Boat,ocean,dt,false);rig.RefreshRig();
        float throttle=0f,turn=0f;
        if(!Hull.Sunk&&CanHelm()&&stations.IsFunctional(0))
        {
            Vector3 toward=target.position-Boat.position;toward.y=0f;float distance=toward.magnitude;
            if(Time.time>=nextTack&&distance>30f){orbitSide=-orbitSide;nextTack=Time.time+35f;}
            Vector3 radial=toward.normalized;
            Vector3 desired=Vector3.Cross(Vector3.up,radial)*orbitSide+radial*Mathf.Clamp((distance-45f)/20f,-1f,1f);
            if(NavalRamMath.Assault(stations.IsFunctional(1),stations.IsFunctional(2)))desired=radial;
            if(NavalWorldMap.Active!=null)desired=NavalWorldMap.Active.AvoidLand(Boat.position,desired,Boat);
            float wanted=Mathf.Atan2(desired.x,desired.z)*Mathf.Rad2Deg;
            turn=Mathf.Clamp(Mathf.DeltaAngle(heading,wanted)/25f,-1f,1f);
            throttle=distance<24f?0.35f:0.8f;
            if(NavalRamMath.Assault(stations.IsFunctional(1),stations.IsFunctional(2)))throttle=0.7f;
        }
        speed=NavalRigMath.StepSpeed(speed,throttle,rig.SailPowerFactor,dt);
        float depth=Hull.Volume/(BoatDimensions.HoldArea*BoatDimensions.AreaScale);
        rudder=NavalRigMath.StepRudder(rudder,turn,dt);
        heading+=NavalRigMath.RudderRate(speed,rudder)*dt/(1f+depth);
        Quaternion flat=Quaternion.Euler(0f,heading,0f);Vector3 forward=flat*Vector3.forward,right=flat*Vector3.right;
        Vector3 p=Boat.position;
        float bow=ocean.Height(p.x+forward.x*9f,p.z+forward.z*9f),stern=ocean.Height(p.x-forward.x*9f,p.z-forward.z*9f);
        float starboard=ocean.Height(p.x+right.x*5.4f,p.z+right.z*5.4f),port=ocean.Height(p.x-right.x*5.4f,p.z-right.z*5.4f);
        float pitch=-Mathf.Atan2(bow-stern,18f)*Mathf.Rad2Deg+Mathf.Clamp(Hull.Flood.LongitudinalMoment/(BoatDimensions.HalfHoldArea*BoatDimensions.VolumeScale)*15f,-15f,15f);
        float roll=Mathf.Atan2(starboard-port,10.8f)*Mathf.Rad2Deg+Mathf.Clamp((Hull.LeftWater-Hull.RightWater)/(BoatDimensions.HalfHoldArea*BoatDimensions.VolumeScale)*33.6f,-22f,22f);
        float height=(bow+stern+starboard+port)*0.25f-Hull.Volume/(BoatDimensions.DisplacementArea*BoatDimensions.AreaScale)-Hull.SinkDepth;
        verticalSpeed+=((height-p.y)*10f-verticalSpeed*5f)*dt;
        Vector3 next=Boat.position+forward*speed*dt/(1f+depth)*(Hull.Sunk?0f:1f);
        if(NavalWorldMap.Active!=null&&!NavalWorldMap.Active.CanSail(next,flat,Boat)){next=Boat.position;speed=0f;}
        next+=Vector3.up*verticalSpeed*dt;
        Quaternion nextRotation=Quaternion.Slerp(Boat.rotation,Quaternion.Euler(pitch,heading,roll),1f-Mathf.Exp(-4f*dt));
        ram.Velocity=(next-Boat.position)/Mathf.Max(dt,0.001f);ram.AngularRate=Mathf.DeltaAngle(Boat.eulerAngles.y,nextRotation.eulerAngles.y)/Mathf.Max(dt,0.001f);
        if(NavalShipContact.Constrain(Boat.position,Boat.rotation,ref next,ref nextRotation,target))
        {ram.Contact(target.GetComponent<NavalRamDamage>());speed=0f;heading=nextRotation.eulerAngles.y;}
        Boat.position=next;Boat.rotation=nextRotation;
        if(rider)
        {
            CharacterController body=player.GetComponentInChildren<CharacterController>();
            bool enabled=body!=null&&body.enabled;if(enabled)body.enabled=false;
            player.transform.position=Boat.TransformPoint(riderLocal);
            if(enabled)body.enabled=true;
        }
        for(int i=0;i<water.Length;i++)
        {
            float d=Hull.Flood.Depth(i);water[i].gameObject.SetActive(d>0.0005f);
            water[i].localScale=new Vector3(Hull.Flood.CellWidth,Mathf.Max(0.0005f,d),Hull.Flood.CellLength);
            water[i].localPosition=new Vector3(Hull.Flood.X(i),BoatDimensions.HoldFloor+d*0.5f,Hull.Flood.Z(i));
        }
        if(CanHelm()&&!Hull.Sunk&&stations.IsFunctional(0))
        {
            Character sailor=crew[0];sailor.DisableAI();
            CharacterController controller=sailor.GetComponent<CharacterController>();
            bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
            sailor.transform.position=stations.CrewPosition(0)+Vector3.up*footOffset*0.85f;
            Vector3 facing=stations.CrewForward(0);facing.y=0f;
            sailor.transform.rotation=Quaternion.LookRotation(facing);
            if(enabled)controller.enabled=true;
            FirstPersonMover mover=sailor as FirstPersonMover;
            if(mover!=null&&Velocity!=null)Velocity.SetValue(mover,Vector3.zero);
        }
        Physics.SyncTransforms();
        crewRoles.Tick(dt,target,NavalTestWorld.Active!=null?NavalTestWorld.Active.ShipVelocity:Vector3.zero,false,footOffset*0.85f);
        stations.UpdateRecoil();
    }
    private void OnGUI()
    {
        if(Boat==null)return;
        string status=Hull.Sunk?"SINKING":"Crew: helm "+(Alive(0)?"alive":"down")+", port "+(Alive(1)?"alive":"down")+", starboard "+(Alive(2)?"alive":"down");
        GUI.Box(new Rect(15f,235f,720f,45f),"ENEMY SHIP (red sail) | F12: remove / respawn\n"+status+" | Water: "+Hull.Volume.ToString("F1")+" m3");
        Camera camera=player!=null?player.GetPlayerCamera():null;
        if(camera!=null)for(int i=0;i<3;i++)if(Alive(i))
        {
            Vector3 screen=camera.WorldToScreenPoint(crew[i].transform.position+Vector3.up*2.2f);
            if(screen.z>0f)GUI.Label(new Rect(screen.x-85f,Screen.height-screen.y-20f,190f,25f),i==0?"Enemy helmsman":crewRoles.Role(i-1));
        }
    }
    private void OnDestroy()
    {
        if(ocean!=null&&ocean.AdditionalHull==Boat)ocean.AdditionalHull=null;
        foreach(Character sailor in crew)if(sailor!=null)Destroy(sailor.gameObject);
        foreach(Material material in materials)if(material!=null)Destroy(material);
    }
}

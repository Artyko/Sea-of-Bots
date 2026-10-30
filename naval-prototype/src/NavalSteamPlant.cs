using System.Collections.Generic;
using UnityEngine;

public sealed class NavalSteamPlant : MonoBehaviour
{
    private NavalHull hull;
    private readonly NavalSteamState state=new NavalSteamState();
    private readonly List<NavalHullVoxel> boiler=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> engine=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> funnel=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> propulsionMain=new List<NavalHullVoxel>(),pumpMain=new List<NavalHullVoxel>(),auxiliaryMain=new List<NavalHullVoxel>();
    private NavalRepairableMechanism boilerRepair,engineRepair,funnelRepair,propulsionMainRepair,pumpMainRepair,auxiliaryMainRepair;
    private NavalSmokeBatch smoke;
    private Material smokeMaterial,steamMaterial;
    private Transform propeller;
    private float smokeClock,steamClock,scaldClock,setting,boilerFlooding,rupturedAt,pumpAssist=1f,pumpDemandUntil;
    private bool liveLeakLogged,pumpSteamLogged;
    private readonly Dictionary<Character,float> steamExposure=new Dictionary<Character,float>();
    private readonly List<Character> steamExposureDead=new List<Character>();
    public float Coal {get{return state.Coal;}}
    public float Pressure {get{return state.Pressure;}}
    public float BoilerIntegrity {get{return Integrity(boiler);}}
    public float EngineIntegrity {get{return Integrity(engine);}}
    public float FunnelIntegrity {get{return Integrity(funnel);}}
    public float PropulsionMainIntegrity {get{return Integrity(propulsionMain);}}
    public float PumpMainIntegrity {get{return Integrity(pumpMain);}}
    public float AuxiliaryMainIntegrity {get{return Integrity(auxiliaryMain);}}
    public float SteamLeakSeverity {get{return 1f-(PropulsionMainIntegrity+PumpMainIntegrity+AuxiliaryMainIntegrity)/3f;}}
    public bool HasLiveSteamLeak {get{return Pressure>.08f&&SteamLeakSeverity>.01f;}}
    public float PowerFactor {get{return state.Power(EngineIntegrity)*NavalSteamMainMath.ConsumerIntegrity(PropulsionMainIntegrity);}}
    public float AuxiliaryFactor {get{return NavalSteamMainMath.AuxiliaryReloadFactor(AuxiliaryMainIntegrity);}}
    public float PumpAssist {get{return Time.time<=pumpDemandUntil?pumpAssist:1f;}}
    public float Setting {get{return setting;}}
    public float BoilerFlooding {get{return boilerFlooding;}}
    public bool BoilerRuptured {get{return state.Ruptured;}}
    public bool NeedsEngineer(float command){return state.NeedsEngineer(command,FunnelIntegrity);}
    // High steam is a continuous watch: the sailor remains assigned instead
    // of leaving as soon as pressure is reached and causing duty oscillation.
    public bool EngineerSettled(float command){return !NeedsEngineer(command);}

    public void Initialize(NavalHull owner,Material iron,Material darkIron,Material accent)
    {
        hull=owner;smokeMaterial=darkIron;
        Shader steamShader=Shader.Find("Unlit/Color");if(steamShader==null)steamShader=Shader.Find("Standard");
        steamMaterial=new Material(steamShader);steamMaterial.color=new Color(.78f,.86f,.90f,.88f);
        // Plant is fitted in the port machinery spaces behind the bulkhead.
        // The clear central aisle is deliberately wider than a robot capsule.
        Vector3 boilerCentre=new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.32f,NavalSteamLayout.BoilerZ);
        Vector3 engineCentre=new Vector3(NavalSteamLayout.EngineX,hull.Floor+.30f,NavalSteamLayout.EngineZ);
        AddGroup(boiler,boilerCentre,new Vector3(.62f,.55f,1.30f),iron);
        AddGroup(engine,engineCentre,new Vector3(.65f,.50f,1.20f),darkIron);
        for(int band=-1;band<=1;band++)AddDetail(boilerCentre+new Vector3(0,0,band*.43f),new Vector3(.68f,.60f,.07f),darkIron);
        AddDetail(boilerCentre+new Vector3(0,.36f,0),new Vector3(.28f,.18f,.30f),iron);
        AddDetail(new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.62f,.45f),new Vector3(.13f,.13f,.80f),accent);
        // A guarded flywheel and cylinder covers distinguish the engine from a
        // generic crate; these details are non-colliding and cannot block crew.
        for(int spoke=0;spoke<8;spoke++)
        {
            float angle=spoke*Mathf.PI/4f;
            AddDetail(engineCentre+new Vector3(.36f,Mathf.Sin(angle)*.23f,Mathf.Cos(angle)*.23f),new Vector3(.07f,.09f,.09f),accent);
        }
        AddDetail(engineCentre+new Vector3(.36f,0,0),new Vector3(.08f,.13f,.56f),iron);
        // A tall capped stack and contrasting band read as an early marine
        // funnel instead of the old single black rectangular block.
        const float funnelHeight=1.85f;
        float uptakeBottom=boilerCentre.y+.30f,uptakeTop=hull.Definition.WeatherDeck;
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,(uptakeBottom+uptakeTop)*.5f,NavalSteamLayout.FunnelZ),
            new Vector3(.30f,uptakeTop-uptakeBottom,.30f),darkIron);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+funnelHeight*.5f,NavalSteamLayout.FunnelZ),new Vector3(.40f,funnelHeight,.40f),darkIron);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+1.18f,NavalSteamLayout.FunnelZ),new Vector3(.48f,.16f,.48f),accent);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+funnelHeight+.04f,NavalSteamLayout.FunnelZ),new Vector3(.52f,.18f,.52f),darkIron);
        // Three narrow, non-blocking live-steam mains. They are trigger-based
        // visual/equipment cells: shells and repairs can address them, while
        // their route along the machinery-space walls never closes a walkway.
        AddMain(propulsionMain,new Vector3(-1.28f,hull.Floor+.57f,0f),new Vector3(.12f,.12f,2.55f),accent);
        AddMain(pumpMain,new Vector3(-1.28f,hull.Floor+.43f,-3.05f),new Vector3(.11f,.11f,3.35f),accent);
        AddMain(auxiliaryMain,new Vector3(1.28f,hull.Floor+.57f,2.25f),new Vector3(.11f,.11f,3.45f),accent);
        // Unicode escapes keep these labels stable when the project is built
        // by Windows PowerShell under a legacy console code page.
        boilerRepair=new NavalRepairableMechanism(hull,boiler,false,false,"\u041A\u043E\u0442\u0451\u043B");
        engineRepair=new NavalRepairableMechanism(hull,engine,false,false,"\u041C\u0430\u0448\u0438\u043D\u0430");
        funnelRepair=new NavalRepairableMechanism(hull,funnel,false,false,"\u0414\u044B\u043C\u043E\u0432\u0430\u044F \u0442\u0440\u0443\u0431\u0430");
        propulsionMainRepair=new NavalRepairableMechanism(hull,propulsionMain,false,false,"\u0413\u043B\u0430\u0432\u043D\u0430\u044F \u043F\u0430\u0440\u043E\u0432\u0430\u044F \u043C\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043B\u044C",true);
        pumpMainRepair=new NavalRepairableMechanism(hull,pumpMain,false,false,"\u041F\u0430\u0440\u043E\u043F\u0440\u043E\u0432\u043E\u0434 \u043F\u043E\u043C\u043F",true);
        auxiliaryMainRepair=new NavalRepairableMechanism(hull,auxiliaryMain,false,false,"\u0412\u0441\u043F\u043E\u043C\u043E\u0433\u0430\u0442\u0435\u043B\u044C\u043D\u0430\u044F \u043F\u0430\u0440\u043E\u0432\u0430\u044F \u043C\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043B\u044C",true);
        BuildPropeller(iron,accent);
        smoke=gameObject.GetComponent<NavalSmokeBatch>();if(smoke==null)smoke=gameObject.AddComponent<NavalSmokeBatch>();
    }
    private void AddGroup(List<NavalHullVoxel> target,Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,true);
        for(int i=first;i<hull.Voxels.Count;i++){NavalHullVoxel cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;target.Add(cell);}
    }
    private void AddDetail(Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,false);
        for(int i=first;i<hull.Voxels.Count;i++){hull.Voxels[i].Metallic=true;hull.Voxels[i].Flammable=false;}
    }
    private void AddMain(List<NavalHullVoxel> target,Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,false);
        for(int i=first;i<hull.Voxels.Count;i++){NavalHullVoxel cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;target.Add(cell);}
    }
    private GameObject PropellerPart(string name,Transform parent,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
    {
        GameObject part=GameObject.CreatePrimitive(PrimitiveType.Cube);part.name=name;part.transform.SetParent(parent,false);
        part.transform.localPosition=position;part.transform.localRotation=rotation;part.transform.localScale=scale;
        part.GetComponent<Renderer>().sharedMaterial=material;Collider collider=part.GetComponent<Collider>();if(collider!=null)Destroy(collider);
        return part;
    }
    private void BuildPropeller(Material iron,Material accent)
    {
        // The old propeller was two full black bars crossing the stern and
        // visually piercing the lower hull.  Four short offset blades now
        // begin at the hub and extend outward only, like a real screw.
        propeller=new GameObject("NavalPrototype_Propeller").transform;propeller.SetParent(transform,false);propeller.localPosition=new Vector3(0,-.72f,-6.24f);
        PropellerPart("Ironclad propeller hub",propeller,Vector3.zero,new Vector3(.30f,.30f,.26f),Quaternion.identity,accent);
        for(int blade=0;blade<4;blade++)
        {
            float angle=45f+blade*90f,rad=angle*Mathf.Deg2Rad;
            Vector3 radial=new Vector3(Mathf.Cos(rad),Mathf.Sin(rad),0);
            PropellerPart("Ironclad propeller blade "+blade,propeller,radial*.39f,new Vector3(.22f,.54f,.11f),
                Quaternion.Euler(18f,0,angle-90f),accent);
        }
        // A short dark shaft ends at the stern aperture; it never runs through
        // the visible hull as an exterior beam.
        PropellerPart("Ironclad propeller shaft",propeller,new Vector3(0,0,.22f),new Vector3(.16f,.16f,.42f),Quaternion.identity,iron);
    }
    private static float Integrity(List<NavalHullVoxel> cells)
    {
        if(cells.Count==0)return 0f;int intact=0;
        foreach(NavalHullVoxel cell in cells)if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
        return intact/(float)cells.Count;
    }
    public float Step(float dt,float command,bool engineer,Vector3 shipVelocity)
    {
        setting=Mathf.Clamp(command,-1f,1f);
        float waterDepth=hull.WaterDepthAt(new Vector3(NavalSteamLayout.BoilerX,hull.Floor,NavalSteamLayout.BoilerZ))*hull.WorldScale;
        boilerFlooding=NavalBoilerFloodMath.Flooding(waterDepth);
        if(state.Ruptured&&Time.time>rupturedAt+3f&&BoilerIntegrity>=.95f)state.RepairRupture();
        if(state.StepFlooded(dt,setting,engineer,BoilerIntegrity,FunnelIntegrity,boilerFlooding))RuptureBoiler(shipVelocity);
        state.ConsumePressure(NavalSteamMainMath.LeakDraw(dt,state.Pressure,SteamLeakSeverity));
        bool liveLeak=HasLiveSteamLeak;
        if(liveLeak!=liveLeakLogged)
        {
            liveLeakLogged=liveLeak;
            Debug.Log("[NavalPrototype] Steam-main leak: ship="+hull.Root.name+" live="+liveLeak+" severity="+(SteamLeakSeverity*100f).ToString("F0")+" pressure="+(Pressure*100f).ToString("F0"));
        }
        if(propeller!=null)propeller.Rotate(0,0,-setting*PowerFactor*720f*dt,Space.Self);
        TickSteamLeaks(dt,shipVelocity);
        EmitSmoke(dt,shipVelocity);
        return state.OutputThrottle(setting);
    }
    public float RequestPumpAssist(float dt)
    {
        pumpAssist=NavalSteamMainMath.PumpMultiplier(state.Pressure,PumpMainIntegrity);
        state.ConsumePressure(NavalSteamMainMath.PumpDraw(dt,pumpAssist));pumpDemandUntil=Time.time+.35f;
        bool steamMode=pumpAssist>1.05f;
        if(steamMode!=pumpSteamLogged)
        {
            pumpSteamLogged=steamMode;
            Debug.Log("[NavalPrototype] Bilge pump mode: ship="+hull.Root.name+" mode="+(steamMode?"steam-assisted":"hand")+" output="+pumpAssist.ToString("F2")+"x pressure="+(Pressure*100f).ToString("F0"));
        }
        return pumpAssist;
    }
    private void TickSteamLeaks(float dt,Vector3 shipVelocity)
    {
        if(!HasLiveSteamLeak){steamExposure.Clear();return;}
        steamClock-=dt;if(steamClock<=0f)
        {
            steamClock=Mathf.Lerp(.34f,.10f,SteamLeakSeverity);
            EmitMainLeaks(propulsionMain,shipVelocity);EmitMainLeaks(pumpMain,shipVelocity);EmitMainLeaks(auxiliaryMain,shipVelocity);
        }
        scaldClock+=dt;if(scaldClock<.25f)return;float elapsed=scaldClock;scaldClock=0f;
        steamExposureDead.Clear();
        foreach(Character robot in NavalCharacters.Snapshot())
        {
            if(robot==null||!robot.IsAlive())continue;bool exposed=NearMissingMain(robot.transform.position,propulsionMain)||NearMissingMain(robot.transform.position,pumpMain)||NearMissingMain(robot.transform.position,auxiliaryMain);
            float seconds;steamExposure.TryGetValue(robot,out seconds);seconds=exposed?seconds+elapsed:Mathf.Max(0f,seconds-elapsed*2f);
            if(seconds>=4f){robot.Kill(null,DamageSourceType.ScriptedKill,-1);steamExposureDead.Add(robot);Debug.Log("[NavalPrototype] Live steam incapacitated crew: ship="+hull.Root.name);}
            else if(seconds>0f)steamExposure[robot]=seconds;else steamExposure.Remove(robot);
        }
        foreach(Character robot in steamExposureDead)steamExposure.Remove(robot);
    }
    private bool NearMissingMain(Vector3 point,List<NavalHullVoxel> cells)
    {foreach(var cell in cells)if(cell!=null&&(cell.Missing||cell.Detached)&&(cell.transform.position-point).sqrMagnitude<1.25f*1.25f)return true;return false;}
    private void EmitMainLeaks(List<NavalHullVoxel> cells,Vector3 shipVelocity)
    {
        if(smoke==null||steamMaterial==null)return;
        foreach(var cell in cells)if(cell!=null&&(cell.Missing||cell.Detached))
        {
            Vector3 sideways=hull.Root.right*Mathf.Sign(cell.LocalCenter.x==0f?1f:cell.LocalCenter.x);
            smoke.EmitPlume(cell.transform.position,shipVelocity*.08f+sideways*1.5f+Vector3.up*2.4f,steamMaterial,.11f,2.6f,.55f,1.45f);break;
        }
    }
    private void RuptureBoiler(Vector3 shipVelocity)
    {
        rupturedAt=Time.time;Vector3 centre=new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.32f,NavalSteamLayout.BoilerZ);
        // Steam rupture is local machinery damage, not a magazine detonation,
        // and therefore cannot spontaneously ignite remote timber.
        NavalHullVoxel direct=null;foreach(var cell in boiler)if(cell!=null&&!cell.Missing&&!cell.Detached){direct=cell;break;}
        hull.Damage(centre,.45f,true,direct,.85f);
        Vector3 world=hull.Root.TransformPoint(centre);
        if(smoke!=null&&smokeMaterial!=null)for(int i=0;i<14;i++)smoke.EmitPlume(world+Random.insideUnitSphere*.18f,
            shipVelocity*.12f+Vector3.up*Random.Range(2.5f,5.0f)+Random.insideUnitSphere*.8f,smokeMaterial,Random.Range(.16f,.28f),Random.Range(2.2f,3.8f),.8f,3.6f);
        Debug.Log("[NavalPrototype] Boiler steam rupture: ship="+hull.Root.name+" flooding="+(boilerFlooding*100f).ToString("F0")+" pressure vessel severely damaged");
    }
    private void EmitSmoke(float dt,Vector3 shipVelocity)
    {
        if(smoke==null||smokeMaterial==null||!NavalSteamSmokeMath.Emits(state.LastBurnRate,boilerFlooding,FunnelIntegrity))return;
        Vector3 top;if(!TrySmokeOutlet(out top))return;
        float density=NavalSteamSmokeMath.Density(state.LastBurnRate,state.Pressure);
        smokeClock-=dt;if(smokeClock>0)return;smokeClock=Mathf.Lerp(.70f,.14f,density);
        // A marine funnel produces a coherent vertical column at the mouth.
        // The old generic plume expanded four-fold and looked like a broad cloud.
        smoke.EmitPlume(top,shipVelocity*.12f+Vector3.up*Mathf.Lerp(1.8f,3.0f,density),smokeMaterial,
            Mathf.Lerp(.16f,.29f,density),Mathf.Lerp(4.8f,6.6f,density),1.15f,3.15f);
    }
    private bool TrySmokeOutlet(out Vector3 point)
    {
        NavalHullVoxel highest=null;float highestY=float.NegativeInfinity;
        foreach(NavalHullVoxel cell in funnel)
            if(cell!=null&&NavalSteamSmokeMath.IsExternalOutlet(cell.Missing,cell.Detached,cell.LocalCenter.y,hull.Definition.WeatherDeck)&&cell.LocalCenter.y>highestY)
            {highest=cell;highestY=cell.LocalCenter.y;}
        if(highest==null){point=Vector3.zero;return false;}
        // Follow the actual surviving top cell.  If the cap or stack falls,
        // smoke moves to the remaining stump; with no exterior stack it stops.
        point=highest.transform.position+hull.Root.up*Mathf.Abs(highest.transform.lossyScale.y)*.5f;
        return true;
    }
    public void Restore(){state.Reset();setting=smokeClock=steamClock=scaldClock=boilerFlooding=rupturedAt=0f;pumpAssist=1f;pumpDemandUntil=0f;liveLeakLogged=pumpSteamLogged=false;steamExposure.Clear();}
    public string Diagnostics
    {get{return "telegraph="+(setting*100f).ToString("F0")+" pressure="+(Pressure*100f).ToString("F0")+" coal="+Coal.ToString("F1")+" boiler="+(BoilerIntegrity*100f).ToString("F0")+" engine="+(EngineIntegrity*100f).ToString("F0")+" funnel="+(FunnelIntegrity*100f).ToString("F0")+" mains="+(PropulsionMainIntegrity*100f).ToString("F0")+"/"+(PumpMainIntegrity*100f).ToString("F0")+"/"+(AuxiliaryMainIntegrity*100f).ToString("F0")+" pump="+PumpAssist.ToString("F2")+"x flood="+(boilerFlooding*100f).ToString("F0")+" burn="+(state.Combustion*100f).ToString("F0")+" ruptured="+state.Ruptured;}}
    private void OnDestroy(){if(steamMaterial!=null)Destroy(steamMaterial);}
}

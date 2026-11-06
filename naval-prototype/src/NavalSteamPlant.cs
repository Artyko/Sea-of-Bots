using System.Collections.Generic;
using UnityEngine;

public sealed partial class NavalSteamPlant : MonoBehaviour
{
    private NavalHull hull;
    private readonly NavalSteamState state=new NavalSteamState(),secondState=new NavalSteamState();
    private readonly List<NavalHullVoxel> firstBoilerCells=new List<NavalHullVoxel>(),secondBoilerCells=new List<NavalHullVoxel>();
    private bool Twin {get{return hull!=null&&hull.Definition.CapitalShip;}}
    private float secondFlood,secondRupturedAt;
    private readonly NavalEarlySteamRetreat earlyRetreat=new NavalEarlySteamRetreat();
    public bool PropulsionFloodEmergency {get{return earlyRetreat.Active||boilerFlooding>.2f||Twin&&secondFlood>.2f;}}
    public bool EmergencyEconomy {get{return (boilerFlooding>(oilStates!=null?.20f:.05f)||Twin&&secondFlood>.05f||Pressure<.4f&&earlyRetreat.Active)&&Pressure>.05f;}}

    private readonly List<NavalHullVoxel> boiler=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> engine=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> funnel=new List<NavalHullVoxel>();
    private readonly List<NavalHullVoxel> propulsionMain=new List<NavalHullVoxel>(),pumpMain=new List<NavalHullVoxel>(),auxiliaryMain=new List<NavalHullVoxel>();
    private NavalRepairableMechanism boilerRepair,engineRepair,funnelRepair,propulsionMainRepair,pumpMainRepair,auxiliaryMainRepair;
    private NavalSmokeBatch smoke,steamClouds;
    private Material smokeMaterial,steamMaterial;
    private Transform propeller;
    private readonly List<NavalHullVoxel> shaftCells=new List<NavalHullVoxel>(),bearingCells=new List<NavalHullVoxel>(),screwCells=new List<NavalHullVoxel>();
    private NavalHullVoxel screwHub;
    private float DriveContinuity {get{foreach(var cell in shaftCells)if(cell==null||cell.Missing||cell.Detached)return 0;return screwHub==null||screwHub.Missing||screwHub.Detached?0:Integrity(screwCells)*Integrity(bearingCells);}}

    private float smokeClock,steamClock,scaldClock,setting,boilerFlooding,rupturedAt,pumpAssist=1f,pumpDemandUntil;
    private bool liveLeakLogged,pumpSteamLogged;
    private readonly Dictionary<Character,float> steamExposure=new Dictionary<Character,float>();
    private readonly List<Character> steamExposureDead=new List<Character>();
    public float Coal {get{return oilStates!=null?OilMean(false):Twin?(state.Coal+secondState.Coal)*.5f:state.Coal;}}
    public float Pressure {get{return oilStates!=null?OilMean(true):Twin?(state.Pressure+secondState.Pressure)*.5f:state.Pressure;}}
    public float BoilerIntegrity {get{return Integrity(boiler);}}
    public float EngineIntegrity {get{return Integrity(engine);}}
    public float FunnelIntegrity {get{return Integrity(funnel);}}
    public float PropulsionMainIntegrity {get{return MainSupply(propulsionMain);}}
    public float PumpMainIntegrity {get{return MainSupply(pumpMain);}}
    public float AuxiliaryMainIntegrity {get{return MainSupply(auxiliaryMain);}}
    public float SteamLeakSeverity {get{return oilStates!=null?isolation.Leak(Integrity(propulsionMain),Integrity(pumpMain),Integrity(auxiliaryMain)):1f-(PropulsionMainIntegrity+PumpMainIntegrity+AuxiliaryMainIntegrity)/3f;}}
    public bool HasLiveSteamLeak {get{return oilStates!=null?((MainSteam(propulsionMain)>.08f&&Integrity(propulsionMain)<.99f)||(MainSteam(pumpMain)>.08f&&Integrity(pumpMain)<.99f)||(MainSteam(auxiliaryMain)>.08f&&Integrity(auxiliaryMain)<.99f)):Pressure>.08f&&SteamLeakSeverity>.01f;}}
    public float PowerFactor {get{return oilStates!=null?OilPower:(Twin?(state.Power(EngineIntegrity)+secondState.Power(EngineIntegrity))*.5f:state.Power(EngineIntegrity))*NavalSteamMainMath.ConsumerIntegrity(PropulsionMainIntegrity)*DriveContinuity;}}
    public float AuxiliaryFactor {get{return NavalSteamMainMath.AuxiliaryReloadFactor(AuxiliaryMainIntegrity);}}
    public float PumpAssist {get{return Time.time<=pumpDemandUntil?pumpAssist:1f;}}
    public float Setting {get{return setting;}}
    public float BoilerFlooding {get{return boilerFlooding;}}
    public bool BoilerRuptured {get{if(oilStates!=null){foreach(var v in oilStates)if(v.Ruptured)return true;return false;}return state.Ruptured||Twin&&secondState.Ruptured;}}
    public string BoilerSummary {get{return oilStates!=null?(BoilerIntegrity*100f).ToString("F0")+"% / 6":Twin?(Integrity(firstBoilerCells)*100f).ToString("F0")+"/"+(Integrity(secondBoilerCells)*100f).ToString("F0")+"%":(BoilerIntegrity*100f).ToString("F0")+"%";}}
    public bool NeedsEngineer(float command){if(oilStates!=null){foreach(var v in oilStates)if(v.NeedsEngineer(command,FunnelIntegrity))return true;return false;}return state.NeedsEngineer(command,FunnelIntegrity)||Twin&&secondState.NeedsEngineer(command,FunnelIntegrity);}
    // High steam is a continuous watch: the sailor remains assigned instead
    // of leaving as soon as pressure is reached and causing duty oscillation.
    public bool EngineerSettled(float command){return !NeedsEngineer(command);}

    public void Initialize(NavalHull owner,Material iron,Material darkIron,Material accent)
    {
        hull=owner;owner.SteamMachinery=this;
        if(owner.Definition.Destroyer){BuildOilPlant(iron,darkIron,accent);return;}
        Shader exhaustShader=Shader.Find("Unlit/Color");if(exhaustShader==null)exhaustShader=Shader.Find("Standard");
        smokeMaterial=new Material(exhaustShader);smokeMaterial.color=new Color(.20f,.22f,.24f,1f);
        Shader steamShader=Shader.Find("Unlit/Color");if(steamShader==null)steamShader=Shader.Find("Standard");
        steamMaterial=new Material(steamShader);steamMaterial.color=new Color(1f,1f,1f,.88f);
        // Plant is fitted in the port machinery spaces behind the bulkhead.
        // The clear central aisle is deliberately wider than a robot capsule.
        Vector3 boilerCentre=new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.32f,NavalSteamLayout.BoilerZFor(hull.Definition));
        Vector3 engineCentre=new Vector3(NavalSteamLayout.EngineX,hull.Floor+.30f,NavalSteamLayout.EngineZ);
        hull.BoilerProtection=new NavalBoilerProtection(hull,iron);
        // Octagonal boiler jacket, curved in voxel steps rather than a plain crate.
        if(Twin)for(int unit=-1;unit<=1;unit+=2){
            Vector3 centre=boilerCentre+new Vector3(0,0,unit*.40f);
            AddGroup(boiler,centre,new Vector3(.62f,.31f,.52f),iron);
            for(int cap=-1;cap<=1;cap+=2)AddGroup(boiler,centre+Vector3.up*(cap*.215f),new Vector3(.43f,.12f,.52f),iron);
        }else AddGroup(boiler,boilerCentre,new Vector3(.62f,.31f,1.30f),iron);
        if(!Twin)for(int cap=-1;cap<=1;cap+=2)
            AddGroup(boiler,boilerCentre+Vector3.up*(cap*.215f),new Vector3(.43f,.12f,1.30f),iron);
        AddGroup(boiler,boilerCentre+new Vector3(.325f,0,-.40f),new Vector3(.05f,.24f,.26f),darkIron);
        for(int foot=-1;foot<=1;foot+=2)
            AddGroup(boiler,boilerCentre+new Vector3(0,-.29f,foot*.44f),new Vector3(.68f,.06f,.16f),darkIron);
        // Three reciprocating cylinders over a common crankcase, with piston rods and valve chests.
        AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.12f,NavalSteamLayout.EngineZ),new Vector3(.65f,.20f,1.20f),darkIron);
        if(Twin){
            for(int stage=-1;stage<=1;stage++)AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.40f,NavalSteamLayout.EngineZ+stage*.38f),new Vector3(stage<0?.48f:.62f,.34f,.36f),iron);
            for(int end=-1;end<=1;end+=2)AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.40f,NavalSteamLayout.EngineZ+end*.60f),new Vector3(.38f,.25f,.08f),accent);
        }else for(int cylinder=-1;cylinder<=1;cylinder++){
            float z=NavalSteamLayout.EngineZ+cylinder*.38f;
            AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.43f,z),new Vector3(.38f,.32f,.28f),iron);
            AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.29f,z),new Vector3(.08f,.16f,.08f),accent);
            AddGroup(engine,new Vector3(NavalSteamLayout.EngineX+.25f,hull.Floor+.43f,z),new Vector3(.12f,.32f,.22f),darkIron);
        }
        AddGroup(engine,new Vector3(NavalSteamLayout.EngineX,hull.Floor+.025f,NavalSteamLayout.EngineZ),new Vector3(.65f,.05f,.9f),iron);
        for(int band=-1;band<=1;band++)AddDetail(boilerCentre+new Vector3(0,0,band*.43f),new Vector3(.68f,.60f,.07f),darkIron);
        AddDetail(boilerCentre+new Vector3(0,.36f,0),new Vector3(.28f,.18f,.30f),iron);
        AddDetail(new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.62f,.45f),new Vector3(.13f,.13f,.80f),accent);
        // A guarded flywheel and cylinder covers distinguish the engine from a
        // generic crate; their solid details remain inside the machinery envelope.
        for(int spoke=0;spoke<8;spoke++)
        {
            float angle=spoke*Mathf.PI/4f;
            AddDetail(engineCentre+new Vector3(.36f,Mathf.Sin(angle)*.23f,Mathf.Cos(angle)*.23f),new Vector3(.07f,.09f,.09f),accent);
        }
        AddDetail(engineCentre+new Vector3(.36f,0,0),new Vector3(.08f,.13f,.56f),iron);
        // A tall capped stack and contrasting band read as an early marine
        // funnel instead of the old single black rectangular block.
        if(hull.Definition.CapitalShip){
            for(int stack=0;stack<2;stack++){
                float z=stack==0?.4f:-2f,bottom=boilerCentre.y+.30f,top=hull.Definition.WeatherDeck;
                AddGroup(funnel,new Vector3(-.95f,(bottom+top)*.5f,z),new Vector3(.22f,top-bottom,.22f),darkIron);
                AddGroup(funnel,new Vector3(-.95f,top+.68f,z),new Vector3(.38f,1.36f,.38f),darkIron);
                AddGroup(funnel,new Vector3(-.95f,top+1.38f,z),new Vector3(.44f,.08f,.44f),accent);
                AddGroup(funnel,new Vector3(-1.2f,top-.30f,z),new Vector3(.50f,.20f,.22f),darkIron);
            }
            // No longitudinal collector between the two funnels: keep the middle-deck passage open.
        }else{
        const float funnelHeight=1.85f;
        float uptakeBottom=boilerCentre.y+.30f,uptakeTop=hull.Definition.WeatherDeck;
        AddGroup(funnel,new Vector3(NavalSteamLayout.BoilerX,(uptakeBottom+uptakeTop)*.5f,NavalSteamLayout.FunnelZ),
            new Vector3(.20f,uptakeTop-uptakeBottom,.20f),darkIron);
        AddGroup(funnel,new Vector3((NavalSteamLayout.BoilerX+NavalSteamLayout.FunnelX)*.5f,uptakeTop-.30f,NavalSteamLayout.FunnelZ),
            new Vector3(Mathf.Abs(NavalSteamLayout.BoilerX-NavalSteamLayout.FunnelX),.20f,.20f),darkIron);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+funnelHeight*.5f,NavalSteamLayout.FunnelZ),new Vector3(.40f,funnelHeight,.40f),darkIron);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+1.18f,NavalSteamLayout.FunnelZ),new Vector3(.48f,.16f,.48f),accent);
        AddGroup(funnel,new Vector3(NavalSteamLayout.FunnelX,hull.Definition.WeatherDeck+funnelHeight+.04f,NavalSteamLayout.FunnelZ),new Vector3(.52f,.18f,.52f),darkIron);
        }
        // Three solid live-steam mains, mounted above wing-room equipment. Their
        // equipment cells accept shell hits and repairs, while
        // their route along the machinery-space walls never closes a walkway.
        AddMain(propulsionMain,new Vector3(-1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),0f),new Vector3(.12f,.12f,2.55f),accent);
        AddMain(pumpMain,new Vector3(-1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),-3.05f),new Vector3(.11f,.11f,3.35f),accent);
        AddMain(auxiliaryMain,new Vector3(1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),2.25f),new Vector3(.11f,.11f,3.45f),accent);
        // Flanged joints and valve bonnets belong to the destructible steam mains.
        for(int joint=-1;joint<=1;joint++){
            AddMain(propulsionMain,new Vector3(-1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),joint*.75f),new Vector3(.18f,.18f,.08f),iron);
            AddMain(pumpMain,new Vector3(-1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),-3.05f+joint*.95f),new Vector3(.17f,.17f,.08f),iron);
            AddMain(auxiliaryMain,new Vector3(1.78f,hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),2.25f+joint*.95f),new Vector3(.17f,.17f,.08f),iron);
        }
        // Continuous feed elbows connect all mains to the boiler casing.
        // Earlier disconnected pipe islands were deleted at spawn.
        float pipeY=NavalSteamLayout.MainHeight(hull.Definition),feedZ=hull.Definition.CapitalShip?.6f:1f;
        AddMain(propulsionMain,new Vector3(-1.78f,hull.Floor+(.3f+pipeY)*.5f,feedZ),new Vector3(.12f,pipeY-.3f,.12f),accent);
        AddMain(pumpMain,new Vector3(-1.78f,hull.Floor+pipeY,-1.325f),new Vector3(.12f,.12f,.24f),accent);
        AddMain(auxiliaryMain,new Vector3(0,hull.Floor+pipeY,feedZ),new Vector3(3.56f,.11f,.11f),accent);
        // Unicode escapes keep these labels stable when the project is built
        // by Windows PowerShell under a legacy console code page.
        BuildTransmissionLinks(iron,darkIron,accent);
        boilerRepair=new NavalRepairableMechanism(hull,boiler,false,false,"\u041A\u043E\u0442\u0451\u043B");
        engineRepair=new NavalRepairableMechanism(hull,engine,false,false,"\u041C\u0430\u0448\u0438\u043D\u0430");
        funnelRepair=new NavalRepairableMechanism(hull,funnel,false,false,"\u0414\u044B\u043C\u043E\u0432\u0430\u044F \u0442\u0440\u0443\u0431\u0430");
        propulsionMainRepair=new NavalRepairableMechanism(hull,propulsionMain,false,false,"\u0413\u043B\u0430\u0432\u043D\u0430\u044F \u043F\u0430\u0440\u043E\u0432\u0430\u044F \u043C\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043B\u044C",true);
        pumpMainRepair=new NavalRepairableMechanism(hull,pumpMain,false,false,"\u041F\u0430\u0440\u043E\u043F\u0440\u043E\u0432\u043E\u0434 \u043F\u043E\u043C\u043F",true);
        auxiliaryMainRepair=new NavalRepairableMechanism(hull,auxiliaryMain,false,false,"\u0412\u0441\u043F\u043E\u043C\u043E\u0433\u0430\u0442\u0435\u043B\u044C\u043D\u0430\u044F \u043F\u0430\u0440\u043E\u0432\u0430\u044F \u043C\u0430\u0433\u0438\u0441\u0442\u0440\u0430\u043B\u044C",true);
        BuildPropeller(iron,accent);
        new NavalRepairableMechanism(hull,shaftCells,false,false,"Валопровод",false,false,false);
        new NavalRepairableMechanism(hull,bearingCells,false,false,"Упорный подшипник");
        new NavalRepairableMechanism(hull,screwCells,false,false,"Гребной винт",false,false,false);
        smoke=gameObject.GetComponent<NavalSmokeBatch>();if(smoke==null)smoke=gameObject.AddComponent<NavalSmokeBatch>();
        var steamRoot=new GameObject("White live-steam effects");steamRoot.transform.SetParent(transform,false);steamClouds=steamRoot.AddComponent<NavalSmokeBatch>();
    }
    private void AddGroup(List<NavalHullVoxel> target,Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,true);
        for(int i=first;i<hull.Voxels.Count;i++){NavalHullVoxel cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;target.Add(cell);}
    }
    private void AddDetail(Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,true);
        for(int i=first;i<hull.Voxels.Count;i++){hull.Voxels[i].Metallic=true;hull.Voxels[i].Flammable=false;}
    }
    private void AddMain(List<NavalHullVoxel> target,Vector3 position,Vector3 size,Material material)
    {
        int first=hull.Voxels.Count;hull.AddDecorationBox(hull.Root,material,position,size,true);
        for(int i=first;i<hull.Voxels.Count;i++){NavalHullVoxel cell=hull.Voxels[i];cell.Metallic=true;cell.Flammable=false;target.Add(cell);}
    }
    private GameObject PropellerPart(string name,Transform parent,Vector3 position,Vector3 scale,Quaternion rotation,Material material)
    {
        var cell=hull.AddVoxel(parent,material,position,scale,false,true);
        cell.gameObject.name=name;cell.transform.localRotation=rotation;cell.Metallic=true;cell.Flammable=false;cell.MovableEquipment=true;
        screwCells.Add(cell);if(screwHub==null)screwHub=cell;
        return cell.gameObject;
    }
    private void Pipe(List<NavalHullVoxel> group,Vector3 a,Vector3 b,float width,Material material){
        Vector3 delta=b-a;int count=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.12f));
        for(int i=0;i<count;i++){
            Vector3 centre=Vector3.Lerp(a,b,(i+.5f)/count);
            var cell=hull.AddVoxel(hull.Root,material,centre,new Vector3(width,width,delta.magnitude/count+.012f),false,true);
            cell.transform.localRotation=Quaternion.LookRotation(delta);cell.Metallic=true;cell.Flammable=false;group.Add(cell);
        }
    }
    private void BuildTransmissionLinks(Material iron,Material dark,Material accent){
        float steamY=hull.Floor+NavalSteamLayout.MainHeight(hull.Definition),boilerZ=NavalSteamLayout.BoilerZFor(hull.Definition);
        // Live-steam delivery from each drum, then a downward feed into the engine cylinders.
        foreach(float dz in Twin?new[]{-.4f,.4f}:new[]{0f}){
            Vector3 drum=new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.60f,boilerZ+dz);
            Vector3 header=new Vector3(-1.78f,steamY,boilerZ+dz);
            Pipe(propulsionMain,drum,new Vector3(drum.x,steamY,drum.z),.09f,accent);
            Pipe(propulsionMain,new Vector3(drum.x,steamY,drum.z),header,.09f,accent);
            Vector3 smokeTop=new Vector3(NavalSteamLayout.BoilerX,hull.Definition.WeatherDeck-.30f,boilerZ+dz);
            Pipe(funnel,new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.55f,boilerZ+dz),smokeTop,.18f,dark);
        }
        Pipe(propulsionMain,new Vector3(-1.78f,steamY,-1.2f),new Vector3(NavalSteamLayout.EngineX,hull.Floor+.55f,-1.2f),.10f,accent);
        Pipe(pumpMain,new Vector3(-1.78f,steamY,-4.70f),new Vector3(-1.78f,steamY,-5.35f),.09f,accent);
        Pipe(pumpMain,new Vector3(-1.78f,steamY,-5.35f),new Vector3(0,steamY,-5.35f),.09f,accent);
        Pipe(pumpMain,new Vector3(0,steamY,-5.35f),new Vector3(0,hull.Floor+.35f,-5.35f),.09f,accent);
        // Keep horizontal exhaust collectors below the deck surface: their old top faces
        // were coplanar with the deck and flickered (z-fighting). Vertical trunks remain.
        // Uptake crosspieces connect both real funnel trunks to the boiler exhaust collector.
        if(Twin)foreach(float z in new[]{.4f,-2f})Pipe(funnel,new Vector3(NavalSteamLayout.BoilerX,hull.Definition.WeatherDeck-.30f,z),new Vector3(-.95f,hull.Definition.WeatherDeck-.30f,z),.18f,dark);
        Vector3 drive=new Vector3(NavalSteamLayout.EngineX,hull.Floor+.03f,NavalSteamLayout.EngineZ-.20f);
        Vector3 stern=new Vector3(0,NavalSteamLayout.ScrewY(hull.Definition),-6.24f);
        Pipe(shaftCells,drive,stern,.10f,iron);
        AddGroup(bearingCells,drive,new Vector3(.28f,.28f,.30f),dark);
    }
    private void BuildPropeller(Material iron,Material accent)
    {
        // The old propeller was two full black bars crossing the stern and
        // visually piercing the lower hull.  Four short offset blades now
        // begin at the hub and extend outward only, like a real screw.
        propeller=new GameObject("NavalPrototype_Propeller").transform;propeller.SetParent(transform,false);propeller.localPosition=new Vector3(0,NavalSteamLayout.ScrewY(hull.Definition),-6.24f);
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
    private struct IntegritySample
    {
        public int Count;
        public float Value;
    }
    private readonly Dictionary<List<NavalHullVoxel>,IntegritySample> integrityCache =
        new Dictionary<List<NavalHullVoxel>,IntegritySample>();
    private int integrityRevision = -1;

    private float Integrity(List<NavalHullVoxel> cells)
    {
        if(cells.Count==0)return 0f;
        if(integrityRevision!=hull.Revision)
        {
            integrityCache.Clear();
            integrityRevision=hull.Revision;
        }
        IntegritySample sample;
        if(integrityCache.TryGetValue(cells,out sample)&&sample.Count==cells.Count)return sample.Value;
        int intact=0;
        foreach(NavalHullVoxel cell in cells)if(cell!=null&&!cell.Missing&&!cell.Detached)intact++;
        sample=new IntegritySample{Count=cells.Count,Value=intact/(float)cells.Count};
        integrityCache[cells]=sample;
        return sample.Value;
    }
    public float Step(float dt,float command,bool engineer,Vector3 shipVelocity)
    {
        if(oilStates!=null)return StepOilPlant(dt,command,engineer,shipVelocity);
        setting=Mathf.Clamp(command,-1f,1f);
        float waterDepth=hull.WaterDepthAt(new Vector3(NavalSteamLayout.BoilerX,hull.Floor,NavalSteamLayout.BoilerZFor(hull.Definition)-(Twin?.35f:0f)))*hull.WorldScale;
        float localDepth=waterDepth,nearbyDepth=waterDepth;bool nearbyInflow=false;
        if(Twin)localDepth=Mathf.Max(localDepth,hull.WaterDepthAt(new Vector3(NavalSteamLayout.BoilerX,hull.Floor,NavalSteamLayout.BoilerZFor(hull.Definition)+.42f))*hull.WorldScale);
        for(int i=0;i<hull.Flood.Water.Length;i++)if(Mathf.Abs(hull.Flood.Z(i)-NavalSteamLayout.BoilerZFor(hull.Definition))<1.2f&&hull.Flood.X(i)<0)nearbyDepth=Mathf.Max(nearbyDepth,hull.Flood.Depth(i)*hull.WorldScale);
        foreach(var cell in hull.GetDamagedVoxels())if(cell.InflowRate>.0001f&&cell.LocalCenter.x<0&&Mathf.Abs(cell.LocalCenter.z-NavalSteamLayout.BoilerZFor(hull.Definition))<1.5f){nearbyInflow=true;break;}
        bool wasRetreat=earlyRetreat.Active;earlyRetreat.Step(dt,localDepth,nearbyDepth,nearbyInflow,Pressure,BoilerIntegrity);
        if(wasRetreat!=earlyRetreat.Active)Debug.Log("[NavalPrototype] Early steam retreat: ship="+hull.Root.name+" active="+earlyRetreat.Active+" localDepth="+localDepth.ToString("F2")+" nearbyDepth="+nearbyDepth.ToString("F2")+" inflow="+nearbyInflow+" pressure="+Pressure.ToString("F2"));
        // Raised firebox admits flooding only above its 0.65 m working plinth.
        boilerFlooding=NavalBoilerFloodMath.Flooding(Mathf.Max(0,waterDepth-.5f));
        if(Twin){
            secondFlood=NavalBoilerFloodMath.Flooding(Mathf.Max(0,hull.WaterDepthAt(new Vector3(NavalSteamLayout.BoilerX,hull.Floor,NavalSteamLayout.BoilerZFor(hull.Definition)+.42f))*hull.WorldScale-.5f));
            if(firstBoilerCells.Count+secondBoilerCells.Count==0)foreach(var cell in boiler){if(cell.LocalCenter.z<NavalSteamLayout.BoilerZFor(hull.Definition))firstBoilerCells.Add(cell);else secondBoilerCells.Add(cell);}
            if(secondState.Ruptured&&Time.time>secondRupturedAt+3f&&Integrity(secondBoilerCells)>=.95f)secondState.RepairRupture();
            if(secondState.StepFlooded(dt,setting,engineer,Integrity(secondBoilerCells),FunnelIntegrity,secondFlood,true)){
                secondRupturedAt=Time.time;Debug.Log("[NavalPrototype] Secondary boiler rupture: ship="+hull.Root.name);
            }
        }
        if(state.Ruptured&&Time.time>rupturedAt+3f&&BoilerIntegrity>=.95f)state.RepairRupture();
        if(state.StepFlooded(dt,setting,engineer,Twin?Integrity(firstBoilerCells):BoilerIntegrity,FunnelIntegrity,boilerFlooding,true))RuptureBoiler(shipVelocity);
        state.ConsumePressure(NavalSteamMainMath.LeakDraw(dt,state.Pressure,SteamLeakSeverity));
        if(Twin)secondState.ConsumePressure(NavalSteamMainMath.LeakDraw(dt,secondState.Pressure,SteamLeakSeverity));
        bool liveLeak=HasLiveSteamLeak;
        if(liveLeak!=liveLeakLogged)
        {
            liveLeakLogged=liveLeak;
            Debug.Log("[NavalPrototype] Steam-main leak: ship="+hull.Root.name+" live="+liveLeak+" severity="+(SteamLeakSeverity*100f).ToString("F0")+" pressure="+(Pressure*100f).ToString("F0"));
        }
        foreach(var cell in screwCells)hull.NoteCellMoved(cell);
        if(propeller!=null)propeller.Rotate(0,0,-setting*PowerFactor*720f*dt,Space.Self);
        TickSteamLeaks(dt,shipVelocity);
        EmitSmoke(dt,shipVelocity);
        return state.OutputThrottle(setting);
    }
    private void ConsumeSteam(float amount){if(oilStates!=null){foreach(var v in oilStates)v.ConsumePressure(amount/6);return;}if(Twin){state.ConsumePressure(amount*.5f);secondState.ConsumePressure(amount*.5f);}else state.ConsumePressure(amount);}
    public float RequestPumpAssist(float dt)
    {
        pumpAssist=NavalSteamMainMath.PumpMultiplier(Pressure,PumpMainIntegrity);
        if(EmergencyEconomy)pumpAssist=Mathf.Min(pumpAssist,1.15f);
        ConsumeSteam(NavalSteamMainMath.PumpDraw(dt,pumpAssist));pumpDemandUntil=Time.time+.35f;
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
            if(robot==null||!robot.IsAlive())continue;bool exposed=(MainSteam(propulsionMain)>.08f&&NearMissingMain(robot.transform.position,propulsionMain))||(MainSteam(pumpMain)>.08f&&NearMissingMain(robot.transform.position,pumpMain))||(MainSteam(auxiliaryMain)>.08f&&NearMissingMain(robot.transform.position,auxiliaryMain));
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
        if(steamClouds==null||steamMaterial==null||MainSteam(cells)<=.08f)return;
        foreach(var cell in cells)if(cell!=null&&(cell.Missing||cell.Detached))
        {
            Vector3 sideways=hull.Root.right*Mathf.Sign(cell.LocalCenter.x==0f?1f:cell.LocalCenter.x);
            Vector3 broken=cell.transform.position,origin=broken;float best=3.5f*3.5f;bool outlet=false;
            foreach(var edge in cells)if(edge!=null&&!edge.Missing&&!edge.Detached){float d=(edge.transform.position-broken).sqrMagnitude;if(d<best){best=d;origin=edge.transform.position;outlet=true;}}
            if(!outlet)continue;
            steamClouds.EmitPlume(origin,shipVelocity*.08f+sideways*1.5f+Vector3.up*2.4f,steamMaterial,.11f,2.6f,.55f,1.45f);break;
        }
    }
    private void RuptureBoiler(Vector3 shipVelocity)
    {
        rupturedAt=Time.time;Vector3 centre=new Vector3(NavalSteamLayout.BoilerX,hull.Floor+.32f,NavalSteamLayout.BoilerZFor(hull.Definition)-(Twin?.35f:0f));
        // Steam rupture is local machinery damage, not a magazine detonation,
        // and therefore cannot spontaneously ignite remote timber.
        NavalHullVoxel direct=null;foreach(var cell in boiler)if(cell!=null&&!cell.Missing&&!cell.Detached){direct=cell;break;}
        hull.Damage(centre,1.8f,true,direct,.85f);
        Vector3 world=hull.Root.TransformPoint(centre);
        if(smoke!=null&&smokeMaterial!=null)for(int i=0;i<14;i++)smoke.EmitPlume(world+Random.insideUnitSphere*.18f,
            shipVelocity*.12f+Vector3.up*Random.Range(2.5f,5.0f)+Random.insideUnitSphere*.8f,smokeMaterial,Random.Range(.16f,.28f),Random.Range(2.2f,3.8f),.8f,3.6f);
        Debug.Log("[NavalPrototype] Boiler steam rupture: ship="+hull.Root.name+" flooding="+(boilerFlooding*100f).ToString("F0")+" pressure vessel severely damaged");
    }
    private void EmitSmoke(float dt,Vector3 shipVelocity)
    {
        if(smoke==null||smokeMaterial==null||!NavalSteamSmokeMath.Emits(Twin?(state.LastBurnRate+secondState.LastBurnRate)*.5f:state.LastBurnRate,Twin?(boilerFlooding+secondFlood)*.5f:boilerFlooding,FunnelIntegrity))return;
        Vector3 top;if(!TrySmokeOutlet(out top))return;
        float density=NavalSteamSmokeMath.Density(Twin?(state.LastBurnRate+secondState.LastBurnRate)*.5f:state.LastBurnRate,Pressure);
        smokeClock-=dt;if(smokeClock>0)return;smokeClock=Mathf.Lerp(.70f,.14f,density);
        // A marine funnel produces a coherent vertical column at the mouth.
        // The old generic plume expanded four-fold and looked like a broad cloud.
        smoke.EmitPlume(top,shipVelocity*.12f+Vector3.up*Mathf.Lerp(1.8f,3.0f,density),smokeMaterial,
            Mathf.Lerp(.16f,.29f,density),Mathf.Lerp(4.8f,6.6f,density),1.15f,3.15f);
        Vector3 other;
        if(hull.Definition.CapitalShip&&TryCapitalSmokeOutlet(-2f,out other)&&(other-top).sqrMagnitude>1f)smoke.EmitPlume(other,shipVelocity*.12f+Vector3.up*Mathf.Lerp(1.8f,3f,density),smokeMaterial,
            Mathf.Lerp(.16f,.29f,density),Mathf.Lerp(4.8f,6.6f,density),1.15f,3.15f);
    }
    private bool TrySmokeOutlet(out Vector3 point)
    {if(!hull.Definition.CapitalShip)return TryCapitalSmokeOutlet(float.NaN,out point);return TryCapitalSmokeOutlet(.4f,out point)||TryCapitalSmokeOutlet(-2f,out point);}
    private bool TryCapitalSmokeOutlet(float z,out Vector3 point)
    {
        NavalHullVoxel highest=null;float highestY=float.NegativeInfinity;
        foreach(NavalHullVoxel cell in funnel)
            if(cell!=null&&(float.IsNaN(z)||Mathf.Abs(cell.LocalCenter.z-z)<.3f)&&NavalSteamSmokeMath.IsExternalOutlet(cell.Missing,cell.Detached,cell.LocalCenter.y,hull.Definition.WeatherDeck)&&cell.LocalCenter.y>highestY)
            {highest=cell;highestY=cell.LocalCenter.y;}
        if(highest==null){point=Vector3.zero;return false;}
        // Follow the actual surviving top cell.  If the cap or stack falls,
        // smoke moves to the remaining stump; with no exterior stack it stops.
        point=highest.transform.position+hull.Root.up*Mathf.Abs(highest.transform.lossyScale.y)*.5f;
        return true;
    }
    public void Restore(){if(oilStates!=null)foreach(var v in oilStates)v.Reset();state.Reset();secondState.Reset();earlyRetreat.Reset();secondFlood=0;setting=smokeClock=steamClock=scaldClock=boilerFlooding=rupturedAt=0f;pumpAssist=1f;pumpDemandUntil=0f;liveLeakLogged=pumpSteamLogged=false;steamExposure.Clear();}
    public string Diagnostics
    {get{if(oilStates!=null)return OilDiagnostics;return "telegraph="+(setting*100f).ToString("F0")+" pressure="+(Pressure*100f).ToString("F0")+" coal="+Coal.ToString("F1")+" boiler="+(BoilerIntegrity*100f).ToString("F0")+" engine="+(EngineIntegrity*100f).ToString("F0")+" funnel="+(FunnelIntegrity*100f).ToString("F0")+" mains="+(PropulsionMainIntegrity*100f).ToString("F0")+"/"+(PumpMainIntegrity*100f).ToString("F0")+"/"+(AuxiliaryMainIntegrity*100f).ToString("F0")+" drive="+DriveContinuity.ToString("F2")+" screw="+Integrity(screwCells).ToString("F2")+" pump="+PumpAssist.ToString("F2")+"x flood="+(boilerFlooding*100f).ToString("F0")+" burn="+(state.Combustion*100f).ToString("F0")+" twin="+Twin+" secondaryPressure="+(secondState.Pressure*100f).ToString("F0")+" secondaryFlood="+(secondFlood*100f).ToString("F0")+" ruptured="+state.Ruptured;}}
    private void OnDestroy(){if(steamMaterial!=null)Destroy(steamMaterial);if(smokeMaterial!=null)Destroy(smokeMaterial);}
}

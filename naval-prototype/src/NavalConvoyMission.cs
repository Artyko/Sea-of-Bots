using System.Collections.Generic;
using UnityEngine;

public partial class NavalTestWorld
{
    public bool ConvoyMission {get;private set;}
    private static bool selectedInterceptorSide;
    private const float ConvoyFixedStep=1f/40f;
    private float savedFixedStep;
    private bool ownsFixedStep;
    private void ApplyConvoyPhysicsStep()
    {
        if(ownsFixedStep||Time.fixedDeltaTime>=ConvoyFixedStep)return;
        savedFixedStep=Time.fixedDeltaTime;
        Time.fixedDeltaTime=ConvoyFixedStep;
        ownsFixedStep=true;
    }
    private void RestoreConvoyPhysicsStep()
    {
        if(!ownsFixedStep)return;
        if(Time.fixedDeltaTime==ConvoyFixedStep)Time.fixedDeltaTime=savedFixedStep;
        ownsFixedStep=false;
    }
    private bool interceptorSide;
    private Transform flagship;
    private NavalShipDefinition previousPlayerProfile,previousEnemyProfile;
    private const float PassageSeconds=720f;
    private float convoyFinishZ,convoyElapsed,screenReviewAt;
    private Transform screenEscort;
    private readonly Dictionary<Transform,int> missionSlots=new Dictionary<Transform,int>();
    private string convoyResult,alliedSummary,enemySummary;
    private bool convoyWon;
    private int finalConvoyShips,finalInterceptors;
    private readonly HashSet<Character> rescuedSailors=new HashSet<Character>();
    private readonly Dictionary<Character,int> convoySailors=new Dictionary<Character,int>();
    private readonly int[] shellsFired=new int[2],shellHits=new int[2],torpedoesFired=new int[2],torpedoHits=new int[2],rescued=new int[2];

    public void RecordConvoySailor(Character sailor,NavalHull ship)
    {
        if(ConvoyMission&&convoyResult==null&&sailor!=null)convoySailors[sailor]=ship.Definition.Destroyer?1:0;
    }
    public void RecordConvoyShot(Transform source,bool torpedo,bool hit)
    {
        if(!ConvoyMission||convoyResult!=null)return;
        int slot;if(!missionSlots.TryGetValue(source,out slot))return;
        int side=slot>=4?1:0;
        if(torpedo){if(hit)torpedoHits[side]++;else torpedoesFired[side]++;}
        else {if(hit)shellHits[side]++;else shellsFired[side]++;}
    }
    private void CountConvoyShips(out int escorts,out int interceptors)
    {
        escorts=0;interceptors=0;
        if(hull!=null&&!hull.Sunk){if(interceptorSide)interceptors++;else escorts++;}
        foreach(var ship in convoyFleet)if(ship!=null&&!ship.Hull.Sunk)
        {if(ship.Hull.Definition.Destroyer)interceptors++;else escorts++;}
    }
    private static string Accuracy(int hits,int shots)
    {return shots>0?(100f*hits/shots).ToString("F1")+"%":"—";}
    private string ConvoySideSummary(string title,int side,int lost,int dead)
    {
        return title+"\nПотеряно кораблей "+lost+
            "\nПогибло матросов "+dead+
            "\nСпасено матросов "+rescued[side]+
            "\nАртиллерия "+Accuracy(shellHits[side],shellsFired[side])+
            "\nТорпеды "+Accuracy(torpedoHits[side],torpedoesFired[side]);
    }
    private void CaptureConvoySummary()
    {
        int[] dead=new int[2];
        foreach(var sailor in convoySailors)if(sailor.Key==null||!sailor.Key.IsAlive())dead[sailor.Value]++;
        int playerSide=interceptorSide?1:0,enemySide=1-playerSide;
        int[] lost={4-finalConvoyShips,3-finalInterceptors};
        alliedSummary=ConvoySideSummary("Мы и союзники",playerSide,lost[playerSide],dead[playerSide]);
        enemySummary=ConvoySideSummary("Противник",enemySide,lost[enemySide],dead[enemySide]);
    }
    private readonly NavalTorpedoAvoidance convoyPlayerWatch=new NavalTorpedoAvoidance();
    private float torpedoWarningUntil;
    private readonly Dictionary<Transform,Vector3> escortOffsets=new Dictionary<Transform,Vector3>();
    private readonly List<NavalEnemyShip> convoyFleet=new List<NavalEnemyShip>(6);

    private void ConfigureConvoy()
    {
        foreach(var placed in Object.FindObjectsByType<ObjectPlacedInLevel>(FindObjectsSortMode.None))
            if(placed.name==NavalEraMarker.ConvoyWater||placed.name==NavalEraMarker.AirRaidWater)
            {ConvoyMission=true;AirRaidMission=placed.name==NavalEraMarker.AirRaidWater;break;}
        if(!ConvoyMission)return;
        interceptorSide=!AirRaidMission&&selectedInterceptorSide;
        previousPlayerProfile=NavalSessionSettings.PlayerShip;
        previousEnemyProfile=NavalSessionSettings.EnemyShip;
        NavalSessionSettings.PlayerShip=interceptorSide?NavalShipDefinition.Novik:NavalShipDefinition.Dreadnought;
        NavalSessionSettings.EnemyShip=interceptorSide?NavalShipDefinition.Dreadnought:NavalShipDefinition.Novik;
        convoyFinishZ=waterCenter.z+2300f;
    }

    private Vector3 ConvoySpawn(int slot)
    {
        switch(slot)
        {
            case 1:return waterCenter+new Vector3(-105f,0f,50f);
            case 2:return waterCenter+new Vector3(105f,0f,50f);
            case 3:return waterCenter+new Vector3(0f,0f,150f);
            case 4:return waterCenter+new Vector3(-340f,0f,1250f);
            case 5:return waterCenter+new Vector3(0f,0f,1430f);
            case 6:return waterCenter+new Vector3(340f,0f,1250f);
            default:return waterCenter;
        }
    }

    private void PlaceConvoyPlayer()
    {
        boat.position=ConvoySpawn(interceptorSide?5:0);
        boat.rotation=Quaternion.LookRotation(interceptorSide?Vector3.back:Vector3.forward);
    }

    private void SpawnConvoy()
    {
        int playerSlot=interceptorSide?5:0;
        missionSlots.Add(boat,playerSlot);
        if(!interceptorSide)flagship=boat;
        for(int slot=0;slot<(AirRaidMission?4:7);slot++)
        {
            if(slot==playerSlot)continue;
            bool escort=slot<4;
            bool allied=escort!=interceptorSide;
            var definition=slot==0?NavalShipDefinition.Dreadnought:escort?NavalShipDefinition.Ironclad:NavalShipDefinition.Novik;
            var root=new GameObject("NavalPrototype_Convoy_"+slot);
            root.transform.SetParent(transform,false);
            var ship=root.AddComponent<NavalEnemyShip>();
            ship.Initialize(player,boat,ocean,footOffset,definition,allied,ConvoySpawn(slot),escort?Vector3.forward:Vector3.back,escort);
            convoyFleet.Add(ship);
            missionSlots.Add(ship.Boat,slot);
            if(slot>0&&escort)escortOffsets.Add(ship.Boat,ConvoySpawn(slot)-waterCenter);
            if(slot==0)flagship=ship.Boat;
        }
        ApplyConvoyPhysicsStep();
        if(AirRaidMission){StartAirRaid();return;}
        TickConvoy();
        Debug.Log("[NavalPrototype] Convoy Interception: seven ships, player side="+(interceptorSide?"interceptors":"convoy"));
    }

    public bool AcceptConvoySurvivor(NavalHull origin,NavalHull destination,Character sailor,NavalRepairSailor worker)
    {
        if(!ConvoyMission||origin==destination||destination.Sunk)return false;
        var controls=NavalStations.ForShip(destination.Root);
        if(controls==null||controls.HostileToPlayer==sailor.IsPlayerTeam)return false;
        bool accepted=false;
        if(destination==hull)accepted=allies!=null&&allies.ReceiveSurvivor(origin,sailor,worker);
        else foreach(var member in convoyFleet)
            if(member!=null&&member.Hull==destination)
            {accepted=member.ReceiveSurvivor(origin,sailor,worker);break;}
        if(accepted&&convoyResult==null&&rescuedSailors.Add(sailor))
            rescued[destination.Definition.Destroyer?1:0]++;
        return accepted;
    }

    private readonly Dictionary<Transform,Transform> convoyEngagementTargets=new Dictionary<Transform,Transform>();
    public Transform ConvoyTarget(Transform source,bool allied)
    {
        if(source==null)return null;
        Transform incumbent;
        convoyEngagementTargets.TryGetValue(source,out incumbent);
        Transform best=null;
        float distance=float.PositiveInfinity;
        if(!allied&&!hull.Sunk){best=boat;distance=ConvoyTargetScore(source,boat)*(boat==incumbent?.8f:1f);}
        foreach(var ship in convoyFleet)
        {
            if(ship==null||ship.Allied==allied||ship.Hull.Sunk)continue;
            float candidate=ConvoyTargetScore(source,ship.Boat)*(ship.Boat==incumbent?.8f:1f);
            if(candidate<distance){best=ship.Boat;distance=candidate;}
        }
        convoyEngagementTargets[source]=best;
        return best;
    }

    private float ConvoyTargetScore(Transform source,Transform candidate)
    {
        float distance=(candidate.position-source.position).sqrMagnitude;
        int slot;
        if(!missionSlots.TryGetValue(source,out slot)||slot>=4)return distance;
        var controls=NavalStations.ForShip(candidate);
        var torpedoes=controls!=null?controls.Torpedoes:null;
        if(torpedoes!=null&&torpedoes.Rounds>0&&
            (Time.time<=torpedoes.LaunchWindowUntil||Mathf.Abs(Vector3.Dot(candidate.right,(source.position-candidate.position).normalized))>.85f))
            return distance*.55f;
        return distance;
    }

    public int InterceptorLane(Transform ship)
    {
        int slot;
        if(!missionSlots.TryGetValue(ship,out slot)||slot<4)return 0;
        return slot==4?-1:slot==6?1:2;
    }

    private void ReviewScreen()
    {
        if(flagship==null||Time.time<screenReviewAt)return;
        screenReviewAt=Time.time+1f;
        float best=520f*520f;
        Transform next=null;
        foreach(var escort in convoyFleet)
        {
            if(escort==null||!escort.InConvoy||escort.Boat==flagship||escort.Hull.Sunk||escort.Retreating)continue;
            float separation=(escort.Boat.position-flagship.position).sqrMagnitude;
            if(separation>(escort.Boat==screenEscort?265f*265f:185f*185f))continue;
            Transform threat=ConvoyTarget(escort.Boat,escort.Allied);
            if(threat==null||(threat.position-flagship.position).sqrMagnitude>650f*650f)continue;
            float score=(threat.position-escort.Boat.position).sqrMagnitude;
            if(escort.Boat==screenEscort)score*=.7f;
            if(score<best){best=score;next=escort.Boat;}
        }
        screenEscort=next;
    }

    public Vector3 FleetVelocity(Transform ship)
    {
        if(ship==null)return Vector3.zero;
        if(ship==boat)return ShipVelocity;
        foreach(var member in convoyFleet)if(member!=null&&member.Boat==ship)return member.ShipVelocity;
        return Vector3.zero;
    }

    public Vector3 ConvoyCourse(Transform ship)
    {
        if(convoyResult!=null||ship==screenEscort)return Vector3.zero;
        Vector3 offset;
        if(flagship==null||!escortOffsets.TryGetValue(ship,out offset))return Vector3.forward;
        Vector3 station=flagship.position+offset+Vector3.forward*100f;
        Vector3 course=station-ship.position;
        course.y=0;
        return course.z<30f?new Vector3(course.x,0,30f):course;
    }

    public IEnumerable<NavalHull> ConvoyHulls()
    {
        yield return hull;
        foreach(var ship in convoyFleet)if(ship!=null&&!ship.Hull.SimulationRetired)yield return ship.Hull;
    }

    internal void AirRaidLosses(out int lost,out int dead)
    {
        int ships,interceptors;CountConvoyShips(out ships,out interceptors);
        lost=4-ships;dead=0;
        foreach(var sailor in convoySailors)if(sailor.Key==null||!sailor.Key.IsAlive())dead++;
    }

    private void RestoreConvoyProfiles()
    {
        if(!ConvoyMission)return;
        NavalSessionSettings.PlayerShip=previousPlayerProfile;
        NavalSessionSettings.EnemyShip=previousEnemyProfile;
    }

    public bool ResolveFleetContact(NavalRamDamage mover,Vector3 start,Quaternion rotation,
        ref Vector3 next,ref Quaternion nextRotation,out float retention)
    {
        retention=1f;
        bool blocked=false;
        for(int slot=0;slot<NavalPhysicsQueries.HullSlotCount;slot++)
        {
            NavalHull other=NavalPhysicsQueries.HullAtSlot(slot);
            if(other==null||other==mover.Hull||other.Root==null)continue;
            float range=mover.Hull.Definition.HullLength+other.Definition.HullLength+40f;
            if((other.Root.position-start).sqrMagnitude>range*range)continue;
            var contact=other.Root.GetComponent<NavalRamDamage>();
            if(NavalShipContact.Constrain(start,rotation,ref next,ref nextRotation,other.Root,mover.Hull.Root))
            {
                mover.Contact(contact);
                mover.PushContact(contact);
                blocked=true;
            }
            retention=Mathf.Min(retention,NavalRigContact.Resolve(mover,start,rotation,ref next,ref nextRotation,contact));
        }
        return blocked;
    }

    private void TickConvoy()
    {
        using(NavalPerformance.Measure(NavalPerfArea.ConvoyCoordinator)) TickConvoyMeasured();
    }
    private void TickConvoyMeasured()
    {
        if(AirRaidMission)return;
        if(stations.AtHelm&&!hull.Sunk)
        {
            float suggestedThrottle=1f;
            Vector3 suggestedCourse;
            if(convoyPlayerWatch.Course(boat,hull,speed,ref suggestedThrottle,out suggestedCourse))
                torpedoWarningUntil=Time.time+2f;
        }
        Transform target=ConvoyTarget(boat,true);
        enemy=null;
        foreach(var ship in convoyFleet)if(ship!=null&&ship.Boat==target){enemy=ship;break;}
        if(convoyResult!=null||flagship==null)return;
        ReviewScreen();
        convoyElapsed+=Mathf.Min(Time.deltaTime,.05f);
        var flagshipRam=flagship.GetComponent<NavalRamDamage>();
        if(flagshipRam!=null&&flagshipRam.Hull.Sunk){convoyWon=interceptorSide;convoyResult="Дредноут потоплен";}
        else if(flagship.position.z>=convoyFinishZ){convoyWon=!interceptorSide;convoyResult="Дредноут вышел из зоны перехвата";}
        else
        {
            bool interceptorsRemain=interceptorSide&&!hull.Sunk;
            foreach(var ship in convoyFleet)
                if(ship!=null&&ship.Hull.Definition.Destroyer&&!ship.Hull.Sunk)interceptorsRemain=true;
            if(!interceptorsRemain){convoyWon=!interceptorSide;convoyResult="Перехватчики потоплены";}
            else if(convoyElapsed>=PassageSeconds){convoyWon=interceptorSide;convoyResult="Конвой задержан";}
        }
        if(convoyResult!=null)
        {
            CountConvoyShips(out finalConvoyShips,out finalInterceptors);
            CaptureConvoySummary();
            Debug.Log("[NavalPrototype] Convoy result: "+convoyResult+" | "+alliedSummary.Replace('\n',' ')+" | "+enemySummary.Replace('\n',' '));
        }
    }

    private GUIStyle convoyPanelStyle;

    private void DrawConvoy()
    {
        if(AirRaidMission){if(AirRaid!=null)AirRaid.Draw();return;}
        string objective=interceptorSide?"Потопить дредноут":"Провести дредноут через зону перехвата";
        float remaining=flagship!=null?Mathf.Max(0,convoyFinishZ-flagship.position.z):0;
        string progress="До выхода "+remaining.ToString("F0")+" м · осталось "+
            Mathf.CeilToInt(Mathf.Max(0,PassageSeconds-convoyElapsed))+" с";
        int escorts=finalConvoyShips,interceptors=finalInterceptors;
        if(convoyResult==null)CountConvoyShips(out escorts,out interceptors);
        string counts="Конвой "+escorts+"/4 · Эсминцы "+interceptors+"/3";
        var label=new GUIContent("Перехват конвоя · "+counts+"\n"+
            (convoyResult==null?objective+"\n"+progress:
            (convoyWon?"Миссия выполнена — ":"Миссия провалена — ")+convoyResult+"\nВремя "+Mathf.RoundToInt(convoyElapsed)+" с · Точность стрельбы")+
            "\nF1 — конвой   F2 — перехватчики   F5 — заново");
        if(convoyPanelStyle==null)
            convoyPanelStyle=new GUIStyle(GUI.skin.box){wordWrap=true,alignment=TextAnchor.UpperCenter};
        float width=Mathf.Min(620f,Screen.width-24f);
        float height=Mathf.Max(76f,convoyPanelStyle.CalcHeight(label,width)+8f);
        float x=(Screen.width-width)*.5f;
        GUI.Box(new Rect(x,14,width,height),label,convoyPanelStyle);
        if(convoyResult!=null)
        {
            float columnWidth=(width-8f)*.5f;
            var left=new GUIContent(alliedSummary);
            var right=new GUIContent(enemySummary);
            float columnHeight=Mathf.Max(convoyPanelStyle.CalcHeight(left,columnWidth),
                convoyPanelStyle.CalcHeight(right,columnWidth))+8f;
            GUI.Box(new Rect(x,18f+height,columnWidth,columnHeight),left,convoyPanelStyle);
            GUI.Box(new Rect(x+columnWidth+8f,18f+height,columnWidth,columnHeight),right,convoyPanelStyle);
            height+=columnHeight+4f;
        }
        if(Time.time<torpedoWarningUntil)
            GUI.Box(new Rect(Screen.width*.5f-160,20+height,320,28),"Торпеды на курсе — уклоняйся");
    }

    private void UpdateConvoyInput()
    {
        if(!ConvoyMission)return;
        if(AirRaidMission){if(Input.GetKeyDown(KeyCode.F5))RestartRequested=true;return;}
        if(Input.GetKeyDown(KeyCode.F1)){selectedInterceptorSide=false;RestartRequested=true;}
        else if(Input.GetKeyDown(KeyCode.F2)){selectedInterceptorSide=true;RestartRequested=true;}
        else if(Input.GetKeyDown(KeyCode.F5))RestartRequested=true;
    }
}

using ModLibrary;
using UnityEngine;

public class NavalAlliedCrew : MonoBehaviour
{
    private FirstPersonMover player;
    private NavalHull hull;
    private NavalStructure structure;
    private float footOffset,retry;
    private Character[] crew;
    private NavalRepairSailor[] workers;
    private bool[] spawned;
    private NavalStations stations;
    private readonly NavalCrewController roles=new NavalCrewController();
    public int LivingCrew {get{int n=0;if(crew!=null)foreach(Character sailor in crew)if(sailor!=null&&sailor.IsAlive())n++;return n;}}
    public bool RecoveringOverboard {get{return roles.RecoveringOverboard;}}
    public bool NeedsRetreat {get{return roles.NeedsRetreat;}}
    public bool HasHelmsman { get { return roles.HasHelmsman; } }
    private Vector3 SpawnPost(int i){return i<=hull.Definition.GunCount?stations.Post(i):new Vector3(NavalShipDefinition.StandbyX(i),hull.Definition.WeatherDeck,NavalShipDefinition.StandbyZ(i));}
    public void Initialize(FirstPersonMover owner,NavalHull ship,NavalStructure frame,NavalStations controls,float feet)
    {player=owner;hull=ship;structure=frame;stations=controls;footOffset=feet;crew=new Character[ship.Definition.CrewCount];workers=new NavalRepairSailor[crew.Length];spawned=new bool[crew.Length];roles.Initialize(hull,stations,crew,workers);}
    private void LateUpdate()
    {
        if(NavalTestWorld.Active==null||!NavalTestWorld.Active.SimulationActive)return;
        if(Time.time>=retry)
        {
            retry=Time.time+2f;EnemyFactory factory=Singleton<EnemyFactory>.Instance;
            if(factory!=null)for(int i=0;i<hull.Definition.CrewCount;i++)if(!spawned[i])
            {
                Vector3 position=hull.Root.TransformPoint(SpawnPost(i))+Vector3.up*footOffset;
                Transform body=factory.SpawnEnemy(EnemyType.Hammer1,position,position+hull.Root.forward,null);
                if(body==null)continue;
                Character sailor=body.GetComponent<Character>();if(sailor==null){Destroy(body.gameObject);continue;}
                body.SetParent(transform,true);body.localScale=Vector3.one;
                sailor.IsPlayerTeam=true;sailor.SetIgnoreForVictoryCount(true);sailor.DisableAI();
                crew[i]=sailor;spawned[i]=true;
                workers[i]=body.gameObject.AddComponent<NavalRepairSailor>();workers[i].Initialize(sailor,hull,structure,SpawnPost(i),footOffset);
                body.gameObject.AddComponent<NavalSailorRescue>().Initialize(sailor,workers[i],hull);
            }
        }
        NavalTestWorld world=NavalTestWorld.Active;
        NavalShipVisuals rig=hull.Root.GetComponent<NavalShipVisuals>();
        bool useful=NavalCrewRoles.HelmUseful(rig!=null?rig.SailPowerFactor:0f,world!=null?world.ShipVelocity.magnitude:0f);
        roles.Tick(Mathf.Min(Time.deltaTime,0.05f),world!=null?world.EnemyBoat:null,world!=null?world.EnemyVelocity:Vector3.zero,useful,footOffset);
    }
    private void OnGUI()
    {
        Camera camera=NavalTestWorld.Active!=null?NavalTestWorld.Active.ViewCamera:null;if(camera==null)return;
        for(int i=0;i<hull.Definition.CrewCount;i++)if(crew[i]!=null&&crew[i].IsAlive())
        {
            Vector3 screen=camera.WorldToScreenPoint(crew[i].transform.position+Vector3.up*2.2f);
            if(screen.z>0f)GUI.Label(new Rect(screen.x-90f,Screen.height-screen.y-20f,200f,25f),"Ally: "+roles.Role(i));
        }
    }
    private void OnDestroy(){foreach(Character sailor in crew)if(sailor!=null)Destroy(sailor.gameObject);}
}

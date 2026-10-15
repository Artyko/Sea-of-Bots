using ModLibrary;
using UnityEngine;

public class NavalAlliedCrew : MonoBehaviour
{
    private FirstPersonMover player;
    private NavalHull hull;
    private NavalStructure structure;
    private float footOffset,retry;
    private readonly Character[] crew=new Character[2];
    private readonly NavalRepairSailor[] workers=new NavalRepairSailor[2];
    private readonly bool[] spawned=new bool[2];
    private NavalStations stations;
    private readonly NavalCrewController roles=new NavalCrewController();
    public bool HasHelmsman { get { return roles.HasHelmsman; } }
    private Vector3 Home(int i){return new Vector3(i==0?-1.7f:1.7f,0.825f,3.2f);}
    public void Initialize(FirstPersonMover owner,NavalHull ship,NavalStructure frame,NavalStations controls,float feet)
    {player=owner;hull=ship;structure=frame;stations=controls;footOffset=feet*0.85f;roles.Initialize(hull,stations,crew,workers);}
    private void LateUpdate()
    {
        if(player==null||!player.IsAlive()||!player.IsPlayerInputEnabled()||Time.timeScale==0f)return;
        if(Time.time>=retry)
        {
            retry=Time.time+2f;EnemyFactory factory=Singleton<EnemyFactory>.Instance;
            if(factory!=null)for(int i=0;i<2;i++)if(!spawned[i])
            {
                Vector3 position=hull.Root.TransformPoint(Home(i))+hull.Root.up*footOffset;
                Transform body=factory.SpawnEnemy(EnemyType.Hammer1,position,position+hull.Root.forward,null);
                if(body==null)continue;
                Character sailor=body.GetComponent<Character>();if(sailor==null){Destroy(body.gameObject);continue;}
                body.SetParent(transform,true);body.localScale=Vector3.one*0.85f;
                sailor.IsPlayerTeam=true;sailor.SetIgnoreForVictoryCount(true);sailor.DisableAI();
                crew[i]=sailor;spawned[i]=true;
                workers[i]=body.gameObject.AddComponent<NavalRepairSailor>();workers[i].Initialize(sailor,hull,structure,Home(i),footOffset);
                body.gameObject.AddComponent<NavalSailorRescue>().Initialize(sailor,workers[i],hull);
            }
        }
        NavalTestWorld world=NavalTestWorld.Active;
        roles.Tick(Mathf.Min(Time.deltaTime,0.05f),world!=null?world.EnemyBoat:null,world!=null?world.EnemyVelocity:Vector3.zero,true,footOffset);
    }
    private void OnGUI()
    {
        Camera camera=player!=null?player.GetPlayerCamera():null;if(camera==null)return;
        for(int i=0;i<2;i++)if(crew[i]!=null&&crew[i].IsAlive())
        {
            Vector3 screen=camera.WorldToScreenPoint(crew[i].transform.position+Vector3.up*2.2f);
            if(screen.z>0f)GUI.Label(new Rect(screen.x-90f,Screen.height-screen.y-20f,200f,25f),"Ally: "+roles.Role(i));
        }
    }
    private void OnDestroy(){foreach(Character sailor in crew)if(sailor!=null)Destroy(sailor.gameObject);}
}

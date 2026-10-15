using UnityEngine;

public class NavalCrewController
{
    private NavalHull hull;
    private NavalStations stations;
    private Character[] crew;
    private NavalRepairSailor[] workers;
    private readonly int[] roles={-1,-1};
    private int combat=-1;
    private NavalDeckDefense defense;
    private readonly NavalBoarder[] boarders=new NavalBoarder[2];
    public bool HasHelmsman { get; private set; }
    public string Role(int i)
    {return workers[i]!=null&&workers[i].Rescuing?"Swimming to ladder":boarders[i]!=null&&boarders[i].Busy?boarders[i].Status:defense!=null&&defense.Guard==i?"Defending deck":workers[i]!=null&&workers[i].Busy?workers[i].Status:roles[i]==0?"Helmsman":roles[i]>0?(roles[i]==1?"Port gunner":"Starboard gunner"):"Standby";}
    public void Initialize(NavalHull ship,NavalStations controls,Character[] sailors,NavalRepairSailor[] repair)
    {hull=ship;stations=controls;crew=sailors;workers=repair;defense=new NavalDeckDefense(hull,crew,workers);}
    private bool Free(int i)
    {return i>=0&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].RepairDuty&&!workers[i].Stranded&&!workers[i].Defending&&!workers[i].Rescuing&&(boarders[i]==null||!boarders[i].Busy);}
    public void Tick(float dt,Transform enemy,Vector3 enemyVelocity,bool helmAllowed,float feetOffset)
    {
        for(int i=0;i<2;i++)if(crew[i]!=null){NavalSailorRescue rescue=crew[i].GetComponent<NavalSailorRescue>();if(rescue!=null)rescue.CheckForWater();}
        HasHelmsman=false;defense.Tick();if(hull.Sunk)return;
        for(int i=0;i<2;i++)if(crew[i]!=null&&workers[i]!=null)
        {
            if(boarders[i]==null){boarders[i]=crew[i].gameObject.AddComponent<NavalBoarder>();boarders[i].Initialize(crew[i],workers[i],hull);}
            if(NavalRamMath.Assault(stations.IsFunctional(1),stations.IsFunctional(2))&&roles[i]!=0&&!workers[i].RepairDuty&&!hull.HasActiveLeaks)
                boarders[i].TryStart(enemy);
        }
        int side=enemy!=null?stations.TargetSide(enemy.position):-1;
        if(side>=0&&(!stations.IsFunctional(side)||stations.PlayerStation==side))side=-1;
        if(side<0)combat=-1;
        else if(!Free(combat))combat=Free(0)?0:Free(1)?1:-1;
        for(int i=0;i<2;i++)if(workers[i]!=null)workers[i].Tick(dt,NavalCrewRoles.CanRepair(Free(i),i,combat)||(Free(i)&&hull.Volume>BoatDimensions.SinkVolume*BoatDimensions.VolumeScale*0.5f));
        int helm=-1;
        if(helmAllowed&&stations.PlayerStation!=0&&stations.IsFunctional(0))
        {
            helm=NavalCrewRoles.HelmCandidate(Free(0),Free(1),combat,stations.PlayerStation==0);
        }
        for(int i=0;i<2;i++)
        {
            if(!Free(i))continue;
            roles[i]=i==combat?side:i==helm?0:-1;
            Vector3 post=roles[i]==0?new Vector3(0f,0.825f,-4.8f):roles[i]>0?new Vector3(roles[i]==1?-1.35f:1.35f,0.825f,-1f):new Vector3(i==0?-1.7f:1.7f,0.825f,3.2f);
            // Yield the whole station to a player, including its physical space.
            workers[i].SetPost(post);
            if(workers[i].Busy)continue;
            Character sailor=crew[i];sailor.DisableAI();
            CharacterController controller=sailor.GetComponentInChildren<CharacterController>();bool enabled=controller!=null&&controller.enabled;
            if(enabled)controller.enabled=false;
            sailor.transform.position=hull.Root.TransformPoint(post)+hull.Root.up*workers[i].FootOffset;
            Vector3 facing=roles[i]>=0?stations.CrewForward(roles[i]):hull.Root.forward;facing.y=0f;
            if(facing.sqrMagnitude>0.01f)sailor.transform.rotation=Quaternion.LookRotation(facing);
            if(enabled)controller.enabled=true;
            if(roles[i]==0)HasHelmsman=true;
            if(roles[i]>0&&enemy!=null)
            {
                float flight=Vector3.Distance(hull.Root.position,enemy.position)/32f;
                Vector3 aim=enemy.TransformPoint(new Vector3(0f,0.05f,0f))+enemyVelocity*flight;
                stations.AimAndFire(roles[i]-1,aim,dt,sailor);
            }
        }
    }
}

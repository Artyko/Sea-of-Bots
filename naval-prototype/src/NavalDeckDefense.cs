using UnityEngine;
using System.Reflection;
public class NavalDeckDefense
{
    private readonly NavalHull hull;
    private readonly Character[] crew;
    private readonly NavalRepairSailor[] workers;
    private Character intruder;
    private float scanAt;
    private Matrix4x4 previousBoat;
    private static readonly MethodInfo SetOpponent=typeof(AIController).GetMethod("SetTargetOpponentCharacter",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    public int Guard {get;private set;}
    public NavalDeckDefense(NavalHull ship,Character[] sailors,NavalRepairSailor[] repair)
    {hull=ship;crew=sailors;workers=repair;Guard=-1;}
    private bool Alive(int i)
    {return i>=0&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].Stranded&&!workers[i].Assaulting&&!workers[i].Rescuing;}
    private bool OnDeck(Character character)
    {
        if(character==null)return false;
        Vector3 local=hull.Root.InverseTransformPoint(character.transform.position);
        if(!NavalBoardingRules.EnemyOnDeck(character.IsAlive(),character.IsNeutral,false,local.x,local.y,local.z))return false;
        RaycastHit hit;
        return Physics.Raycast(character.transform.position+hull.Root.up*0.2f,-hull.Root.up,out hit,3.5f,
            PhysicsManager.GetEnvironmentLayerMask(),QueryTriggerInteraction.Ignore)&&hit.collider.transform.IsChildOf(hull.Root)&&
            hull.Root.InverseTransformPoint(hit.point).y>=0.55f;
    }
    public void Tick()
    {
        int sample=-1;for(int i=0;i<crew.Length;i++)if(Alive(i)){sample=i;break;}
        if(sample<0||hull.Sunk){Stop();return;}
        if(intruder!=null&&(!OnDeck(intruder)||intruder.IsPlayerTeam==crew[sample].IsPlayerTeam))intruder=null;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.35f;
            if(intruder==null)foreach(Character candidate in Object.FindObjectsByType<Character>(FindObjectsSortMode.None))
                if(candidate.IsPlayerTeam!=crew[sample].IsPlayerTeam&&OnDeck(candidate))
                {intruder=candidate;break;}
        }
        if(intruder==null){Stop();return;}
        if(!Alive(Guard))
        {
            Stop();
            int chosen=-1;for(int pass=0;pass<2&&chosen<0;pass++)for(int i=0;i<crew.Length;i++)if(Alive(i)&&(pass>0||!workers[i].RepairDuty)){chosen=i;break;}
            if(chosen<0)return;
            Guard=chosen;workers[Guard].BeginDefense();crew[Guard].ActivateAI();
            previousBoat=hull.Root.localToWorldMatrix;
        }
        Character sailor=crew[Guard];
        CharacterController controller=sailor.GetComponentInChildren<CharacterController>();
        bool enabled=controller!=null&&controller.enabled;if(enabled)controller.enabled=false;
        sailor.transform.position=hull.Root.TransformPoint(previousBoat.inverse.MultiplyPoint3x4(sailor.transform.position));
        if(enabled)controller.enabled=true;
        previousBoat=hull.Root.localToWorldMatrix;
        FirstPersonMover mover=sailor as FirstPersonMover;
        AIController ai=mover!=null?mover.GetAIController():sailor.GetComponent<AIController>();
        if(ai!=null){ai.SetTargetOverrides(false,false);if(ai.GetTargetOpponentCharacter()!=intruder&&SetOpponent!=null)SetOpponent.Invoke(ai,new object[]{intruder});}
        // Never pursue a boarder off the ship: a swimmer ceases to be a valid target.
        Vector3 position=hull.Root.InverseTransformPoint(sailor.transform.position);
        if(Mathf.Abs(position.x)>3.2f||Mathf.Abs(position.z)>6.2f||position.y<0.4f)Stop();
    }
    public void Stop()
    {
        if(Guard>=0&&workers[Guard]!=null&&crew[Guard]!=null)
        {
            crew[Guard].DisableAI();
            FirstPersonMover mover=crew[Guard] as FirstPersonMover;
            if(mover!=null&&mover.GetAIController()!=null&&SetOpponent!=null)SetOpponent.Invoke(mover.GetAIController(),new object[]{null});
            if(!workers[Guard].Rescuing)workers[Guard].EndDefense();
        }
        Guard=-1;
    }
}

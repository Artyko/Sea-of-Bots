using UnityEngine;
using System.Reflection;
public class NavalDeckDefense
{
    private readonly NavalHull hull;
    private readonly Character[] crew;
    private readonly NavalRepairSailor[] workers;
    private Character intruder;
    private float scanAt;
    private static readonly MethodInfo SetOpponent=typeof(AIController).GetMethod("SetTargetOpponentCharacter",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
    public int Guard {get;private set;}
    public NavalDeckDefense(NavalHull ship,Character[] sailors,NavalRepairSailor[] repair)
    {hull=ship;crew=sailors;workers=repair;Guard=-1;}
    private bool Alive(int i)
    {return i>=0&&crew[i]!=null&&crew[i].IsAlive()&&workers[i]!=null&&!workers[i].Stranded&&!workers[i].Assaulting&&!workers[i].Rescuing;}
    private bool OnDeck(Character character)
    {
        if(character==null)return false;
        for(int i=0;i<crew.Length;i++)if(Alive(i))
        {
            NavalBoarder navigator=crew[i].GetComponent<NavalBoarder>();
            return navigator!=null&&navigator.EnemyAboard(character);
        }
        return false;
    }
    public void Tick(int[] roles,bool helmUseful)
    {
        int sample=-1;for(int i=0;i<crew.Length;i++)if(Alive(i)){sample=i;break;}
        if(sample<0||hull.Sunk){Stop();return;}
        if(intruder!=null&&(!OnDeck(intruder)||intruder.IsPlayerTeam==crew[sample].IsPlayerTeam))intruder=null;
        if(Time.time>=scanAt)
        {
            scanAt=Time.time+0.35f;
            if(intruder==null)foreach(Character candidate in NavalCharacters.Snapshot())
                if(candidate!=null&&candidate.IsPlayerTeam!=crew[sample].IsPlayerTeam&&OnDeck(candidate))
                {intruder=candidate;break;}
        }
        if(intruder==null){Stop();return;}
        if(!Alive(Guard))
        {
            Stop();
            int guns=0;foreach(int role in roles)if(role>0)guns++;
            int chosen=-1;float best=float.MaxValue;
            for(int i=0;i<crew.Length;i++)if(Alive(i))
            {
                int role=roles[i]==0&&!helmUseful?-1:roles[i];
                float score=NavalCrewPriorities.DefenseCost(role,guns,workers[i].RepairDuty,workers[i].HasJob,hull.CombatCritical,
                    Vector3.Distance(crew[i].transform.position,intruder.transform.position));
                if(score<best){best=score;chosen=i;}
            }
            if(chosen<0)return;
            Guard=chosen;
        }
        NavalBoarder defense=crew[Guard].GetComponent<NavalBoarder>();
        if(defense==null){Stop();return;}
        defense.Defend(intruder);
    }

    public void Stop()
    {
        if(Guard>=0&&workers[Guard]!=null&&crew[Guard]!=null)
        {
            NavalBoarder defense=crew[Guard].GetComponent<NavalBoarder>();
            if(defense!=null)defense.StopDefense();
            else if(!workers[Guard].Rescuing)workers[Guard].EndDefense();
        }
        Guard=-1;
    }
}

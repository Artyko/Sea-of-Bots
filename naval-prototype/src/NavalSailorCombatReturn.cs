using UnityEngine;
public partial class NavalRepairSailor
{
    private bool combatReturnPending;
    private Vector3 combatReturnPost;
    // Promote completed maintenance before station allocation, not after the next task scan.
    public void OfferCombatHandoff(bool opportunity,bool fire,bool leaks,bool pumpDemand,bool danger)
    {
        if(robot==null||!robot.IsAlive()||stranded||Defending||Rescuing||Assaulting||FinalAttackDuty)return;
        bool active=HandlingSails||fireJob!=null||leak!=null||pumpDuty||bailState!=0||shieldJob!=null;
        if(!NavalRepairDispatch.ResumeGunnery(opportunity,nextTaskPending||ReturningWithoutWork||waitingForRepair,
            active,fire,leaks,pumpDemand,danger,BucketWater))return;
        Release();route.Clear();returning=false;changingPost=false;combatReturnPending=false;stuck=0;
    }
    public bool ReturnToGun(Vector3 post)
    {
        if(robot==null||!robot.IsAlive()||stranded||Defending||Rescuing||Assaulting||BucketWater>0f)return false;
        combatReturnPost=post;combatReturnPending=true;
        // Clearing the critical flag must not recall an operator who still has useful work.
        if(HandlingSails||nextTaskPending||fireJob!=null||pumpDuty&&hull.Bilge!=null&&hull.Bilge.WantsOperator)return false;
        if(!NavalRepairDispatch.CanRecall(leak!=null&&(leak.Missing||leak.Detached),hull.HasRepairDemand,OtherLeakRepairer(),BucketWater))return false;
        ResumeCombatPost();
        return true;
    }
    private void ResumeCombatPost()
    {
        Release();route.Clear();returning=false;changingPost=false;combatReturnPending=false;
        SetPost(combatReturnPost,true);
    }
}

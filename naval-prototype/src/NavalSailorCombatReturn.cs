using UnityEngine;
public partial class NavalRepairSailor
{
    private bool combatReturnPending;
    private Vector3 combatReturnPost;
    public bool ReturnToGun(Vector3 post)
    {
        if(robot==null||!robot.IsAlive()||stranded||Defending||Rescuing||Assaulting||BucketWater>0f)return false;
        combatReturnPost=post;combatReturnPending=true;
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

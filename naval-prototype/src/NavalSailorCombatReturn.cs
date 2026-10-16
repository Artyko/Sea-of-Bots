using UnityEngine;
public partial class NavalRepairSailor
{
    private bool combatReturnPending;
    private Vector3 combatReturnPost;
    public bool ReturnToGun(Vector3 post)
    {
        if(robot==null||!robot.IsAlive()||stranded||Defending||Rescuing||Assaulting)return false;
        combatReturnPost=post;combatReturnPending=true;
        ResumeCombatPost();
        return true;
    }
    private void ResumeCombatPost()
    {
        Release();route.Clear();returning=false;changingPost=false;combatReturnPending=false;
        SetPost(combatReturnPost,true);
    }
}

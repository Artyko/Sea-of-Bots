using UnityEngine;

public partial class NavalRepairSailor
{
    private NavalBilgeSystem.FloodShield shieldJob;
    private int shieldStage; // 1 fetch planks, 2 carry to opening, 3 assemble/remove
    private GameObject carriedBoards;
    private float passageWorkUntil;
    public bool AvailableForShield {get{return robot!=null&&robot.IsAlive()&&!Defending&&hull!=null&&!hull.Sunk;}}
    public bool WorkingOnPassage {get{return Time.time<passageWorkUntil;}}
    private bool ShieldWorking {get{return shieldJob!=null||Time.time<passageWorkUntil;}}
    private string ShieldStatus {get{return shieldJob==null?"Снимает щит для прохода":shieldStage==1?"Идёт за досками":shieldStage==2?"Несёт доски":(shieldJob.Removing?"Снимает щит ":"Закрепляет щит ")+Mathf.RoundToInt(shieldJob.Work/(shieldJob.Removing?NavalFloodResponseMath.RemoveSeconds:NavalFloodResponseMath.FitSeconds)*100f)+"%";}}
    public void WaitForPassage(){passageWorkUntil=Time.time+.2f;}
    public void ShieldPassageWork()
    {
        passageWorkUntil=Time.time+.2f;
        if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(Mathf.Sin(Time.time*12f)*35f,0f,0f);
    }
    private void ShowBoards(bool show)
    {
        if(!show){if(carriedBoards!=null)Destroy(carriedBoards);carriedBoards=null;return;}
        if(carriedBoards!=null)return;
        carriedBoards=new GameObject("Carried flood board bundle");carriedBoards.transform.SetParent(robot.transform,false);
        carriedBoards.transform.localPosition=new Vector3(0f,.1f,.6f);
        for(int i=0;i<3;i++)
        {
            GameObject plank=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(plank.GetComponent<Collider>());
            plank.transform.SetParent(carriedBoards.transform,false);plank.transform.localPosition=new Vector3(0f,i*.09f,0f);
            plank.transform.localScale=new Vector3(.9f,.07f,.2f);
            if(shieldJob!=null)plank.GetComponent<Renderer>().sharedMaterial=shieldJob.Cells[0].OriginalSurface;
        }
    }
    private void ReleaseShieldJob()
    {
        if(hull!=null&&hull.Bilge!=null)hull.Bilge.ReleaseShieldClaims(this);
        shieldJob=null;shieldStage=0;ShowBoards(false);passageWorkUntil=0f;
    }
}

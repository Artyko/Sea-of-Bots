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
    private bool TickShields(float dt,bool allowed)
    {
        NavalBilgeSystem system=hull.Bilge;
        if(system==null)return false;
        if(shieldJob==null)
        {
            if((!allowed&&!(returning&&!changingPost))||leak!=null||pumpDuty||bailState!=0)return false;
            if(!system.WantsShieldWorker)return false;
            shieldJob=system.ClaimShield(this);if(shieldJob==null)return false;
            shieldStage=shieldJob.Removing?2:1;
            feet=boat.InverseTransformPoint(robot.transform.position-boat.up*footOffset);
            returning=false;changingPost=false;route.Clear();stuck=0f;
            Vector3 point=shieldJob.Post;if(shieldStage==1)point.z=shieldJob.Rack.z;
            PlanTo(point);
        }
        if(shieldJob.Worker!=this){ReleaseShieldJob();BeginReturn();return false;}
        shieldJob.Lease=Time.time+1f;robot.DisableAI();
        if(route.Count>0)
        {
            RouteResult movement=FollowRoute(dt,5f);
            if(movement==RouteResult.Blocked||movement==RouteResult.Fell)
            {system.ReleaseShield(shieldJob,this,true);ReleaseShieldJob();BeginReturn();}
            return true;
        }
        Walking(false,dt);HoldPosition();
        if(shieldStage==1)
        {
            ShowBoards(true);shieldStage=2;PlanTo(shieldJob.Post);return true;
        }
        shieldStage=3;ShowBoards(false);
        Vector3 face=boat.TransformPoint(shieldJob.Opening)-robot.transform.position;face.y=0f;
        if(face.sqrMagnitude>.01f)robot.transform.rotation=Quaternion.LookRotation(face);
        if(hammer!=null)hammer.localRotation=hammerRest*Quaternion.Euler(Mathf.Sin(Time.time*12f)*35f,0f,0f);
        if(!system.WorkShield(shieldJob,this,dt))
        {system.ReleaseShield(shieldJob,this,true);ReleaseShieldJob();BeginReturn();return true;}
        if(shieldJob.Worker!=this)
        {
            Debug.Log("[NavalPrototype] Flood boards "+(shieldJob.Installed?"installed":"removed")+" in compartment "+shieldJob.Region);
            ReleaseShieldJob();BeginReturn();
        }
        return true;
    }
}

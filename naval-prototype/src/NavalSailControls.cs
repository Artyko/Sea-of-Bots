using UnityEngine;
public partial class NavalStations
{
    private readonly NavalSailControlMath sailControl=new NavalSailControlMath();
    public int SailStation {get{return GunCount+1;}}
    public float SailSetting {get{return sailControl.Setting;}}
    public bool ManualSails {get{return sailControl.Manual;}}
    public NavalRepairSailor SailOperator;
    public bool RescueStop,LastAttackApproach,ReverseManeuver;
    private float requestedSails;
    private float EffectiveSailRequest {get{return RescueStop?0:requestedSails;}}
    private NavalShipVisuals sailRig;
    public bool NeedsSailOperator {get{
        if(sailRig==null)sailRig=hull.Root.GetComponent<NavalShipVisuals>();
        return (!ManualSails||RescueStop)&&!hull.Sunk&&sailRig!=null&&sailRig.SailPowerFactor>.01f&&Mathf.Abs(SailSetting-EffectiveSailRequest)>.04f;
    }}
    public bool SailOperatorSettled {get{return Mathf.Abs(SailSetting-EffectiveSailRequest)<=.04f;}}
    public float StepSails(float dt,float command,bool automated)
    {
        if(automated&&!ManualSails)requestedSails=Mathf.Round(Mathf.Clamp(command,-1,1)*10f)/10f;
        bool present=SailOperator!=null&&SailOperator.AtSailControls&&!SailOperator.Rescuing&&!SailOperator.Stranded;
        return sailControl.Step(dt,requestedSails,present,RescueStop?present:ManualSails||present,RescueStop);
    }
    public Vector3 SailPost {get{return new Vector3(NavalSailWorkLayout.PostX,hull.Definition.WeatherDeck,NavalSailWorkLayout.PostZ);}}
    public Vector3 SailRopePoint {get{return boat.TransformPoint(SailPost+new Vector3(NavalSailWorkLayout.CleatOffset,.75f,0)/hull.WorldScale);}}
}

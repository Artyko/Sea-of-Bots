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
    private NavalRamDamage propulsionMotion;
    public NavalSteamPlant SteamPlant {get{if(sailRig==null&&hull!=null)sailRig=hull.Root.GetComponent<NavalShipVisuals>();return sailRig!=null?sailRig.SteamPlant:null;}}
    public bool SteamPowered {get{return hull!=null&&hull.Definition.SteamPowered;}}
    public string PropulsionName {get{return SteamPowered?"машинный телеграф":"парусные снасти";}}
    public bool NeedsSailOperator {get{
        if(sailRig==null)sailRig=hull.Root.GetComponent<NavalShipVisuals>();
        NavalSteamPlant steam=SteamPlant;
        bool available=steam!=null?steam.Coal>0&&steam.BoilerIntegrity>0&&steam.EngineIntegrity>0:sailRig!=null&&sailRig.PowerFactor>.01f;
        bool work=Mathf.Abs(SailSetting-EffectiveSailRequest)>.04f||steam!=null&&steam.NeedsEngineer(EffectiveSailRequest);
        return (!ManualSails||RescueStop||steam!=null)&&!hull.Sunk&&available&&work;
    }}
    public bool SailOperatorSettled {get{NavalSteamPlant steam=SteamPlant;return Mathf.Abs(SailSetting-EffectiveSailRequest)<=.04f&&(steam==null||steam.EngineerSettled(SailSetting));}}
    public float StepPropulsion(float dt,float command,bool automated)
    {
        if(automated&&!ManualSails)requestedSails=Mathf.Round(Mathf.Clamp(command,-1,1)*10f)/10f;
        bool present=SailOperator!=null&&SailOperator.AtSailControls&&!SailOperator.Rescuing&&!SailOperator.Stranded;
        bool helmControl=SteamPowered&&(automated||PlayerStation==0);
        present|=helmControl;
        float setting=sailControl.Step(dt,requestedSails,present,RescueStop?present:ManualSails||present,RescueStop);
        if(propulsionMotion==null&&boat!=null)propulsionMotion=boat.GetComponent<NavalRamDamage>();
        return sailRig!=null?sailRig.UpdatePropulsion(dt,setting,present||PlayerStation==SailStation,propulsionMotion!=null?propulsionMotion.Velocity:Vector3.zero):setting;
    }
    public float StepSails(float dt,float command,bool automated)
    {return StepPropulsion(dt,command,automated);} // compatibility for existing checks and integrations
    public Vector3 SailPost {get{return hull.Definition.PropulsionPost;}}
    public Vector3 SailRopePoint {get{return boat.TransformPoint(SailPost+(SteamPowered?NavalSteamLayout.ControlOffset(hull.WorldScale):new Vector3(NavalSailWorkLayout.CleatOffset,.75f,0)/hull.WorldScale));}}
}

using System;
// One available combat hand can steer and visit one loaded cannon while the other hand repairs.
public sealed class NavalSkeletonCrewPolicy
{
    public const float HelmSeconds=6f,GunVisitSeconds=20f;
    private float steering,visit;
    public int GunStation {get;private set;}
    public void Reset(){GunStation=0;steering=visit=0;}
    public int Step(float dt,bool eligible,bool helmReady,int candidate,bool gunReady,bool bearing,bool danger)
    {
        dt=Math.Max(0,dt);
        if(!eligible||danger){Reset();return 0;}
        if(GunStation>0)
        {
            visit+=dt;
            // Reload beginning means the shot was fired. Never stand at the cannon for its whole reload.
            if(candidate!=GunStation||!gunReady||!bearing||visit>=GunVisitSeconds)Reset();
            return GunStation;
        }
        if(helmReady)steering+=dt;
        if(steering>=HelmSeconds&&candidate>0&&gunReady&&bearing)
        {GunStation=candidate;visit=0;}
        return GunStation;
    }
    public static bool SoloMaintenance(int availableCrew,bool maintenanceNeeded)
    {return availableCrew==1&&maintenanceNeeded;}
}

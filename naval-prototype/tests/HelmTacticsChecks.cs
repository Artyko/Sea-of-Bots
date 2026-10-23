using System;
class HelmTacticsChecks
{
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);}
    static void Main()
    {
        NavalHelmRecovery recovery=new NavalHelmRecovery();
        Check(recovery.BeginReverse()&&recovery.Phase==1,"ship contact starts reverse immediately");
        for(int i=0;i<70;i++){Check(!recovery.BeginReverse(),"repeated contact cannot restart reverse");recovery.Step(.1f,0,0,true,true,3);}
        Check(recovery.Phase==1,"reverse allowed to decelerate and build sternward speed");
        for(int i=0;i<235;i++){recovery.BeginReverse();recovery.Step(.1f,0,0,true,true,3);}
        Check(recovery.Phase==2,"blocked stern stops reverse within bounded time despite repeated contact");
        for(int i=0;i<45;i++)recovery.Step(.1f,.2f,0,true,false,3);
        int previous=recovery.Side;Check(recovery.BeginReverse()&&recovery.Side==-previous,"next recovery alternates turn direction");
        recovery.Step(.1f,0,0,false,true,3);Check(recovery.Phase==0,"manual takeover or no sail power cancels autonomous recovery");
        foreach(float own in new float[]{2,3})foreach(float other in new float[]{2,3})
        {
            float safe=(float)Math.Sqrt(6.45*6.45+12.75*12.75)*(own+other)*.5f+8;
            Check(NavalHelmTactics.Approach(safe-1,own,other)<0,"normal gunnery course increases clearance near scaled hulls");
            Check(Math.Abs(NavalHelmTactics.Approach(250,own,other)-NavalGunneryMath.ApproachWeight(250))<1e-6,"long range approach retained");
            Check(NavalHelmTactics.Approach(0,own,other)>=-1.5f,"repulsion bounded");
        }
        Check(NavalHelmTactics.Approach(45,3,3)<NavalHelmTactics.Approach(45,2,2),"brigantines keep more room than sloops");
        NavalCombatEmergency flood=new NavalCombatEmergency();flood.Step(.349f,0);Check(!flood.RepairRetreat,"below 35 percent stays in battle");
        flood.Step(.35f,0);Check(flood.RepairRetreat&&!flood.Critical,"35 percent retreats without releasing last gunner/buckets");
        flood.Step(.251f,0);Check(flood.RepairRetreat,"retreat remains latched above 25 percent");
        flood.Step(.25f,12);Check(!flood.RepairRetreat,"safe water and heel restore combat");
        flood.Step(.3f,16);Check(flood.RepairRetreat,"dangerous heel still initiates earlier retreat");
        Console.WriteLine("PASS: immediate bounded contact reverse, repeated-contact stability, alternating retry, control cancellation, scaled separation and 35/25 retreat.");
    }
}

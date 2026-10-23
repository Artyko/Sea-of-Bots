using System;
class HelmIntentChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
 Check(NavalHelmIntentMath.Mode(true,true,true)==3,"Rescue stop overrides battle requests");
 Check(NavalHelmIntentMath.Mode(false,true,true)==2,"Committed final ram does not fight retreat command");
 Check(NavalHelmIntentMath.Mode(false,false,true)==1,"Repair withdrawal overrides ordinary combat");
 float heading=179;heading=NavalHelmIntentMath.Heading(heading,-179,.05f,16);Check(Math.Abs(heading-179.8f)<.001,"Heading wrap takes short turn across north");
 foreach(float start in new float[]{-179,-90,0,90,179})foreach(float target in new float[]{-170,-80,0,80,170}){
 float h=start,lastError=Math.Abs(NavalBroadsideMath.Delta(h,target));
 for(int i=0;i<400;i++){float next=NavalHelmIntentMath.Heading(h,target,.05f,16);Check(Math.Abs(next-h)<=.80001,"Tactical handover cannot instantly flip course");h=next;float error=Math.Abs(NavalBroadsideMath.Delta(h,target));Check(error<=lastError+.001,"Committed target converges without oscillation");lastError=error;}
 Check(lastError<.001,"Stable target is reached");}
 var recovery=new NavalHelmRecovery();
 bool risk=NavalHelmIntentMath.ReverseRisk(false,true,false,100,3);
 for(int i=0;i<1000;i++)recovery.Step(.05f,.08f,0,true,risk,3);
 Check(recovery.Phase==0,"Sliding ship contact does not start reverse");
 bool started=false;for(int i=0;i<60;i++)started|=recovery.Step(.05f,0,0,true,risk,3);
 Check(started&&recovery.Phase==1,"Actually stuck ship contact triggers recovery");
 Check(!NavalHelmIntentMath.ReverseRisk(false,true,true,100,3),"Final ram is not cancelled by ordinary hull touch");
 Check(NavalHelmIntentMath.ReverseRisk(true,true,true,100,3),"Final ram still respects grounding");
 Check(NavalHelmIntentMath.TurnBudget(90,.5f)>NavalHelmIntentMath.TurnBudget(90,4),"Slow ship gets longer to complete its turn");
 Console.WriteLine("PASS: exclusive priorities, heading wrap/convergence, smooth tactical changes, moving vs stuck contact, final-ram grounding and turn budget.");
 }
}

using System;
class ReverseSailChecks
{
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);}
 static void Main(){
 foreach(float scale in new float[]{2,3,4}){
 var recovery=new NavalHelmRecovery();recovery.BeginReverse();
 var sails=new NavalSailControlMath();for(int i=0;i<100;i++)sails.Step(.05f,1,true,true);
 float speed=3;bool reversed=false;float backed=0;
 for(int frame=0;frame<1600&&recovery.Phase==1;frame++){
 bool operatorPresent=frame>=240; // Twelve seconds to get up from the hold.
 float setting=sails.Step(.05f,-.8f,true,operatorPresent);
 speed=NavalRigMath.StepSpeed(speed,setting,1,.05f,4.2f);
 float backwards=-speed*.05f;
 recovery.Step(.05f,Math.Abs(speed)*.05f,backwards,true,true,scale,setting,speed);
 if(setting>-.7f||speed>=-.1f)Check(recovery.Phase==1,"Cannot finish recovery while walking to ropes/furling/braking");
 if(setting<=-.7f&&speed<-.1f){reversed=true;backed=Math.Max(0,backed+backwards);}
 if(backed<6&&reversed)Check(recovery.Phase==1,"Short retreat is insufficient to clear island");
 }
 Check(reversed&&recovery.Phase==2,"Operator fully reaches reverse setting and ship eventually rejoins");
 Check(recovery.BackwardDistance>=NavalHelmRecovery.ReverseDistance(scale),"Clearance is based on scaled real reverse distance");
 }
 var position=new NavalHelmRecovery();position.BeginReverse();
 position.Step(.05f,2,2,true,true,3,-.8f,-2,-3);Check(position.BackwardDistance==0,"Backing after prior forward drift must first clear starting point");
 position.Step(.05f,5,5,true,true,3,-.8f,-2,2);Check(position.BackwardDistance==2,"Measure displacement on original reverse axis rather than traveled arc");
 var stalled=new NavalHelmRecovery();stalled.BeginReverse();
 for(int i=0;i<600;i++)stalled.Step(.05f,0,0,true,true,3,1,0);
 Check(stalled.Phase==1,"Time waiting for sail handling is not the reverse travel timer");
 for(int i=0;i<1850&&stalled.Phase==1;i++)stalled.Step(.05f,0,0,true,true,3,1,0);
 Check(stalled.Phase!=1,"Unavailable sail operator cannot hold recovery forever");
 Console.WriteLine("PASS: delayed rope operator, full negative sails, braking inertia, 13/19.5/26m reverse clearance and bounded missing-operator timeout.");
 }
}

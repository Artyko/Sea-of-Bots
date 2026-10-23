using System;
class HelmRecoveryChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
 var r=new NavalHelmRecovery();
 for(int i=0;i<150;i++)r.Step(.02f,.04f,0f,true,true,3f);
 Check(r.Phase==0,"Progress near an island does not trigger reverse");
 for(int i=0;i<150;i++)r.Step(.02f,0f,0f,true,false,3f);
 Check(r.Phase==0,"Stopped away from shore is not a grounding");
 for(int i=0;i<150;i++)r.Step(.02f,0f,0f,false,true,3f);
 Check(r.Phase==0,"Manual helm or destroyed sail does not autonomously reverse");
 bool started=false;
 for(int i=0;i<130;i++)started|=r.Step(.02f,0f,0f,true,true,3f);
 Check(started&&r.Phase==1,"Sustained grounding triggers backing out");
 int side=r.Side;
 r.Step(.1f,2f,0f,true,true,3f);Check(r.Phase==1&&r.BackwardDistance==0f,"Forward drift cannot count as backing clear");
 r.Step(.1f,20f,20f,true,true,3f);Check(r.Phase==2,"Sufficient real backward travel starts rejoin");
 for(int i=0;i<45;i++)r.Step(.1f,.1f,0f,true,false,3f,.65f,1f);
 Check(r.Phase==0,"Normal navigation resumes after rejoin");
 for(int i=0;i<130;i++)r.Step(.02f,0f,0f,true,true,3f);
 Check(r.Phase==1&&r.Side==-side,"Repeated grounding alternates the recovery side");
 for(int i=0;i<305;i++)r.Step(.1f,0f,0f,true,true,3f);
 Check(r.Phase==2,"Blocked stern does not reverse forever");
 r.Reset();Check(r.Phase==0,"Manual takeover cancels recovery");
 for(int i=0;i<130;i++)r.Step(.02f,0f,0f,true,true,2f);
 Check(r.Phase==1,"Same recovery works for sloop");
 r.Step(.1f,14f,14f,true,true,2f);Check(r.Phase==2,"Clearance scales with ship size");
 Check(NavalRigMath.RudderRate(-2f,1f)<0f,"Reverse rudder response is accounted for");
 Console.WriteLine("Helm recovery checks passed: progress, shore detection, reverse travel, bounded retry, alternate sides, manual control and both hull sizes.");
 }
}

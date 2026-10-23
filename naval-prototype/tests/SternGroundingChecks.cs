using System;
class SternGroundingChecks {
 static void Check(bool value,string why){if(!value)throw new Exception(why);}
 static void Main(){foreach(var ship in NavalShipDefinition.Choices){
 var recovery=new NavalHelmRecovery();recovery.BeginReverse();
 for(int i=0;i<100;i++)recovery.Step(.05f,0,0,true,true,ship.Scale,1,0);
 Check(recovery.Phase==1,"Waiting for ropes cannot count as stern grounding");
 for(int i=0;i<52;i++)recovery.Step(.05f,0,0,true,true,ship.Scale,-.8f,0);
 Check(recovery.Phase==2,"Stopped stern at shore switches to forward escape after 2.5s, even with constrained zero speed");
 for(int i=0;i<400;i++)recovery.Step(.05f,0,0,true,true,ship.Scale,-.8f,0);
 Check(recovery.Phase==2,"Forward escape retains recovery while negative sails have not been changed");
 var sails=new NavalSailControlMath();for(int i=0;i<100;i++)sails.Step(.05f,-.8f,true,true);
 float speed=-1f;for(int i=0;i<500&&recovery.Phase==2;i++){
 float setting=sails.Step(.05f,.65f,true,true);speed=NavalRigMath.StepSpeed(speed,setting,1,.05f,ship.MaxSailingSpeed);
 recovery.Step(.05f,Math.Max(0,speed)*.05f,0,true,false,ship.Scale,setting,speed);
 if(setting<.4f||speed<=.1f)Check(recovery.Phase==2,"Braking and sail crossing cannot release operator");
 }
 Check(recovery.Phase==0&&sails.Setting>=.4f&&speed>.1f,"Actual forward departure completes escape");
 recovery.BeginReverse();for(int i=0;i<52;i++)recovery.Step(.05f,0,0,true,true,ship.Scale,-.8f,0);
 for(int i=0;i<85&&recovery.Phase==2;i++)recovery.Step(.05f,0,0,true,true,ship.Scale,.65f,0);
 Check(recovery.Phase==1,"If bow also stays blocked, recovery retries the other side rather than idling");
 Check(NavalSailRecoveryPolicy.MayBorrow(true,false,false,false,true,true,false,false,false,false,false),"Refurbishment can yield to escape");
 Check(!NavalSailRecoveryPolicy.MayBorrow(false,false,false,true,true,true,false,false,false,false,false),"Critical active pump remains protected");
 Check(!NavalSailRecoveryPolicy.MayBorrow(false,false,false,false,false,false,false,false,true,false,true),"Sole leak repairer remains protected");
 Check(NavalSailRecoveryPolicy.MayBorrow(false,false,false,false,false,false,false,false,true,true,true),"Covered travelling repair hand can handle emergency ropes");
 Check(!NavalSailRecoveryPolicy.MayBorrow(false,true,true,false,false,false,false,true,false,false,false),"Loaded bucket must be emptied");
 }
 Console.WriteLine("PASS: stern contact -> forward sail handling -> real escape on all ships; no four-second premature release; bow-blocked retry and protected survival workers.");
 }
}

using System;
class SailControlChecks
{
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main()
 {
  var remote=new NavalSailControlMath();for(int i=0;i<100;i++)remote.Step(.05f,1,true,false);
  Check(remote.Setting==0&&remote.Target==0,"Remote AI request cannot move sails without a worker at the post");
  remote.Step(.5f,1,true,true);float held=remote.Setting;
  for(int i=0;i<100;i++)remote.Step(.05f,-1,true,false);
  Check(remote.Setting==held,"Leaving the sail post freezes automatic handling, preserving current setting");
  for(int i=0;i<100;i++)remote.Step(.05f,-1,true,true);Check(remote.Setting==-1,"An operator at the post can finish the requested reverse setting");
  var sails=new NavalSailControlMath();Check(sails.Setting==0,"New sails start furled");
  sails.Adjust(1,2.5f);for(int i=0;i<100;i++)sails.Step(.05f,-1,true);
  Check(sails.Setting==1,"Manual sail command overrides AI throttle");
  for(int i=0;i<100;i++)sails.Step(.05f,0,false);Check(sails.Setting==1,"Sails retain drive after leaving controls or losing helmsman");
  sails.Adjust(-1,2.5f);for(int i=0;i<100;i++)sails.Step(.05f,0,false);Check(Math.Abs(sails.Setting)<.001,"Furling removes sail drive");
  sails.Adjust(-1,2.5f);for(int i=0;i<100;i++)sails.Step(.05f,1,true);Check(sails.Setting==-1,"Reverse retained independently of wheel");
  sails.Auto();for(int i=0;i<100;i++)sails.Step(.05f,.5f,true);Check(Math.Abs(sails.Setting-.5f)<.001,"Crew auto control can resume");
  Check(NavalSailControlMath.ClothY(3,7,0)>6.7f&&NavalSailControlMath.ClothY(3,7,1)==3,"Cloth gathers upward and unfurls fully");
  foreach(var ship in NavalShipDefinition.Choices)
  {
   float speed=0;for(int i=0;i<2000;i++)speed=NavalRigMath.StepSpeed(speed,sails.Setting,.5f,.05f,ship.MaxSailingSpeed);
   Check(Math.Abs(speed-ship.MaxSailingSpeed*.25f)<.001,"Deployment and sail damage combine across all ship profiles");
  }
  var rescued=new NavalSailControlMath();rescued.Adjust(1,2.5f);
  for(int i=0;i<100;i++)rescued.Step(.05f,0,false);
  rescued.Step(1,0,true,false,true);Check(rescued.Setting==1,"Rescue cannot remotely furl sails");
  for(int i=0;i<100;i++)rescued.Step(.05f,0,true,true,true);
  Check(rescued.Setting==0&&rescued.Target==1&&rescued.Manual,"Rescue furls sails while retaining manual command");
  for(int i=0;i<100;i++)rescued.Step(.05f,0,false,true,false);
  Check(rescued.Setting==1,"Manual sail setting resumes after rescue");
  var state=new NavalStationState();state.PressInteract(7,false,7);Check(state.Active==7,"Extra sail station works without changing cannon indices");
  Console.WriteLine("PASS: manual/auto sail control, retained drive without helm, furling, reverse, cloth gathering, damage-scaled speed and station interaction.");
 }
}

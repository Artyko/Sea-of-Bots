using System;
class MechanismDamageChecks
{
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static void Main(){
  foreach(int total in new[]{4,17,25,40,100}){
   var state=new NavalMechanismDamage();
   state.Observe(total,0);Check(!state.Destroyed,"Intact mechanism is repairable");
   int limit=total/4;
   state.Observe(total,limit);Check(!state.Destroyed,"At most a quarter loss allows repair");
   state.Observe(total,0);Check(!state.Destroyed,"Minor damage repair keeps mechanism operational");
   state.Observe(total,limit+1);Check(state.Destroyed,"Above quarter loss is irreversible");
   state.Observe(total,limit);Check(state.Destroyed,"Small patch cannot bypass serious destruction");
   state.Observe(total,0);Check(state.Destroyed,"Partial/external restoration cannot clear destruction latch");
   state.Reset();state.Observe(total,0);Check(!state.Destroyed,"New ship or explicit debug full restore resets state");
   state.Observe(total,total);Check(state.Destroyed,"Complete destruction is not repairable");
  }
  Console.WriteLine("PASS: minor damage, inclusive 25 percent boundary, uneven voxel counts, severe loss, anti-patch bypass, total destruction and explicit reset.");
 }
}

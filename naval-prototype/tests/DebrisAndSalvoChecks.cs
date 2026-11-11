using System;
using UnityEngine;
class DebrisAndSalvoChecks{
 static void Main(){var v=new Vector3(62,0,0);for(int i=0;i<20;i++)v=NavalAircraftDebrisMath.StepVelocity(v,.05f);
  if(v.x>=62||v.x<30||v.y>=0||v.y< -10)throw new Exception("Debris must trail and descend under drag");
  if(NavalAircraftDebrisMath.Elapsed(.1f)!=.05f)throw new Exception("Debris clock must match aircraft");
  if(NavalTorpedoSalvoMath.AttackStyle(.1f,8,1200)!=NavalTorpedoSalvoMath.LongSingle||NavalTorpedoSalvoMath.AttackStyle(.4f,8,1200)!=NavalTorpedoSalvoMath.LongPair||NavalTorpedoSalvoMath.AttackStyle(.9f,8,1200)!=NavalTorpedoSalvoMath.SequentialFan)throw new Exception("Varied attack choices");
  Console.WriteLine("PASS: trailing debris, shared time step and long single/pair/sequential torpedo plans.");
 }
}

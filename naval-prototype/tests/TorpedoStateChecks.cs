using System;
class TorpedoStateChecks {
 static void Check(bool v,string message){if(!v)throw new Exception(message);}
 static void Main(){
  int leads=0;for(int angle=0;angle<360;angle+=5)for(int speed=0;speed<=10;speed+=2){double a=angle*Math.PI/180;float vx=(float)Math.Sin(a)*speed,vz=(float)Math.Cos(a)*speed,t;if(NavalTorpedoMath.Lead(900,200,vx,vz,out t)){double x=900+vx*t,z=200+vz*t;Check(Math.Abs(Math.Sqrt(x*x+z*z)-t*NavalTorpedoMath.Speed)<.01,"Intercept error");leads++;}}
  float time;Check(NavalTorpedoMath.Lead(1600,0,0,0,out time)&&Math.Abs(time-80)<.001,"Maximum motor range");Check(!NavalTorpedoMath.Lead(1601,0,0,0,out time),"Finite range");
  var safe=new NavalTorpedoWarheadState();Check(!safe.Impact(.9f)&&!safe.Lit,"Low sensitivity");Check(safe.Impact(.01f)&&safe.Spent,"Direct detonation probability");Check(!safe.Impact(0),"No duplicate detonation");
  var delayed=new NavalTorpedoWarheadState();Check(!delayed.Impact(.1f)&&delayed.Lit,"Delayed ignition");Check(!delayed.Step(9,false,false)&&delayed.Step(1,false,false),"Ten-second fire fuse");
  delayed.Reset();delayed.Ignite(0);delayed.Douse();Check(!delayed.Step(20,true,false)&&!delayed.Lit,"Crew extinguishing cancels fuse");delayed.Reset();delayed.Ignite(0);Check(!delayed.Step(20,true,true),"Submersion extinguishes warhead fire");
  var steam=new NavalSteamState();float fuel=steam.Coal;steam.LoseFuel(12);Check(Math.Abs(steam.Coal-(fuel-12))<.001,"Fuel leaks conserve stock");steam.LoseFuel(1000);Check(steam.Coal==0,"No negative fuel");
  var ship=NavalShipDefinition.Novik;Check(Math.Abs(NavalBallastLayout.Surface(5.8f/ship.Scale,14f/ship.Scale,ship.HoldFloor,ship)-ship.HoldFloor)<.001,"Double-bottom tanks do not invent a raised walking surface");
  Check(!NavalShipContactMath.Overlap(0,0,0,16,0,0,ship.Scale/2,ship.Scale/2,(ship.HullBeam+.9f)/ship.Scale,(ship.HullBeam+.9f)/ship.Scale),"Independent narrow collision beam");
  Console.WriteLine("PASS: "+leads+" moving-target solutions, finite run, separate warhead probabilities, extinguishing, fuel loss and narrow hull collision.");
 }
}

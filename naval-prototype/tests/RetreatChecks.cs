using System;
class RetreatChecks
{
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Main(){
 var state=new NavalRetreatState();
 state.Step(.1f,false,true,true);Check(!state.Active,"Manned guns do not trigger retreat");
 state.Step(.1f,true,false,true);Check(!state.Active,"No flooding: no repair retreat");
 state.Step(.1f,true,true,false);Check(!state.Active,"No opponent: no retreat");
 state.Step(.1f,true,true,true);Check(state.Active,"Unmanned guns and repairs trigger retreat");
 for(int i=0;i<200;i++)state.Step(.1f,false,true,true);
 Check(state.Active,"Continue until flooding is handled even if a gun is remanned");
 for(int i=0;i<40;i++)state.Step(.1f,false,false,true);
 Check(state.Active,"Brief quiet interval does not resume combat");
 state.Step(.1f,false,true,true);
 for(int i=0;i<40;i++)state.Step(.1f,false,false,true);
 Check(state.Active,"Renewed flooding resets quiet timer");
 for(int i=0;i<45;i++)state.Step(.1f,false,false,true);
 Check(!state.Active,"Resume after sustained completed repairs");
 state.Step(.1f,true,true,true);Check(state.Active,"A new emergency can start another retreat");
 state.Step(.1f,true,true,false);Check(!state.Active,"Lost threat or manual helm immediately clears autopilot retreat");
 var sloop=NavalShipDefinition.Sloop;
 Check(sloop.Compartments&&sloop.GunCount==2&&sloop.CrewCount==2,"Sloop gets systems with original crew and guns");
 Check(NavalCompartmentMath.WallX-sloop.StairWidth*.5f>.8f,"Sloop stair leaves side corridors clear");
 Check(sloop.StairWidth*sloop.Scale>2f,"Stair remains wide enough for robots and player");
 Console.WriteLine("Retreat and sloop layout checks passed");
 }
}

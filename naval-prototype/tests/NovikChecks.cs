using System;
using System.Collections.Generic;
class NovikChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var s=NavalShipDefinition.Novik;Check(Math.Abs(s.HullLength-102.4f)<.01&&Math.Abs(s.HullBeam-14)<.01,"Independent hull dimensions");Check(s.GunCount==3&&s.CrewCount==12&&s.HeavyCrewCount==0,"Three guns, twelve hands, no heavy turrets");Check(Math.Abs(s.HoldClearance-NavalShipDefinition.Ironclad.HoldClearance)<.001,"Steam hold clearance");
 var points=new List<NavalWalkPoint>();for(int i=0;i<s.GunCount;i++){var p=s.GunOperator(i);points.Add(new NavalWalkPoint(p.x,p.y,p.z));}for(int i=0;i<10;i++){var p=NavalCargoLayout.Pickup(s,NavalCargoLayout.Position(s,i));points.Add(p);}for(int side=-1;side<=1;side+=2)foreach(float z in new[]{26f,18f,10f,2f,-6f,-14f,-25f})points.Add(new NavalWalkPoint(side*3.6f/s.Scale,s.HoldFloor,z/s.Scale));points.Add(new NavalWalkPoint(0,s.HoldFloor,-30f/s.Scale));
 for(int i=0;i<4;i++){float[] zs={18,6,-3,-16};points.Add(new NavalWalkPoint(5.5f/s.Scale,s.WeatherDeck,(zs[i]-2.8f)/s.Scale));}
 points.Add(new NavalWalkPoint(0,s.HelmDeck,NavalShipDefinition.HelmZ(s)));points.Add(new NavalWalkPoint(3.6f/s.Scale,s.HoldFloor,-35.5f/s.Scale));
 int routes=0;foreach(var a in points)foreach(var b in points){var prior=a;foreach(var p in NavalSupplyRoutes.Plan(a,b,s)){Check(Math.Abs(p.Y-prior.Y)*s.Scale<.65f,"Route skips a deck height");Check(NavalCrewDeckSafety.Inside(p.X,p.Z,s,.8f),"Outside hull: "+p.X+","+p.Z);if(Math.Abs(p.Y-prior.Y)<.001&&!NavalDestroyerLayout.Stair(p,s)&&!NavalDestroyerLayout.Stair(prior,s))Check(NavalDestroyerNavigation.Clear(prior,p,s),"Blocked segment: "+prior.X+","+prior.Z+" -> "+p.X+","+p.Z+" y="+p.Y);prior=p;}routes++;}
 float time;Check(NavalTorpedoMath.Lead(1000,0,0,0,out time)&&Math.Abs(time-50)<.01,"Stationary lead");Check(NavalTorpedoMath.Lead(1000,0,0,5,out time)&&time>50,"Crossing lead");Check(!NavalTorpedoMath.Lead(1000,0,25,0,out time),"Unreachable receding target");Check(!NavalTorpedoMath.Armed(69)&&NavalTorpedoMath.Armed(70),"Safe run");Check(NavalTorpedoMath.Threat(200,0,-20,0,10,out time)&&Math.Abs(time-10)<.01,"Collision prediction");Check(!NavalTorpedoMath.Threat(200,100,-20,0,10,out time),"Passing torpedo");
 Check(NavalTorpedoMath.SweepNearHull(-200,0,200,0,100),"Fast relative sweep crosses a hull between samples");
 Check(!NavalTorpedoMath.SweepNearHull(-200,150,200,150,100),"Distant parallel sweep is culled");
 Console.WriteLine("PASS: Novik dimensions, crew, "+routes+" service routes; torpedo lead, safe run and threat prediction.");}
}

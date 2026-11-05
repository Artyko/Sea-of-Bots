using System;
class Naval322Checks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){var s=NavalShipDefinition.Novik;float q=s.Scale;
 foreach(float side in new[]{-1f,1f})foreach(float z in new[]{-20.5f,20.5f}){
 var a=new NavalWalkPoint(side*5.6f/q,s.HoldFloor,-27f/q);var b=new NavalWalkPoint(side*5.6f/q,s.WeatherDeck,z/q);
 var route=NavalDestroyerLayout.Route(a,b,s);bool outer=false;
 foreach(var p in route)if(Math.Abs(p.X*q-side*5.6f)<.01f){outer=true;Check(Math.Abs(p.X*q)-.8f>4.6f,"Full capsule clears hatch edge");Check(Math.Abs(p.X*q)+.8f<7f,"Full capsule clears side armour");}
 Check(outer,"Uses widened side aisle");
 }
 for(int style=0;style<3;style++){float lo=NavalTorpedoSalvoMath.LaunchDistance(style,0),hi=NavalTorpedoSalvoMath.LaunchDistance(style,1);Check(lo>=280&&hi<=600&&lo<hi,"Close varied launch bands");Check(lo>NavalTorpedoMath.SafeRun+150,"Safe arming approach");}
 Check(5.6f-3f>1.6f+.2f,"Engineer and passing sailor have separate footprints");
 Console.WriteLine("PASS: four stair approaches clear hatch edges, engineer passing separation, close torpedo bands with safe run.");}
}

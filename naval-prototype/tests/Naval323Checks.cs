using System;
class Naval323Checks {
 static void Check(bool v,string why){if(!v)throw new Exception(why);}
 static void Main(){var s=NavalShipDefinition.Novik;float q=s.Scale;int count=0;
 foreach(float centre in new[]{-20.5f,20.5f})foreach(float side in new[]{-1f,1f})foreach(float t in new[]{.25f,.5f,.75f})foreach(bool up in new[]{true,false}){
 var a=new NavalWalkPoint(side*3.6f/q,s.HoldFloor+(s.WeatherDeck-s.HoldFloor)*t,(centre-4+8*t)/q);
 var b=new NavalWalkPoint(side*5.6f/q,up?s.WeatherDeck:s.HoldFloor,-32/q);
 var route=NavalDestroyerLayout.Route(a,b,s);Check(route.Count>2,"Continued flight exists");
 float previous=a.Y;bool landing=false;
 foreach(var p in route){if(Math.Abs(p.X*q-side*3.6f)>.01f){landing=true;Check(Math.Abs(previous-(up?s.WeatherDeck:s.HoldFloor))<.001,"Reach landing height before leaving stair");break;}
 Check(Math.Abs(p.Y-previous)*q<=.25f,"No vertical shortcut through stair side");Check(up?p.Y>=previous-.001:p.Y<=previous+.001,"Correct tread direction");previous=p.Y;}
 Check(landing,"Flight exits to real landing");count++;
 }
 Check(NavalCrewController.TorpedoOperatorCount==4,"Four independent torpedo operators");
 Console.WriteLine("PASS: "+count+" mid-flight task changes follow treads to landing; four torpedo operator slots.");}
}

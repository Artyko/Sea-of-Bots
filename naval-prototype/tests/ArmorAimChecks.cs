using System;
class ArmorAimChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static float C(float vx,float vz,float x,float z,float slope,bool end){return NavalHullAimZones.Incidence(vx,0,vz,x,0,z,slope,end,false);}
 static void Main(){
  Check(C(0,100,0,6,0,true)>.99f,"Bow plate faces bow attacker");
  Check(C(0,-100,0,-6,0,true)>.99f,"Stern plate faces stern attacker");
  Check(C(0,100,2,0,0,false)==0,"Bow attacker rejects grazing side");
  Check(C(0,-100,-2,0,0,false)==0,"Stern attacker rejects grazing side");
  Check(C(100,0,2,0,0,false)>.99f&&C(-100,0,-2,0,0,false)>.99f,"Both broadsides");
  Check(C(100,0,-2,0,0,false)==0,"Far side faces away");
  Check(C(50,100,1,5,-1.5f,false)>.95f,"Tapered bow normal");
  Check(NavalHullAimZones.Incidence(0,100,0,0,0,0,0,false,true)>.99f,"Deck from above");
  Check(NavalHullAimZones.Incidence(100,0,0,0,0,0,0,false,true)==0,"Grazing deck");
  int count=0;for(int a=0;a<360;a++){double rad=a*Math.PI/180;float vx=(float)Math.Sin(rad)*100,vz=(float)Math.Cos(rad)*100;
   float best=0;foreach(int side in new[]{-1,1}){best=Math.Max(best,C(vx,vz,side*2,0,0,false));best=Math.Max(best,C(vx,vz,0,side*6,0,true));}
   Check(best>.65f,"Exposed projection available at "+a);count++;
  }
  Console.WriteLine("PASS: bow/stern/both sides, tapered armor, deck and "+count+" attack bearings.");
 }
}

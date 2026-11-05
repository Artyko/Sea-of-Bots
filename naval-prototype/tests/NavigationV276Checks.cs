using System;
using System.Collections.Generic;
class NavigationV276Checks {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Main(){
  foreach(var material in new[]{NavalPenetrationMaterial.Armor,NavalPenetrationMaterial.Iron,NavalPenetrationMaterial.Machinery}){
   Check(NavalArmorMath.ArmsInternalFuse(material,1f),"Metal must still arm AP");
   Check(!NavalArmorMath.ArmsInternalFuse(material,0),"Stopped round cannot arm");
  }
  Check(!NavalArmorMath.ArmsInternalFuse(NavalPenetrationMaterial.Wood,100),"Even powerful AP must not arm in wood");
  var random=new Random(276);var index=new NavalPointIndex();var candidates=new List<int>();
  float[,] cells=new float[1500,6];float extent=0;
  for(int i=0;i<1500;i++){
   for(int a=0;a<3;a++)cells[i,a]=(float)(random.NextDouble()*12-6);
   cells[i,3]=.1f+(float)random.NextDouble()*.9f;cells[i,4]=.1f;cells[i,5]=.1f+(float)random.NextDouble()*.9f;
   extent=Math.Max(extent,Math.Max(cells[i,3],cells[i,5])*.5f);index.Add(i,cells[i,0],cells[i,1],cells[i,2]);
  }
  foreach(float scale in new[]{1f,2f,4f,6f})for(int n=0;n<500;n++){
   float x=(float)(random.NextDouble()*12-6),y=(float)(random.NextDouble()*12-6),z=(float)(random.NextDouble()*12-6),radius=.8f;
   index.Query(x,y,z,extent+Math.Max(.08f,.45f/scale)+radius/scale,candidates);
   bool indexed=false,brute=false;
   foreach(int i in candidates)if(NavalDeckGapMath.Overlaps(x,y,z,radius,scale,cells[i,0],cells[i,1],cells[i,2],cells[i,3],cells[i,4],cells[i,5]))indexed=true;
   for(int i=0;i<1500;i++)if(NavalDeckGapMath.Overlaps(x,y,z,radius,scale,cells[i,0],cells[i,1],cells[i,2],cells[i,3],cells[i,4],cells[i,5]))brute=true;
   Check(indexed==brute,"Indexed deck-gap detection must preserve exact results");
  }
  foreach(var ship in new[]{NavalShipDefinition.Dreadnought,NavalShipDefinition.Ironclad}){
   float floor=ship.HoldFloor;
   var near=new NavalWalkPoint(ship.StairWidth*.5f+.7f/ship.Scale,floor,-1.2f);
   Check(!NavalSupplyRoutes.CargoClear(near,near,ship),"Flight bypass must include full-size shoulders");
  }
  Console.WriteLine("PASS: AP material arming, 2000 indexed/brute deck-gap comparisons and full-body stair margins.");
 }
}

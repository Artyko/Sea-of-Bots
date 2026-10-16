using System;
class CoamingChecks {
 static void Check(bool b,string message){if(!b)throw new Exception(message);}
 static void Main(){
 foreach(float scale in new float[]{2f,3f,4f}){
  float height=NavalCompartmentMath.CoamingHeight(scale);
  Check(Math.Abs(height*scale-.3f)<.0001f,"Physical threshold height");
  Check(height*scale/4f<.1f,"Small walkable risers");
 }
 var grid=new NavalFloodGrid(2,1,2f,1f,1f,4f);
 grid.RightSill[0]=.3f;grid.Add(0,.2f);
 grid.Spread(.5f,new float[2],1f);Check(grid.Water[1]==0f,"Shallow water retained");
 grid.Add(0,.4f);float total=grid.Total;
 grid.Spread(.5f,new float[2],1f);Check(grid.Water[1]>0f,"Overflow above threshold");
 Check(Math.Abs(grid.Total-total)<.0001f,"Volume conserved");
 var broken=new NavalFloodGrid(2,1,2f,1f,1f,4f);broken.Add(0,.2f);broken.RightSill[0]=0f;
 broken.Spread(.5f,new float[2],1f);Check(broken.Water[1]>0f,"Broken threshold allows shallow flow");
 Console.WriteLine("Coaming checks passed");
 }
}

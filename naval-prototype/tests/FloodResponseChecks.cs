using System;
class FloodResponseChecks
{
 static void Check(bool value,string name){if(!value)throw new Exception(name);}
 static void Near(float a,float b){Check(Math.Abs(a-b)<.0001f,"Water conserved");}
 static void Main()
 {
  Check(NavalFloodResponseMath.Isolate(.4f,.05f,.1f),"Isolate leaking room before corridor floods");
  Check(NavalFloodResponseMath.Isolate(.7f,.05f,0f),"Retain accumulated water after a leak is patched");
  Check(!NavalFloodResponseMath.Isolate(.1f,0f,1f),"Do not seal shallow splash water");
  Check(!NavalFloodResponseMath.Isolate(.7f,.65f,1f),"Equal levels do not cause pointless isolation");
  Check(!NavalFloodResponseMath.SafeToRemove(.6f,.05f,0f),"Patch alone does not justify releasing a flooded room");
  Check(!NavalFloodResponseMath.SafeToRemove(.05f,.05f,.1f),"Active inflow prevents reopening");
  Check(NavalFloodResponseMath.SafeToRemove(.05f,.05f,0f),"Drained room can reopen");
  Check(!NavalFloodResponseMath.Isolate(.05f,.05f,0f),"Drained room will not immediately close again");
  foreach(float wall in new float[]{-1.4375f,1.4375f})foreach(float z in new float[]{-2.625f,2.625f})
  {
   Check(NavalFloodResponseMath.Crosses(wall-.5f,z,wall+.5f,z,wall,z),"Crossing closed opening requests removal");
   Check(NavalFloodResponseMath.Crosses(wall+.5f,z,wall-.5f,z,wall,z),"Escape from either side can remove boards");
   Check(!NavalFloodResponseMath.Crosses(wall-.5f,z-1f,wall-.5f,z+1f,wall,z),"Walking down corridor does not reopen boards");
   Check(!NavalFloodResponseMath.Crosses(wall-.5f,z+2f,wall+.5f,z+2f,wall,z),"Do not request unrelated opening");
  }
  foreach(float scale in new float[]{2f,3f,4f})
  {
   float sill=NavalCompartmentMath.CoamingHeight(scale)*scale;
   var grid=new NavalFloodGrid(2,1,2f,1f,1f,4f);grid.RightSill[0]=1.5f;grid.Add(0,.8f);
   grid.Spread(.5f,new float[2],1f);Near(grid.Water[1],0f);
   grid.RightSill[0]=sill;float total=grid.Total;
   grid.Spread(.5f,new float[2],1f);Check(grid.Water[1]>0f,"Removing shield restores flow over coaming");Near(grid.Total,total);
  }
  var overflow=new NavalFloodGrid(2,1,2f,1f,1f,4f);overflow.RightSill[0]=1.5f;overflow.Add(0,2f);
  overflow.Spread(.2f,new float[2],1f);Check(overflow.Water[1]>0f,"Water can overtop wooden shield");Near(overflow.Total,2f);
  var damaged=new NavalFloodGrid(2,1,2f,1f,1f,4f);damaged.RightSill[0]=.5f;damaged.Add(0,.8f);
  damaged.Spread(.2f,new float[2],1f);Check(damaged.Water[1]>0f,"Broken boards lower retention height");Near(damaged.Total,.8f);
  Console.WriteLine("Flood response checks passed: isolation, reopening, both-way passage, overflow, damage and volume conservation.");
 }
}

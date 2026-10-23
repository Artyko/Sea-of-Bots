using System;
// Free volume and first moment in a hydraulic column with solid/closed obstructions.
public sealed class NavalFloodStorage
{
 private readonly float[,] bottoms,tops,fractions;
 private readonly int count;
 public NavalFloodStorage(NavalFloodGrid grid,NavalShipDefinition ship)
 {
  count=3;bottoms=new float[grid.Water.Length,count];tops=new float[grid.Water.Length,count];fractions=new float[grid.Water.Length,count];
  for(int i=0;i<grid.Water.Length;i++){
   float x=grid.X(i),z=grid.Z(i),dx=grid.CellWidth,dz=grid.CellLength;
   float tankArea=0,tankHeightArea=0;
   for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2){
    float a=Overlap(x-dx*.5f,x+dx*.5f,side*NavalBallastLayout.X-NavalBallastLayout.Width*.5f,side*NavalBallastLayout.X+NavalBallastLayout.Width*.5f);
    float left=Math.Max(z-dz*.5f,end*NavalBallastLayout.Z-NavalBallastLayout.Length*.5f),right=Math.Min(z+dz*.5f,end*NavalBallastLayout.Z+NavalBallastLayout.Length*.5f);
    if(a<=0||right<=left)continue;
    float height=0;for(int sample=0;sample<12;sample++)height+=NavalBallastLayout.Surface(side*NavalBallastLayout.X,left+(sample+.5f)*(right-left)/12,0,ship.Scale)/12;
    float area=a*(right-left);tankArea+=area;tankHeightArea+=area*height;
   }
   float fraction=tankArea/(dx*dz),tankHeight=tankArea>0?tankHeightArea/tankArea:0;
   bottoms[i,0]=0;tops[i,0]=tankHeight;fractions[i,0]=fraction;
   bottoms[i,1]=.575f-ship.HoldFloor;tops[i,1]=.825f-ship.HoldFloor;
   float hole=Overlap(x-dx*.5f,x+dx*.5f,-ship.StairWidth*.5f-.025f,ship.StairWidth*.5f+.025f)*Overlap(z-dz*.5f,z+dz*.5f,-3,.5f)/(dx*dz);
   fractions[i,1]=1-hole;
   bottoms[i,2]=.825f-ship.HoldFloor;tops[i,2]=bottoms[i,2]+tankHeight;fractions[i,2]=fraction;
  }
 }
 private static float Overlap(float a,float b,float c,float d){return Math.Max(0,Math.Min(b,d)-Math.Max(a,c));}
 public float FreeDepth(int cell,float depth)
 {
  float result=depth;for(int layer=0;layer<count;layer++)result-=fractions[cell,layer]*Math.Max(0,Math.Min(depth,tops[cell,layer])-bottoms[cell,layer]);
  return Math.Max(0,result);
 }
 public float Depth(int cell,float free)
 {
  float depth=0;
  for(int layer=0;layer<count;layer++){
   float gap=bottoms[cell,layer]-depth;if(free<=gap)return depth+free;
   free-=gap;depth=bottoms[cell,layer];float height=tops[cell,layer]-depth,area=1-fractions[cell,layer];
   if(area>.00001f&&free<height*area)return depth+free/area;
   free-=height*area;depth=tops[cell,layer];
  }
  return depth+Math.Max(0,free);
 }
 public float Center(int cell,float depth)
 {
  float first=depth*depth*.5f,free=FreeDepth(cell,depth);
  for(int layer=0;layer<count;layer++){float top=Math.Min(depth,tops[cell,layer]),bottom=bottoms[cell,layer];if(top>bottom)first-=fractions[cell,layer]*(top*top-bottom*bottom)*.5f;}
  return free>.00001f?first/free:0;
 }
}

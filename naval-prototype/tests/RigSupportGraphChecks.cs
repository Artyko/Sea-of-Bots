using System;
using System.Collections.Generic;
class RigSupportGraphChecks
{
 static void Check(bool v,string text){if(!v)throw new Exception(text);}
 static readonly List<float[]> cells=new List<float[]>();static readonly List<bool> cloth=new List<bool>(),mast=new List<bool>();static readonly List<int> indices=new List<int>();
 static void Box(float x,float y,float z,float sx,float sy,float sz,bool stem)
 {
  int nx=(int)Math.Ceiling(sx/.25f),ny=(int)Math.Ceiling(sy/.25f),nz=(int)Math.Ceiling(sz/.25f);
  for(int a=0;a<nx;a++)for(int b=0;b<ny;b++)for(int c=0;c<nz;c++){
   cells.Add(new[]{x+(a+.5f)*sx/nx-sx*.5f,y+(b+.5f)*sy/ny-sy*.5f,z+(c+.5f)*sz/nz-sz*.5f,sx/nx*.5f,sy/ny*.5f,sz/nz*.5f});cloth.Add(false);mast.Add(stem);indices.Add(-1);
  }
 }
 static void Main(){
  Box(0f,4.1f,0f,.3f,9.2f,.3f,true);Box(0f,1.1f,0f,.65f,.55f,.65f,true);
  Box(0f,7.8f,0f,5.9f,.2f,.2f,false);Box(0f,3.62f,0f,5.7f,.16f,.18f,false);Box(0f,8.5f,0f,1.1f,.16f,.18f,false);
  for(int y=0;y<16;y++)for(int x=0;x<22;x++){
   float u=(x+.5f)/22f,v=(y+.5f)/16f;
   cells.Add(new[]{(u-.5f)*5.5f,3.7f+v*4f,.02f+(float)(Math.Sin(u*Math.PI)*Math.Sin(v*Math.PI))*.5f,.125f,.125f,.035f});cloth.Add(true);mast.Add(false);indices.Add(y*22+x);
  }
  var bounds=new float[cells.Count,6];for(int i=0;i<cells.Count;i++)for(int j=0;j<6;j++)bounds[i,j]=cells[i][j];
  var graph=new NavalRigSupportGraph(bounds,cloth.ToArray(),mast.ToArray(),indices.ToArray(),22,.825f);var missing=new bool[cells.Count];
  graph.Evaluate(missing,true);for(int i=0;i<missing.Length;i++)Check(graph.Supported[i],"Intact rig must stay attached: "+i);
  // Cut a yard off to the right of the mast. Cloth must not support its wood.
  for(int i=0;i<missing.Length;i++)if(!mast[i]&&!cloth[i]&&Math.Abs(bounds[i,1]-7.8f)<.1f&&bounds[i,0]>.3f&&bounds[i,0]<.6f)missing[i]=true;
  graph.Evaluate(missing,true);bool yardFalls=false,mastStays=false;
  for(int i=0;i<missing.Length;i++){
   if(!cloth[i]&&!mast[i]&&!missing[i]&&Math.Abs(bounds[i,1]-7.8f)<.1f&&bounds[i,0]>.8f){Check(!graph.Supported[i],"Detached horizontal yard cannot hang on cloth");yardFalls=true;}
   if(mast[i]&&bounds[i,1]>8f){Check(graph.Supported[i],"Uncut mast remains");mastStays=true;}
  }
  Check(yardFalls&&mastStays,"Test includes detached yard and surviving mast");
  Array.Clear(missing,0,missing.Length);
  for(int i=0;i<missing.Length;i++)if(mast[i]&&bounds[i,1]>5f&&bounds[i,1]<5.5f)missing[i]=true;
  graph.Evaluate(missing,true);
  for(int i=0;i<missing.Length;i++)if(!missing[i]){
   if(bounds[i,1]>5.7f)Check(!graph.Supported[i],"Upper mast, yards and cloth fall after cross-cut");
   if(mast[i]&&bounds[i,1]<4.9f)Check(graph.Supported[i],"Lower mast stays attached");
   if(cloth[i]&&bounds[i,1]<4.9f)Check(graph.Supported[i],"Lower cloth keeps lower attachment");
  }
  int detached=0;foreach(var component in graph.FallingComponents(missing))foreach(int i in component){Check(!missing[i]&&!graph.Supported[i],"Only actual unsupported cells drop");detached++;}
  Check(detached>0,"Cut produces falling components");
  Array.Clear(missing,0,missing.Length);graph.Evaluate(missing,false);for(int i=0;i<missing.Length;i++)Check(!graph.Supported[i],"No foundation releases entire rig");
  graph.Evaluate(missing,true);for(int i=0;i<missing.Length;i++)Check(graph.Supported[i],"Restoring foundation restores support");
  Console.WriteLine("PASS: actual rig geometry, detached horizontal yard, cloth cannot carry wood, middle cut, separate falling components and foundation restoration ("+cells.Count+" cells).");
 }
}

using System;
using System.Collections.Generic;
class GalleonRigChecks
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
 static void Main()
 {
  var ship=NavalShipDefinition.Galleon;
  for(int rig=0;rig<3;rig++)
  {
   cells.Clear();cloth.Clear();mast.Clear();indices.Clear();float factor=ship.SailSize(rig),baseY=ship.WeatherDeck,top=baseY+(8.7f-.825f)*factor;
   Box(0,(top-.5f)*.5f,0,.3f,top+.5f,.3f,true);
   Box(0,baseY+(1.1f-.825f)*factor,0,.65f*factor,.55f*factor,.65f*factor,true);
   Box(0,baseY+(7.8f-.825f)*factor,0,5.9f*factor,.2f*factor,.2f*factor,false);
   Box(0,baseY+(3.62f-.825f)*factor,0,5.7f*factor,.16f*factor,.18f*factor,false);
   Box(0,baseY+(8.5f-.825f)*factor,0,1.1f*factor,.16f*factor,.18f*factor,false);
   int columns=(int)Math.Ceiling(22*factor),rows=(int)Math.Ceiling(16*factor);
   for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
   {
    float u=(x+.5f)/columns,v=(y+.5f)/rows;
    cells.Add(new[]{(u-.5f)*5.5f*factor,baseY+(3.7f-.825f+v*4)*factor,.02f+(float)(Math.Sin(u*Math.PI)*Math.Sin(v*Math.PI))*.5f,.125f*factor*22/columns,.125f*factor*16/rows,.035f});cloth.Add(true);mast.Add(false);indices.Add(y*columns+x);
   }
   var bounds=new float[cells.Count,6];for(int i=0;i<cells.Count;i++)for(int j=0;j<6;j++)bounds[i,j]=cells[i][j];
   var graph=new NavalRigSupportGraph(bounds,cloth.ToArray(),mast.ToArray(),indices.ToArray(),columns,.825f);var missing=new bool[cells.Count];
   graph.Evaluate(missing,true);for(int i=0;i<missing.Length;i++)Check(graph.Supported[i],"Scaled intact rig must stay attached: rig="+rig+" cell="+i);
   float cut=baseY+(5.5f-.825f)*factor;
   for(int i=0;i<missing.Length;i++)if(mast[i]&&Math.Abs(bounds[i,1]-cut)<.3f)missing[i]=true;
   graph.Evaluate(missing,true);int fallen=0;
   for(int i=0;i<missing.Length;i++)if(!missing[i]&&bounds[i,1]>cut+.35f){Check(!graph.Supported[i],"Upper mast, yard and cloth must fall after mid cut");fallen++;}
   Check(fallen>10,"Mid cut detaches a real upper assembly");
   Console.WriteLine("PASS: galleon rig "+rig+", size "+factor+", "+cells.Count+" actual-geometry cells supported intact; cross-cut sheds "+fallen+" upper cells.");
  }
 }
}

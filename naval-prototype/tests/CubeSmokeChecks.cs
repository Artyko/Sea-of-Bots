using System;
class CubeSmokeChecks {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Main(){foreach(float life in new float[]{6,12,18}){
 Check(Math.Abs(NavalCubeSmokeMath.Grey(0,life)-.12f)<.0001,"Charcoal source colour");
 Check(Math.Abs(NavalCubeSmokeMath.Grey(1000,life)-.60f)<.0001,"Light grey cap without white");
 float previous=0;for(int i=0;i<100;i++){float grey=NavalCubeSmokeMath.Grey(i*.5f,life);Check(grey>=previous&&grey<=.601f,"Continuous monotonically lighter height gradient");previous=grey;}
 Check(NavalCubeSmokeMath.Size(life,life)==0,"Cube disappears by shrinking, not transparent sheets");
 Check(NavalCubeSmokeMath.Size(.4f,life)>NavalCubeSmokeMath.Size(0,life),"Smooth growth at source");
 Check(NavalCubeSmokeMath.Size(life*.99f,life)<NavalCubeSmokeMath.Size(life*.8f,life),"Dissipating cubes shrink");}
 Check(NavalCubeSmokeMath.Face(2)>NavalCubeSmokeMath.Face(0)&&NavalCubeSmokeMath.Face(0)>NavalCubeSmokeMath.Face(4),"Distinct top and side shades show volume");
 Check(NavalCubeSmokeMath.Capacity*24<65535,"Bounded mesh fits 16-bit vertex indices");
 Check(NavalSmokePlumeMath.RelativeDrift(4,4)<-10,"Moving ships leave smoke behind");
 Console.WriteLine("PASS: height gradient, charcoal/light grey bounds, cube growth/dissipation, face shading, mesh bound and moving-source trail.");}
}

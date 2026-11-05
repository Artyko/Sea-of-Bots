using UnityEngine;
public partial class NavalStations {
 private float MainMuzzleZ {get{return NavalGunDimensions.Muzzle(NavalGunDimensions.IroncladMainCalibre)/hull.WorldScale;}}
 private const float CapitalBarrelLengthScale=.72f;
 private float CapitalMuzzleZ {get{return NavalGunDimensions.Muzzle(NavalGunDimensions.CapitalCalibre)*CapitalBarrelLengthScale/hull.WorldScale;}}
 private void CalibratedTube(Transform parent,Material metal,Material rim,int mm,float metresPerUnit,float x=0,float lengthScale=1f){
  int first=hull.Voxels.Count;
  float q=metresPerUnit,tip=NavalGunDimensions.Muzzle(mm)*lengthScale,wide=NavalGunDimensions.BreechDiameter(mm),tube=NavalGunDimensions.TubeDiameter(mm);
  hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.08f/q),new Vector3(wide,wide,tip*.22f)/q,true);
  if(mm==NavalGunDimensions.CapitalCalibre)
  {
   // A reinforced jacket steps down to the long chase and muzzle.
   hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.28f/q),new Vector3(wide,wide,tip*.40f)/q,true);
   hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.57f/q),new Vector3(tube*.83f,tube*.83f,tip*.30f)/q,true);
   tube*=.67f;
   hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.84f/q),new Vector3(tube,tube,tip*.32f)/q,true);
  }
  else hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.57f/q),new Vector3(tube,tube,tip*.84f)/q,true);
  if(mm<=152){
   hull.AddDecorationBox(parent,metal,new Vector3(x,0,tip*.30f/q),new Vector3(wide*.82f,wide*.82f,tip*.42f)/q,true);
   for(int side=-1;side<=1;side+=2)hull.AddDecorationBox(parent,rim,new Vector3(x+side*wide*.48f/q,-wide*.35f/q,tip*.22f/q),new Vector3(.14f,.14f,tip*.48f)/q,true);
  }
  // Four lips leave the muzzle visibly hollow rather than a solid black cap.
  float bore=NavalGunDimensions.Bore(mm),lip=(tube-bore)*.5f;
  for(int side=-1;side<=1;side+=2){
   hull.AddDecorationBox(parent,rim,new Vector3(x+side*(bore+lip)*.5f/q,0,tip/q),new Vector3(lip,tube,lip)/q,true);
   hull.AddDecorationBox(parent,rim,new Vector3(x,side*(bore+lip)*.5f/q,tip/q),new Vector3(bore,lip,lip)/q,true);
  }
  for(int i=first;i<hull.Voxels.Count;i++){var c=hull.Voxels[i];c.Metallic=true;c.Flammable=false;if(mm>152)c.Armored=true;}
 }
}

using UnityEngine;
public partial class NavalStations {
 private Transform[] openShields;
 private void BuildOpenShield(int gun,Material armour,Material trim){
  if(openShields==null)openShields=new Transform[GunCount];
  var shield=new GameObject("102 mm traversing armoured casemate").transform;shield.SetParent(guns[gun],false);openShields[gun]=shield;
  int first=hull.Voxels.Count;
  // Solid stepped cheeks and split glacis leave an actual barrel embrasure.
  for(int side=-1;side<=1;side+=2){
   hull.AddDecorationBox(shield,armour,new Vector3(side*.80f,.95f,.55f),new Vector3(.85f,2.65f,.24f),true);
   hull.AddDecorationBox(shield,armour,new Vector3(side*NavalGunStationLayout.ShieldHalfWidth,.95f,-1.00f),new Vector3(NavalGunStationLayout.ShieldThickness,2.65f,3.5f),true);
   hull.AddDecorationBox(shield,trim,new Vector3(side*1.30f,.95f,.58f),new Vector3(.27f,2.70f,.28f),true);
  }
  hull.AddDecorationBox(shield,armour,new Vector3(0,NavalGunStationLayout.RoofY,-1.00f),new Vector3(2.85f,NavalGunStationLayout.RoofThickness,3.5f),true);
  hull.AddDecorationBox(shield,trim,new Vector3(0,-.27f,-.12f),new Vector3(2.65f,.20f,1.7f),true);
  for(int i=first;i<hull.Voxels.Count;i++){var c=hull.Voxels[i];c.Metallic=true;c.Armored=true;c.Flammable=false;c.MovableEquipment=true;}
 }
}

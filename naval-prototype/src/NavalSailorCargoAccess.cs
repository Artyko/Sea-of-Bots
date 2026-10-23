using UnityEngine;
using System.Collections.Generic;
public partial class NavalRepairSailor {
 private readonly Dictionary<object,float> blockedCargo=new Dictionary<object,float>();
 private bool CargoRetry(object stock){float until;return !blockedCargo.TryGetValue(stock,out until)||Time.time>=until;}
 private Vector3 CargoApproach(Vector3 pickup,int index){
  Vector3 p=pickup;if(index>0)p.z+=(index==1?-.4f:.4f)/hull.WorldScale;
  float floor=hull.Floor;for(int level=0;level<hull.Definition.MaintenanceDeckCount;level++)if(hull.Definition.MaintenanceFloor(level)<=pickup.y+.001f)floor=hull.Definition.MaintenanceFloor(level);
  p.y=NavalBallastLayout.Surface(p.x,p.z,floor,hull.WorldScale);return p;
 }
}

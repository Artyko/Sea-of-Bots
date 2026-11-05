using UnityEngine;
public partial class NavalRepairSailor {
 private float trafficWaitUntil,trafficLogAt;
 public int PassagePriority {get{return FightingFire?0:RepairingLeak?1:RepairingPump||Bailing?2:DeliveringAmmo?3:4;}}
 public Vector3 TravelDirection {get{return route.Count>0?boat.TransformVector(route[0]-feet).normalized:Vector3.zero;}}
 public void WaitForCrewTraffic(){trafficWaitUntil=Time.time+.25f;if(Time.time>=trafficLogAt){trafficLogAt=Time.time+4;Debug.Log("[NavalPrototype] Stair traffic yielding: ship="+boat.name+" sailor="+robot.GetInstanceID()+" task="+Status);}}
 private bool TickTrafficWait(float dt){if(Time.time>=trafficWaitUntil)return false;stuck=0;navigationRecovery.Reset();movementWatchdog.Reset();HoldPosition();Walking(false,dt);return true;}
}

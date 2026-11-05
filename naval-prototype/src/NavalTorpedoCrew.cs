using UnityEngine;
public partial class NavalCrewController {
 private void AssignPlantHands(bool emergency,bool releaseOne){var plant=stations.SteamPlant;if(plant==null||!hull.Definition.Destroyer)return;plant.OilEngineerCount=0;if(emergency)return;
  for(int post=0;post<(releaseOne?1:2);post++){int chosen=-1;float best=float.PositiveInfinity;for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&(!workers[i].Busy||workers[i].ReturningWithoutWork||roles[i]==300+post)){float distance=(crew[i].transform.position-hull.Root.TransformPoint(plant.OilEngineerPost(post))).sqrMagnitude;if(roles[i]==300+post)distance=-1;if(distance<best){chosen=i;best=distance;}}if(chosen>=0)assigned[chosen]=300+post;}
 }
 private bool TickPlantHand(int i,float dt){if(assigned[i]<300||assigned[i]>=302)return false;var plant=stations.SteamPlant;Vector3 post=plant.OilEngineerPost(assigned[i]-300);workers[i].SpareMaintenance=false;workers[i].AssignFreeCombatPost(post);workers[i].SetPost(post);workers[i].Tick(dt,false);roles[i]=assigned[i];if(!workers[i].Busy&&workers[i].AtStation(post)){workers[i].AnchorAtPost(post);plant.OilEngineerCount++;}return true;}
 public const int TorpedoOperatorCount=4;
 private void AssignTorpedoHands(bool emergency,Transform enemy){var battery=stations.Torpedoes;if(battery==null||enemy==null||!battery.HasCombatPotential||emergency)return;
  for(int slot=0;slot<TorpedoOperatorCount;slot++){int chosen=-1,mount=-1;float best=float.PositiveInfinity;
   for(int i=0;i<crew.Length;i++)if(Free(i)&&assigned[i]<0&&!(roles[i]>0&&roles[i]<=stations.GunCount&&stations.CrewGunAssignable(roles[i]))&&(!workers[i].Busy||workers[i].ReturningWithoutWork||roles[i]>=200&&roles[i]<204))for(int m=0;m<4;m++)if(battery.Available(m)){
    bool taken=false;for(int k=0;k<assigned.Length;k++)if(assigned[k]==200+m)taken=true;if(taken)continue;
    float distance=(crew[i].transform.position-hull.Root.TransformPoint(battery.Post(m))).sqrMagnitude;
    if(roles[i]==200+m)distance=-1; // hold a valid assignment until spent or interrupted
    if(distance<best){best=distance;chosen=i;mount=m;}
   }
   if(chosen>=0){assigned[chosen]=200+mount;}
  }
 }
 private bool TickTorpedoHand(int i,float dt,Transform enemy,Vector3 velocity){if(assigned[i]<200||assigned[i]>=204)return false;var b=stations.Torpedoes;if(b==null)return false;Vector3 post=b.Post(assigned[i]-200);workers[i].SpareMaintenance=false;workers[i].AssignFreeCombatPost(post);workers[i].SetPost(post);workers[i].Tick(dt,false);roles[i]=assigned[i];
  if(!workers[i].Busy&&workers[i].AtStation(post)){workers[i].AnchorAtPost(post);b.OperatorAt(assigned[i]-200,crew[i]);b.AimAI(assigned[i]-200,enemy,velocity,dt,crew[i]);}return true;
 }
}

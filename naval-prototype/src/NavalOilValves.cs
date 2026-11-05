using System.Collections.Generic;
using UnityEngine;
public sealed partial class NavalSteamPlant {
 private readonly NavalSteamIsolation isolation=new NavalSteamIsolation();
 private readonly List<NavalHullVoxel>[] oilValveCells={new List<NavalHullVoxel>(),new List<NavalHullVoxel>(),new List<NavalHullVoxel>()};
 private int nearestOilValve=-1;
 private Vector3 ValvePost(int i){return new Vector3(5.5f/hull.WorldScale,hull.Floor,(-23-i*2)/hull.WorldScale);}
 private void BuildOilValves(Material metal){for(int i=0;i<3;i++){Vector3 p=ValvePost(i)+new Vector3(.9f,1.3f,0)/hull.WorldScale;OilBox(oilValveCells[i],p,new Vector3(.35f,.35f,.35f),metal);new NavalRepairableMechanism(hull,oilValveCells[i],false,false,"Запорный клапан "+(i+1));}}
 private void Update(){
  nearestOilValve=-1;if(oilStates==null)return;var world=NavalTestWorld.Active;
  if(world==null||!world.SimulationActive||world.Spectating||world.PlayerHull!=hull)return;
  float best=2f;for(int i=0;i<3;i++){float distance=Vector3.Distance(world.PlayerFootPoint,hull.Root.TransformPoint(ValvePost(i)));if(distance<best&&Integrity(oilValveCells[i])>.95f){best=distance;nearestOilValve=i;}}
  if(nearestOilValve>=0&&Input.GetKeyDown(KeyCode.V)){isolation.Toggle(nearestOilValve);Debug.Log("[NavalPrototype] Steam valve: ship="+hull.Root.name+" branch="+nearestOilValve+" closed="+isolation.Closed(nearestOilValve));}
 }
 private void OnGUI(){if(nearestOilValve<0)return;string[] names={"левая магистраль","правая магистраль","вспомогательная линия"};GUI.Box(new Rect(Screen.width*.5f-250,Screen.height-165,500,30),"V — "+(isolation.Closed(nearestOilValve)?"открыть ":"отсечь ")+names[nearestOilValve]);}
 public string OilDiagnostics {get{var text=new System.Text.StringBuilder();text.Append("telegraph=").Append(setting.ToString("F2")).Append(" fuel_tanks=").Append(Integrity(oilFuelTanks[0]).ToString("F2")).Append("/").Append(Integrity(oilFuelTanks[1]).ToString("F2")).Append(" mains=").Append(PropulsionMainIntegrity.ToString("F2")).Append("/").Append(PumpMainIntegrity.ToString("F2")).Append(" leak=").Append(SteamLeakSeverity.ToString("F2")).Append(" oil engineers=").Append(OilEngineerCount).Append(" power=").Append(OilPower.ToString("F2"));for(int i=0;i<6;i++)text.Append(" boiler=").Append(i).Append(" pressure=").Append(oilStates[i].Pressure.ToString("F2")).Append(" fuel=").Append(oilStates[i].Coal.ToString("F1")).Append(" intact=").Append(Integrity(oilBoilers[i]).ToString("F2")).Append(" ruptured=").Append(oilStates[i].Ruptured);for(int i=0;i<3;i++)text.Append(" drive_connected=").Append(OilDriveConnected(i)).Append(" valve=").Append(i).Append(" closed=").Append(isolation.Closed(i)).Append(" trapped=").Append(isolation.Steam(i).ToString("F2")).Append(" turbine=").Append(Integrity(oilTurbines[i]).ToString("F2")).Append(" shaft=").Append(Integrity(oilShafts[i]).ToString("F2"));return text.ToString();}}
 private float MainSupply(List<NavalHullVoxel> cells){float raw=Integrity(cells);return oilStates==null?raw:isolation.Supply(cells==propulsionMain?0:cells==pumpMain?1:2,raw);}
 private float MainSteam(List<NavalHullVoxel> cells){return oilStates==null?Pressure:isolation.Steam(cells==propulsionMain?0:cells==pumpMain?1:2);}
}
